using System;
using System.Collections.Generic;
using System.Linq;
using HexControl.Core;
using HexControl.Core.Buffers;
using HexControl.Core.Buffers.Extensions;
using HexControl.Core.Helpers;
using HexControl.Core.Numerics;
using HexControl.PatternLanguage.AST;
using HexControl.PatternLanguage.Functions;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Patterns;
using HexControl.PatternLanguage.Types;

namespace HexControl.PatternLanguage;

public enum DangerousFunctionPermission
{
    Ask,
    Deny,
    Allow
}

internal enum ControlFlowStatement
{
    None,
    Continue,
    Break,
    Return
}

public class Evaluator
{
    private static readonly ObjectPool<List<PatternData>> ScopePool = new(10);

    private readonly Dictionary<string, Literal> _envVariables;
    private readonly Dictionary<string, Literal> _inVariables;
    private readonly Dictionary<string, long> _outVariables;

    private readonly List<Scope> _scopes;
    private readonly List<Literal> _stack;

    public Evaluator()
    {
        CustomFunctions = new FunctionRegistry();
        _envVariables = new Dictionary<string, Literal>();
        _inVariables = new Dictionary<string, Literal>();
        _outVariables = new Dictionary<string, long>();

        _scopes = new List<Scope>();
        _stack = new List<Literal>();
    }

    public BaseBuffer Buffer { get; private set; } = null!;
    public long CurrentOffset { get; set; }

    internal IReadOnlyList<Scope> Scopes => _scopes;

    internal Scope GlobalScope => _scopes.First();

    internal bool IsGlobalScope => _scopes.Count == 1;

    private IReadOnlyDictionary<string, Literal> OutVariables
    {
        get
        {
            var result = new Dictionary<string, Literal>();

            foreach (var (name, offset) in _outVariables)
            {
                result.Add(name, _stack[(int)offset]);
            }

            return result;
        }
    }

    public Endianess DefaultEndian { get; set; } = Endianess.Native;

    public long EvaluationDepth { get; set; } = 20000;

    public long ArrayLimit { get; set; } = 20000;

    public long PatternLimit { get; set; }

    public long PatternCount { get; set; }

    public long LoopLimit { get; set; } = 20000;

    public Func<DangerousFunctionPermission>? AskDangerousFunctionExecution { get; set; }

    public IReadOnlyList<Literal> Stack => _stack;

    public bool HasDangerousFunctionBeenCalled { get; private set; }

    public bool AllowDangerousFunctions
    {
        get => DangerousFunctionPermission is DangerousFunctionPermission.Allow;
        set
        {
            DangerousFunctionPermission = value ? DangerousFunctionPermission.Allow : DangerousFunctionPermission.Deny;
            HasDangerousFunctionBeenCalled = false;
        }
    }

    public DangerousFunctionPermission DangerousFunctionPermission { get; private set; } =
        DangerousFunctionPermission.Ask;

    internal ControlFlowStatement CurrentControlFlowStatement { get; set; }

    public FunctionRegistry CustomFunctions { get; }

    public IEnumerable<PatternData>? Evaluate(Document document, ParsedLanguage parsed)
    {
        var patterns = Evaluate(document.Buffer, parsed);
        throw new NotImplementedException();
        return patterns;
    }

