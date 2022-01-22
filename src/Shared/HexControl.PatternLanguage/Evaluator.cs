using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexControl.Core.Buffers;
using HexControl.Core.Buffers.Extensions;
using HexControl.PatternLanguage.AST;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Patterns;
using HexControl.PatternLanguage.Types;

namespace HexControl.PatternLanguage
{
    public static class ListExtensions
    {

        public static T? PopLast<T>(this IList<T> list)
        {
            var last = list.LastOrDefault();
            if (last is null)
            {
                return default;
            }

            list.RemoveAt(list.Count - 1);
            return last;
        }
    }

    public enum DangerousFunctionPermission
    {
        Ask,
        Deny,
        Allow
    };

    internal enum ControlFlowStatement
    {
        None,
        Continue,
        Break,
        Return
    };
    
    public class Evaluator
    {
        public IEnumerable<PatternData>? Evaluate(ParsedLanguage parsed)
        {
            _stack.Clear();
            _customFunctions.Clear();
            _scopes.Clear();
            _aborted = false;

            if (_allowDangerousFunctions == DangerousFunctionPermission.Deny)
            {
                _allowDangerousFunctions = DangerousFunctionPermission.Ask;
            }

            _dangerousFunctionCalled = false;
            
            CurrentOffset = 0x00;
            _currentPatternCount = 0;

            _customFunctionDefinitions.Clear();

            var patterns = new List<PatternData>();

            //try {
            SetCurrentControlFlowStatement(ControlFlowStatement.None);
            PushScope(null, patterns);

            foreach (var node in parsed.Nodes)
            {
                if (node is ASTNodeTypeDecl)
                {
                    ; // Don't create patterns from type declarations
                }
                else if (node is ASTNodeFunctionCall)
                {
                    node.Evaluate(this);
                }
                else if (node is ASTNodeFunctionDefinition)
                {
                    _customFunctionDefinitions.Add(node.Evaluate(this));
                }
                else if (node is ASTNodeVariableDecl varDeclNode)
                {
                    var pattern = node.CreatePatterns(this)[0];

                    if (varDeclNode.GetPlacementOffset() is null)
                    {
                        var type = varDeclNode.Type.Evaluate(this);

                        var name = pattern.VariableName;
                        CreateVariable(name, type, null, varDeclNode.IsOutVariable());

                        if (varDeclNode.IsInVariable() && _inVariables.ContainsKey(name))
                        {
                            SetVariable(name, _inVariables[name]);
                        }
                    }
                    else
                    {
                        patterns.Add(pattern);
                    }
                }
                else
                {
                    var newPatterns = node.CreatePatterns(this);
                    patterns.AddRange(newPatterns);
                }
            }

            if (_customFunctions.ContainsKey("main"))
            {
                var mainFunction = _customFunctions["main"];

                if (mainFunction.ParameterCount > 0)
                {
                    throw new Exception("main function may not accept any arguments");
                }

                var result = mainFunction.Body(this, Array.Empty<Literal>());
                if (result is not null)
                {
                    var returnCode = result.ToSignedLong();

                    if (returnCode != 0)
                    {
                        throw new Exception($"non-success value returned from main: {returnCode}");
                    }
                }
            }

            PopScope();

            return patterns;
        }
        
        public record struct Scope {
            public PatternData? Parent { get; init; }
            public List<PatternData> Entries { get; init; } = new();
        };

        public void PushScope(PatternData? parent, List<PatternData> scope)
        {
            if (_scopes.Count > GetEvaluationDepth())
            {
                //LogConsole.abortEvaluation($"evaluation depth exceeded set limit of {getEvaluationDepth()}");
                throw new Exception($"evaluation depth exceeded set limit of {GetEvaluationDepth()}");
            }

            HandleAbort();

            _scopes.Add(new Scope { Parent = parent, Entries = scope });
        }

        public void PopScope()
        {
            _scopes.PopLast();
        }

        public Scope GetScope(int index)
        {
            return _scopes[_scopes.Count - 1 + index];
        }

        public Scope GetGlobalScope()
        {
            return _scopes.First();
        }

        public int GetScopeCount()
        {
            return _scopes.Count;
        }

        public bool IsGlobalScope()
        {
            return _scopes.Count == 1;
        }

        public void SetBuffer(BaseBuffer provider)
        {
            _buffer = provider;
        }

        public void SetInVariables(Dictionary<string, Literal> inVariables) {
            _inVariables = inVariables;
        }

        private Dictionary<string, Literal> GetOutVariables() {
            var result = new Dictionary<string, Literal>();

            foreach (var (name, offset) in _outVariables) {
                result.Add(name, GetStack()[(int)offset]);
            }

            return result;
        }

        public BaseBuffer GetBuffer() {
            return _buffer;
        }

        public void SetDefaultEndian(Endianess endian)
        {
            _defaultEndian = endian;
        }

