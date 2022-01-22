using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HexControl.Core.Buffers.Extensions;
using HexControl.PatternLanguage.Extensions;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Patterns;
using HexControl.PatternLanguage.Types;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeRValue : ASTNode
{
    private readonly Path _path;

    public ASTNodeRValue(Path path)
    {
        _path = path;
    }

    private ASTNodeRValue(ASTNodeRValue other) : base(other)
    {
        _path = other._path;
    }

    public override ASTNode Clone() => new ASTNodeRValue(this);

    public override ASTNode Evaluate(Evaluator evaluator)
    {
        if (_path.Values.Count == 1)
        {
            if (_path.Values.First() is "$")
            {
                return new ASTNodeLiteral(evaluator.CurrentOffset);
            }
        }

        var pattern = CreatePatterns(evaluator)[0];

        Literal literal;
        if (pattern is PatternDataUnsigned or PatternDataEnum)
        {
            literal = ReadValue<ulong>(evaluator, pattern) ?? 0;
        }
        else if (pattern is PatternDataSigned)
        {
            literal = ReadValue<long>(evaluator, pattern) ?? 0L;
            //value = hex::signExtend(pattern.Size * 8, value); // TODO: impl
        }
        else if (pattern is PatternDataFloat)
        {
            if (pattern.Size == sizeof(ushort))
            {
                //ushort value = 0;
                //ReadValue(value, pattern);
                //literal = double(float16ToFloat32(value));
                literal = ReadValue<double>(evaluator, pattern) ?? 0d;
            }
            else if (pattern.Size == sizeof(float))
            {
                literal = ReadValue<double>(evaluator, pattern) ?? 0d;
            }
            else if (pattern.Size == sizeof(double))
            {
                literal = ReadValue<double>(evaluator, pattern) ?? 0d;
            }
            else
            {
                throw
                    new Exception(
                        "invalid floating point type access"); // LogConsole::abortEvaluation("invalid floating point type access", this);
            }
        }
        else if (pattern is PatternDataCharacter)
        {
            literal = ReadValue<AsciiChar>(evaluator, pattern) ?? (AsciiChar)0;
        }
        else if (pattern is PatternDataCharacter16)
        {
            literal = ReadValue<char>(evaluator, pattern) ?? (char)0;
        }
        else if (pattern is PatternDataBoolean)
        {
            //bool value = false;
            literal = ReadValue<bool>(evaluator, pattern) ?? false;
            //literal = value;
        }
        else if (pattern is PatternDataString)
        {
            if (pattern.Local)
            {
                var literalVar = evaluator.GetStack()[(int)pattern.Offset];
                if (literalVar is not StringLiteral or CharLiteral or Char16Literal)
                {
                    throw new Exception($"cannot assign '{pattern.TypeName}' to string");
                    //LogConsole::abortEvaluation(hex::format("cannot assign '{}' to string", pattern->getTypeName()), this);
                }

                literal = literalVar.ToString()!;
            }
            else
            {
                var buffer = new byte[pattern.Size];
                evaluator.GetBuffer().Read(pattern.Offset, buffer);
                literal = Encoding.UTF8.GetString(buffer);
            }
        }
        else if (pattern is PatternDataBitfieldField bitfieldFieldPattern)
        {
            var value = ReadValue<long>(evaluator, pattern) ?? 0;
            //literal = u128(hex::extract(bitfieldFieldPattern->getBitOffset() + (bitfieldFieldPattern->getBitSize() - 1), bitfieldFieldPattern->getBitOffset(), value));
            literal = value & (1 << bitfieldFieldPattern.BitOffset);
        }
        else
        {
            literal = pattern.Clone();
        }

        var transformFunc = pattern.TransformFunction;
        if (transformFunc is not null)
        {
            var result = transformFunc.Invoke(evaluator, new[] {literal});
            literal = result ?? throw new Exception("transform function did not return a value");
        }

        return new ASTNodeLiteral(literal);
    }

    public override IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator)
    {
        var initialSearchScope = new List<PatternData>();
        PatternData? currentPattern = null;
        var scopeIndex = 0;

        if (!evaluator.IsGlobalScope())
        {
            initialSearchScope.AddRange(evaluator.GetGlobalScope().Entries);
        }

        initialSearchScope.AddRange(evaluator.GetScope(scopeIndex).Entries);

        // Readonly for safety
        IReadOnlyList<PatternData> searchScope = initialSearchScope;

        var shouldClone = true;
        foreach (var part in _path.Values)
        {
            if (part is string name)
            {
                if (name == "parent")
                {
                    scopeIndex--;

                    if (-scopeIndex >= evaluator.GetScopeCount())
                    {
                        throw new Exception("cannot access parent of global scope"); //this
                    }

                    searchScope = evaluator.GetScope(scopeIndex).Entries;
                    currentPattern = evaluator.GetScope(scopeIndex).Parent;
                    continue;
                }

                if (name == "this")
                {
                    searchScope = evaluator.GetScope(scopeIndex).Entries;
                    var currentParent = evaluator.GetScope(0).Parent;
                    currentPattern = currentParent ?? throw new Exception("invalid use of 'this' outside of struct-like type");
                    continue;
                }

                var found = false;

                foreach (var entry in searchScope)
                {
                    if (entry.VariableName == name)
                    {
                        currentPattern = entry;
                        found = true;
                        break;
                    }
                }

                if (name == "$")
                {
                    throw new Exception("invalid use of placeholder operator in rvalue");
                    //LogConsole.abortEvaluation("invalid use of placeholder operator in rvalue");
                }

                if (!found)
                {
                    throw new Exception($"no variable named '{name}' found");
                    //LogConsole.abortEvaluation(hex::format("no variable named '{}' found", name), this);
                }
            }
            else
            {
                // Array indexing
                var indexNode = (ASTNodeLiteral)((ASTNode)part).Evaluate(evaluator);
                var indexLiteral = indexNode.Literal;
                switch (indexLiteral)
                {
                    case StringLiteral:
                        throw new Exception("cannot use string to index array");
                    case PatternDataLiteral:
                        throw new Exception("cannot use custom type to index array");
                    default:
                    {
                        var arrayIndex = (int)indexLiteral.ToSignedLong();
                        switch (currentPattern)
                        {
                            case PatternDataDynamicArray when arrayIndex >= searchScope.Count || arrayIndex < 0:
                                throw new Exception("array index out of bounds");
                            //LogConsole::abortEvaluation("array index out of bounds", this);
                            case PatternDataDynamicArray:
                            {
                                currentPattern = searchScope[arrayIndex];
                                break;
                            }
                            case PatternDataStaticArray staticArrayPattern
                                when arrayIndex >= staticArrayPattern.EntryCount || arrayIndex < 0:
                                throw new Exception("array index out of bounds");
                            //LogConsole::abortEvaluation("array index out of bounds", this);
                            case PatternDataStaticArray staticArrayPattern:
                            {
                                shouldClone = false;
                                var newPattern = searchScope.First().Clone();
                                newPattern.Offset = (staticArrayPattern.Offset +
                                                     arrayIndex * staticArrayPattern.Template.Size);

                                currentPattern = newPattern;
                                break;
                            }
                            default:
                                throw new Exception("Unsupported array type.");
                        }

                        break;
                    }
                }
            }

            if (currentPattern is null)
            {
                break;
            }

            if (currentPattern is PatternDataPointer pointerPattern)
            {
                currentPattern = pointerPattern.PointedAtPattern;
            }

            PatternData? indexPattern;
            if (currentPattern.Local)
            {
                var stackLiteral = evaluator.GetStack()[(int)currentPattern.Offset];
                if (stackLiteral is PatternDataLiteral stackPattern)
                {
                    indexPattern = stackPattern.Value;
                }
                else
                {
                    return new [] {currentPattern};
                }
            }
            else
            {
                indexPattern = currentPattern;
            }

            if (indexPattern is PatternDataStruct structPattern)
            {
                searchScope = structPattern.Members;
            }
            else if (indexPattern is PatternDataUnion unionPattern)
            {
                searchScope = unionPattern.Members;
            }
            else if (indexPattern is PatternDataBitfield bitfieldPattern)
            {
                searchScope = bitfieldPattern.Fields;
            }
            else if (indexPattern is PatternDataDynamicArray dynamicArrayPattern)
            {
                searchScope = dynamicArrayPattern.Entries;
            }
            else if (indexPattern is PatternDataStaticArray staticArrayPattern)
            {
                searchScope = new List<PatternData> {staticArrayPattern.Template};
            }
        }

        if (currentPattern is null)
        {
            throw new Exception("cannot reference global scope");
            //LogConsole::abortEvaluation("cannot reference global scope", this);
        }

        var pattern = shouldClone ? currentPattern.Clone() : currentPattern;
        return new [] {pattern};
    }


    private static Literal ReadLocal(Evaluator evaluator, long offset) => evaluator.GetStack()[(int)offset];

    private static T? ReadValue<T>(Evaluator evaluator, PatternData data) where T : struct
    {
        T? value = null;
        var buffer = evaluator.GetBuffer();
        var offset = data.Offset;
        var size = (int)data.Size;
        var endian = data.Endian;

        // TODO: readlocal should have endian conversions
        if (typeof(T) == typeof(ulong))
        {
            value = (T)(object)(data.Local
                ? ReadLocal(evaluator, offset).ToUnsignedLong()
                : buffer.ReadUInt64(offset, size, endian));
        }
        else if (typeof(T) == typeof(long))
        {
            value = (T)(object)(data.Local
                ? ReadLocal(evaluator, offset).ToSignedLong()
                : buffer.ReadInt64(offset, size, endian));
        }
        else if (typeof(T) == typeof(bool))
        {
            value = (T)(object)(data.Local ? ReadLocal(evaluator, offset).ToBool() : buffer.ReadUByte(offset) != 0);
        }
        else if (typeof(T) == typeof(AsciiChar))
        {
            value = (T)(object)(data.Local ? ReadLocal(evaluator, offset).ToChar() : buffer.ReadUByte(offset));
        }
        else if (typeof(T) == typeof(char))
        {
            value = (T)(object)(data.Local
                ? ReadLocal(evaluator, offset).ToChar16()
                : buffer.ReadChar(offset, endian));
        }

        if (value is null)
        {
            throw new Exception("Literal is null.");
        }

        return value;
    }

    public class Path
    {
        public Path()
        {
            Values = new List<object>();
        }

        public Path(string path) : this()
        {
            Values.Add(path);
        }

        public List<object> Values { get; }
    }
}