    public IEnumerable<PatternData>? Evaluate(BaseBuffer buffer, ParsedLanguage parsed)
    {
        Buffer = buffer;

        _stack.Clear();
        CustomFunctions.Clear();
        _scopes.Clear();

        if (DangerousFunctionPermission is DangerousFunctionPermission.Deny)
        {
            DangerousFunctionPermission = DangerousFunctionPermission.Ask;
        }

        HasDangerousFunctionBeenCalled = false;
        CurrentOffset = 0;

        var patterns = new List<PatternData>();

        //try {
        CurrentControlFlowStatement = ControlFlowStatement.None;
        PushScope(null, patterns);

        foreach (var node in parsed.Nodes)
        {
            switch (node)
            {
                case ASTNodeTypeDecl:
                    // Don't create patterns from type declarations
                    break;
                case ASTNodeFunctionCall:
                case ASTNodeFunctionDefinition:
                    node.Evaluate(this);
                    break;
                case ASTNodeVariableDecl varDeclNode:
                    var pattern = node.CreatePatterns(this)[0];

                    if (varDeclNode.PlacementOffset is null)
                    {
                        var type = varDeclNode.Type.Evaluate(this);

                        var name = pattern.VariableName;
                        if (name is null)
                        {
                            throw new Exception("Variable name cannot be null.");
                        }

                        CreateVariable(name, type, null, varDeclNode.IsOutVariable);

                        if (varDeclNode.IsInVariable && _inVariables.ContainsKey(name))
                        {
                            SetVariable(name, _inVariables[name]);
                        }
                    }
                    else
                    {
                        patterns.Add(pattern);
                    }

                    break;
                default:
                    var newPatterns = node.CreatePatterns(this);
                    patterns.AddRange(newPatterns);
                    break;
            }
        }

        if (CustomFunctions.Functions.TryGetValue("main", out var mainFunction))
        {
            if (mainFunction.ParameterCount != FunctionParameterCount.None)
            {
                throw new Exception("main function may not accept any arguments");
            }

            var result = mainFunction.Body(this, Array.Empty<Literal>());
            if (result is not null)
            {
                var returnCode = result.ToInt128();

                if (returnCode != 0)
                {
                    throw new Exception($"non-success value returned from main: {returnCode}");
                }
            }
        }

        PopScope();

        return patterns;
    }

    internal Scope PushScope()
    {
        var entries = ScopePool.Rent();
        entries.Clear();
        return PushScope(null, entries, true);
    }

    internal Scope PushScope(PatternData? parent)
    {
        var entries = ScopePool.Rent();
        entries.Clear();
        return PushScope(parent, entries, true);
    }

    internal Scope PushScope(List<PatternData> entries) => PushScope(null, entries);

    private Scope PushScope(PatternData? parent, List<PatternData> entries, bool rented = false)
    {
        if (_scopes.Count > EvaluationDepth)
        {
            throw new Exception($"evaluation depth exceeded set limit of {EvaluationDepth}");
        }

        HandleAbort();

        var scope = new Scope {Parent = parent, Entries = entries, Rented = rented, InitialCount = entries.Count};
        _scopes.Add(scope);
        return scope;
    }

    internal void HandleAbort()
    {
        // TODO: cancellation token
    }

    public void PopScope(bool cleanup = false)
    {
        var scope = _scopes[^1];
        try
        {
            _scopes.RemoveAt(_scopes.Count - 1);

            if (!cleanup || scope.Entries.Count <= scope.InitialCount)
            {
                return;
            }

            var stackSize = _stack.Count;
            for (var i = scope.InitialCount; i < scope.Entries.Count; i++)
            {
                if (stackSize < 0)
                {
                    throw new Exception("stack pointer underflow!");
                }

                scope.Entries.RemoveAt(scope.Entries.Count - 1);
                _stack.RemoveAt(_stack.Count - 1);
            }
        }
        finally
        {
            if (scope.Rented)
            {
                ScopePool.Return(scope.Entries);
            }
        }
    }

    internal Scope ScopeAt(int index) => _scopes[_scopes.Count - 1 + index];