        public Endianess GetDefaultEndian() {
            return _defaultEndian;
        }

        public void SetEvaluationDepth(long evalDepth)
        {
            _evalDepth = evalDepth;
        }

        public long GetEvaluationDepth() {
            return _evalDepth;
        }

        public void SetArrayLimit(long arrayLimit)
        {
            _arrayLimit = arrayLimit;
        }

        public long GetArrayLimit() {
            return _arrayLimit;
        }

        public void SetPatternLimit(long limit)
        {
            _patternLimit = limit;
        }

        public long GetPatternLimit()
        {
            return _patternLimit;
        }

        public long GetPatternCount()
        {
            return _currentPatternCount;
        }

        public void SetLoopLimit(long limit)
        {
            _loopLimit = limit;
        }

        public long GetLoopLimit()
        {
            return _loopLimit;
        }

        public long DataOffset() { return CurrentOffset; }

        // TODO: custom function
        public bool AddCustomFunction(string name, int numParams, PatternFunctionBody function)
        {
            _customFunctions.Add(name, new ContentRegistry.PatternLanguage.Function((uint)numParams, function, false));

            return true;
        }

        public Dictionary<string, ContentRegistry.PatternLanguage.Function> GetCustomFunctions()
        {
            return _customFunctions;
        }

        public List<Literal> GetStack()
        {
            return _stack;
        }

        internal void CreateVariable(string name, ASTNode type, Literal? value = null, bool outVariable = false)
        {
            var variables = GetScope(0).Entries;
            foreach (var variable in variables)
            {
                if (variable.VariableName == name)
                {
                    throw new Exception($"variable with name '{name}' already exists");
                }
            }

            var startOffset = DataOffset();
            var pattern = type.CreatePatterns(this).FirstOrDefault();
            CurrentOffset = startOffset;

            if (pattern is null)
            {
                // Handle auto variables
                if (value is null)
                {
                    throw new Exception("cannot determine type of auto variable"); // type
                }

                if (value is UInt64Literal)
                {
                    pattern = new PatternDataUnsigned(0, sizeof(ulong), this);
                }
                else if (value is Int64Literal)
                {
                    pattern = new PatternDataSigned(0, sizeof(long), this);
                }
                else if (value is DoubleLiteral)
                {
                    pattern = new PatternDataFloat(0, sizeof(double), this);
                }
                else if (value is BoolLiteral)
                {
                    pattern = new PatternDataBoolean(0, this);
                }
                else if (value is CharLiteral)
                {
                    pattern = new PatternDataCharacter16(0, this);
                }
                else if (value is Char16Literal)
                {
                    pattern = new PatternDataCharacter16(0, this);
                }
                else if (value is PatternDataLiteral pat)
                {
                    pattern = pat.Value.Clone();
                }
                else if (value is StringLiteral)
                {
                    pattern = new PatternDataString(0, 1, this);
                }
                else
                {
                    throw new Exception("builtin unreachable");
                    //__builtin_unreachable();
                }

            }

            pattern.VariableName = name;
            pattern.Local = true;
            pattern.Offset = GetStack().Count;

            // TODO: what doe this do?
            GetStack().Add(null!);
            variables.Add(pattern);

            if (outVariable)
            {
                _outVariables[name] = pattern.Offset;
            }
        }

        public void SetVariable(string name, Literal value)
        {
            PatternData? pattern = null;

            var variables = GetScope(0).Entries;
            foreach (var variable in variables)
            {
                if (variable.VariableName == name)
                {
                    pattern = variable;
                    break;
                }
            }

            if (pattern is null)
            {
                variables = GetGlobalScope().Entries;
                foreach (var variable in variables)
                {
                    if (variable.VariableName == name)
                    {
                        if (!variable.Local)
                        {
                            throw new Exception(
                                $"cannot modify global variable '{name}' which has been placed in memory");
                        }

                        pattern = variable;
                        break;
                    }
                }
            }

            if (pattern is null)
            {
                throw new Exception($"no variable with name '{name}' found");
            }

            //var val = value.Value;
            Literal? castedLiteral = null;
            if (value is DoubleLiteral doubleVal)
            {
                if (pattern is PatternDataUnsigned)
                {
                    castedLiteral = Bitmask(doubleVal.ToUnsignedLong(), (byte)pattern.Size);
                }
                else if (pattern is PatternDataSigned)
                {
                    castedLiteral = Bitmask(doubleVal.ToSignedLong(), (byte)pattern.Size);
                }
                else if (pattern is PatternDataFloat)
                {
                    castedLiteral = pattern.Size == sizeof(float) ? (double)(float)doubleVal.Value : doubleVal;
                }
                else
                {
                    throw new Exception($"cannot cast type 'double' to type '{pattern.TypeName}'");
                }
            }
            else if (value is StringLiteral stringVal)
            {
                if (pattern is PatternDataString)
                {
                    castedLiteral = stringVal;
                }
                else
                {
                    throw new Exception($"cannot cast type 'string' to type '{pattern.TypeName}'");
                }
            }
            else if (value is PatternDataLiteral patternVal)
            {
                if (patternVal.Value.TypeName == pattern.TypeName)
                {
                    castedLiteral = patternVal;
                }
                else
                {
                    throw new Exception(
                        $"cannot cast type '{patternVal.Value.TypeName}' to type '{pattern.TypeName}'");
                }
            }
            else if (value is UInt64Literal or Int64Literal)
            {
                if (pattern is PatternDataUnsigned or PatternDataEnum)
                {
                    castedLiteral = Bitmask(value.ToUnsignedLong(), (byte)pattern.Size);
                }
                else if (pattern is PatternDataSigned)
                {
                    castedLiteral = Bitmask(value.ToSignedLong(), (byte)pattern.Size);
                }
                else if (pattern is PatternDataCharacter)
                {
                    castedLiteral = (AsciiChar)(byte)value.ToUnsignedLong();
                }
                else if (pattern is PatternDataCharacter16)
                {
                    castedLiteral = (char)value.ToUnsignedLong();
                }
                else if (pattern is PatternDataBoolean)
                {
                    castedLiteral = value.ToSignedLong() != 0;
                }
                else if (pattern is PatternDataFloat)
                {
                    if (value is Int64Literal)
                    {
                        castedLiteral = pattern.Size == sizeof(float) ? (float)value.ToSignedLong() : value.ToSignedLong();
                    }
                    else
                    {
                        castedLiteral = pattern.Size == sizeof(float) ? (float)value.ToUnsignedLong() : value.ToUnsignedLong();
                    }

                }
                else
                {
                    throw new Exception($"cannot cast integer literal to type '{pattern.TypeName}'");
                }
            }
            
            if (castedLiteral is null)
            {
                throw new Exception("casted literal is null");
            }

            GetStack()[(int)pattern.Offset] = castedLiteral;
        }

        private static long Bitmask(long input, byte size)
        {
            var mask = long.MaxValue;
            return input & (mask >> (64 - size * 8));
        }

        private static ulong Bitmask(ulong input, byte size)
        {
            var mask = ulong.MaxValue;
            return input & (mask >> (64 - size * 8));
        }

        public void Abort()
        {
            _aborted = true;
        }

        public void HandleAbort()
        {
            if (_aborted)
            {
                //LogConsole.abortEvaluation("evaluation aborted by user");
                throw new Exception("evaluation aborted by user");
            }
        }

        public Literal? GetEnvVariable(string name) {
            if (_envVariables.ContainsKey(name))
            {
                return _envVariables[name];
            }
            else
            {
                return null;
            }
        }

        public void SetEnvVariable(string name, Literal value)
        {
            _envVariables[name] = value;
        }

        public bool HasDangerousFunctionBeenCalled() {
            return _dangerousFunctionCalled;
        }

        public void DangerousFunctionCalled()
        {
            _dangerousFunctionCalled = true;
        }

        public void AllowDangerousFunctions(bool allow)
        {
            _allowDangerousFunctions = allow ? DangerousFunctionPermission.Allow : DangerousFunctionPermission.Deny;
            _dangerousFunctionCalled = false;
        }

        public DangerousFunctionPermission GetDangerousFunctionPermission() {
            return _allowDangerousFunctions;
        }

        internal void SetCurrentControlFlowStatement(ControlFlowStatement statement)
        {
            _currentControlFlowStatement = statement;
        }

        internal ControlFlowStatement GetCurrentControlFlowStatement() {
            return _currentControlFlowStatement;
        }

        public void PatternCreated()
        {

        }
        public void PatternDestroyed()
        {

        }

        public long CurrentOffset { get; set; } = 0;

        private BaseBuffer _buffer = null;

        private Endianess _defaultEndian = Endianess.Native;
        private long _evalDepth;
        private long _arrayLimit;
        private long _patternLimit;
        private long _loopLimit = 9999999;

        private long _currentPatternCount;

        private bool _aborted;

        private readonly List<Scope> _scopes = new();
        private readonly Dictionary<string, ContentRegistry.PatternLanguage.Function> _customFunctions = new();
        private readonly List<ASTNode> _customFunctionDefinitions = new();
        private readonly List<Literal> _stack = new();

        private readonly Dictionary<string, Literal> _envVariables = new();
        private Dictionary<string, Literal> _inVariables = new();
        private readonly Dictionary<string, long> _outVariables = new();

        private bool _dangerousFunctionCalled = false;
        private DangerousFunctionPermission _allowDangerousFunctions = DangerousFunctionPermission.Ask;
        private ControlFlowStatement _currentControlFlowStatement;

        public class PatternCreationLimiter
        {

        }
    }
}