    internal void CreateVariable(string name, ASTNode type, Literal? value = null, bool outVariable = false)
    {
        var variables = ScopeAt(0).Entries;
        foreach (var variable in variables)
        {
            if (variable.VariableName == name)
            {
                throw new Exception($"variable with name '{name}' already exists");
            }
        }

        var startOffset = CurrentOffset;
        var pattern = type.CreatePatterns(this).FirstOrDefault();
        CurrentOffset = startOffset;

        if (pattern is null)
        {
            switch (value)
            {
                // Handle auto variables
                case null:
                    throw new Exception("cannot determine type of auto variable"); // type
                case UInt128Literal:
                    pattern = new PatternDataUnsigned(0, sizeof(ulong), this);
                    break;
                case Int128Literal:
                    pattern = new PatternDataSigned(0, sizeof(long), this);
                    break;
                case DoubleLiteral:
                    pattern = new PatternDataFloat(0, sizeof(double), this);
                    break;
                case BoolLiteral:
                    pattern = new PatternDataBoolean(0, this);
                    break;
                case CharLiteral:
                case Char16Literal:
                    pattern = new PatternDataCharacter16(0, this);
                    break;
                case PatternDataLiteral pat:
                    pattern = pat.Value.Clone();
                    break;
                case StringLiteral:
                    pattern = new PatternDataString(0, 1, this);
                    break;
                default:
                    throw new Exception("builtin unreachable");
            }
        }

        pattern.VariableName = name;
        pattern.Local = true;
        pattern.Offset = _stack.Count;

        _stack.Add(null!);
        variables.Add(pattern);

        if (outVariable)
        {
            _outVariables[name] = pattern.Offset;
        }
    }

    public void SetVariable(string name, Literal value)
    {
        PatternData? pattern = null;

        var variables = ScopeAt(0).Entries;
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
            variables = GlobalScope.Entries;
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

        Literal? castedLiteral = null;
        if (value is DoubleLiteral doubleVal)
        {
            if (pattern is PatternDataUnsigned)
            {
                castedLiteral = Bitmask(doubleVal.ToUInt128(), (byte)pattern.Size);
            }
            else if (pattern is PatternDataSigned)
            {
                castedLiteral = Bitmask(doubleVal.ToInt128(), (byte)pattern.Size);
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
        else if (value is UInt128Literal or Int128Literal)
        {
            if (pattern is PatternDataUnsigned or PatternDataEnum)
            {
                castedLiteral = Bitmask(value.ToUInt128(), (byte)pattern.Size);
            }
            else if (pattern is PatternDataSigned)
            {
                castedLiteral = Bitmask(value.ToInt128(), (byte)pattern.Size);
            }
            else if (pattern is PatternDataCharacter)
            {
                castedLiteral = (AsciiChar)(byte)value.ToUInt128();
            }
            else if (pattern is PatternDataCharacter16)
            {
                castedLiteral = (char)value.ToUInt128();
            }
            else if (pattern is PatternDataBoolean)
            {
                castedLiteral = value.ToInt128() != 0;
            }
            else if (pattern is PatternDataFloat)
            {
                if (value is Int128Literal)
                {
                    castedLiteral = pattern.Size == sizeof(float) ? (float)value.ToInt128() : value.ToInt128();
                }
                else
                {
                    castedLiteral = pattern.Size == sizeof(float) ? (float)value.ToUInt128() : value.ToUInt128();
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

        _stack[(int)pattern.Offset] = castedLiteral;
    }

    private static Int128 Bitmask(Int128 input, byte size)
    {
        var mask = Int128.MaxValue;
        return input & (mask >> (128 - size * 8));
    }

    private static UInt128 Bitmask(UInt128 input, byte size)
    {
        var mask = UInt128.MaxValue;
        return input & (mask >> (128 - size * 8));
    }

    public void AddInVariable(string name, Literal value)
    {
        _inVariables[name] = value;
    }

    public void AddEnvVariable(string name, Literal value)
    {
        _envVariables[name] = value;
    }

    public Literal? GetEnvVariable(string name) => _envVariables.TryGetValue(name, out var literal) ? literal : null;

    public void DangerousFunctionCalled()
    {
        if (AskDangerousFunctionExecution is not null)
        {
            DangerousFunctionPermission = AskDangerousFunctionExecution.Invoke();
        }

        HasDangerousFunctionBeenCalled = true;
    }

    internal record struct Scope
    {
        public PatternData? Parent { get; init; }
        public List<PatternData> Entries { get; init; }
        public bool Rented { get; init; }
        public int InitialCount { get; init; }
    }
}