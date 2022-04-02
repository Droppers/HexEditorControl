using System;
using System.Collections.Generic;
using System.Linq;
using HexControl.Core.Buffers;
using HexControl.Core.Helpers;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeArrayVariableDecl : AttributableASTNode
{
    private readonly string _name;
    private readonly ASTNode? _placementOffset;
    private readonly ASTNode? _size;

    private readonly ASTNode _type;

    private bool _inlined;

    public ASTNodeArrayVariableDecl(string name, ASTNode type, ASTNode? size, ASTNode? placementOffset = null)
    {
        _name = name;
        _type = type;
        _size = size;
        _placementOffset = placementOffset;
    }

    private ASTNodeArrayVariableDecl(ASTNodeArrayVariableDecl other) : base(other)
    {
        _name = other._name;
        _type = other._type.Clone();
        _size = other._size?.Clone();
        _placementOffset = other._placementOffset?.Clone();
    }

    public override bool MultiPattern => false;

    public override ASTNode Clone() => new ASTNodeArrayVariableDecl(this);

    public override IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator)
    {
        return new[] {CreatePattern(evaluator)};
    }

    public override PatternData CreatePattern(Evaluator evaluator)
    {
        if (_placementOffset is not null)
        {
            var offsetNode = (ASTNodeLiteral)_placementOffset.Evaluate(evaluator);
            evaluator.CurrentOffset = offsetNode.Literal switch
            {
                StringLiteral => throw new Exception("placement offset cannot be a string"),
                PatternDataLiteral => throw new Exception("placement offset cannot be a custom type"),
                _ => (int)offsetNode.Literal.ToInt128()
            };
        }

        var type = _type.Evaluate(evaluator);

        PatternData pattern;
        if (type is ASTNodeBuiltinType)
        {
            pattern = CreateStaticArray(evaluator);
        }
        else if (type is AttributableASTNode attributable)
        {
            var attributes = attributable.Attributes;
            var isStaticType = attributes.Any(a => a.Attribute == "static" && a.Value is not null);
            pattern = isStaticType ? CreateStaticArray(evaluator) : CreateDynamicArray(evaluator);
        }
        else
        {
            throw new Exception("invalid type used in array");
        }

        ApplyVariableAttributes(evaluator, this, pattern);


        pattern.VariableName = _name;
        pattern.StaticData = StaticData;
        return pattern;
    }

    private PatternData CreateStaticArray(Evaluator evaluator)
    {
        var startOffset = evaluator.CurrentOffset;
        var templatePattern = _type.CreatePattern(evaluator);
        evaluator.CurrentOffset = startOffset;

        var entryCount = 0;
        if (_size is not null)
        {
            var sizeNode = _size.Evaluate(evaluator);

            if (sizeNode is ASTNodeLiteral literalNode)
            {
                entryCount = literalNode.Literal switch
                {
                    StringLiteral => throw new Exception("cannot use string to index array"),
                    PatternDataLiteral => throw new Exception("cannot use custom type to index array"),
                    _ => (int)literalNode.Literal.ToInt128()
                };
            }
            else if (sizeNode is ASTNodeWhileStatement whileStatement)
            {
                while (whileStatement.EvaluateCondition(evaluator))
                {
                    entryCount++;
                    evaluator.CurrentOffset += templatePattern.Size;
                    evaluator.HandleAbort();
                }
            }
        }
        else
        {
            // TODO: will this even work? what is templatepattern.Size? Since these arrays are unsized
            var buffer = new byte[templatePattern.Size];
            //std::vector<u8> buffer(templatePattern->Size);
            while (true)
            {
                if (evaluator.CurrentOffset >= evaluator.Buffer.Length - buffer.Length)
                {
                    throw new Exception("reached end of file before finding end of unsized array");
                }

                evaluator.Buffer.Read(buffer, evaluator.CurrentOffset);
                evaluator.CurrentOffset += buffer.Length;

                entryCount++;

                var reachedEnd = true;
                foreach (var @byte in buffer)
                {
                    if (@byte != 0x00)
                    {
                        reachedEnd = false;
                        break;
                    }
                }

                if (reachedEnd)
                {
                    break;
                }

                evaluator.HandleAbort();
            }
        }

        PatternData outputPattern = templatePattern switch
        {
            PatternDataPadding => new PatternDataPadding(startOffset, 0, evaluator),
            PatternDataCharacter => new PatternDataString(startOffset, 0, evaluator),
            PatternDataCharacter16 => new PatternDataString16(startOffset, 0, evaluator),
            _ => new PatternDataStaticArray(startOffset, 0, evaluator)
            {
                Template = templatePattern.Clone(), EntryCount = entryCount
            }
        };

        outputPattern.Endian = templatePattern.Endian;
        outputPattern.Color = templatePattern.Color;
        outputPattern.TypeNameIndex = templatePattern.TypeNameIndex;
        outputPattern.Size = templatePattern.Size * entryCount;

        evaluator.CurrentOffset = startOffset + outputPattern.Size;

        return outputPattern;
    }

    private static long AddDynamicArrayEntry(List<PatternData> entries, Endianess? endianess, PatternData pattern)
    {
        pattern.ArrayIndex = entries.Count;
        pattern.Endian = endianess;
        entries.Add(pattern);
        return pattern.Size;
    }

    private static long DiscardDynamicArrayEntry(List<PatternData> entries)
    {
        var lastEntry = entries[^1];
        entries.RemoveAt(entries.Count - 1);
        return lastEntry.Size;
    }

    private PatternData CreateDynamicArray(Evaluator evaluator)
    {
        var arrayPattern = new PatternDataDynamicArray(evaluator.CurrentOffset, 0, evaluator);

        var endian = arrayPattern.Endian;
        var entries = ObjectPool<List<PatternData>>.Shared.Rent();
        long size = 0;
        if (_size is not null)
        {
            var sizeNode = _size.Evaluate(evaluator);

            if (sizeNode is ASTNodeLiteral literalNode)
            {
                var entryCount = literalNode.Literal switch
                {
                    StringLiteral => throw new Exception("cannot use string to index array"),
                    PatternDataLiteral => throw new Exception("cannot use custom type to index array"),
                    _ => (int)literalNode.Literal.ToInt128()
                };

                var limit = evaluator.ArrayLimit;
                if (entryCount > limit)
                {
                    throw new Exception($"array grew past set limit of {limit}");
                }

                for (var i = 0; i < entryCount; i++)
                {
                    var pattern = _type.CreatePattern(evaluator);

                    if (pattern is not null)
                    {
                        size += AddDynamicArrayEntry(entries, endian, pattern);
                    }

                    var ctrlFlow = evaluator.CurrentControlFlowStatement;
                    if (ctrlFlow == ControlFlowStatement.Break)
                    {
                        break;
                    }

                    if (ctrlFlow == ControlFlowStatement.Continue)
                    {
                        size -= DiscardDynamicArrayEntry(entries);
                    }
                }
            }
            else if (sizeNode is ASTNodeWhileStatement whileStatement)
            {
                while (whileStatement.EvaluateCondition(evaluator))
                {
                    var limit = evaluator.ArrayLimit;
                    if (entries.Count > limit)
                    {
                        throw new Exception($"array grew past set limit of {limit}");
                    }

                    var pattern = _type.CreatePattern(evaluator);

                    if (pattern is not null)
                    {
                        size += AddDynamicArrayEntry(entries, endian, pattern);
                    }

                    var ctrlFlow = evaluator.CurrentControlFlowStatement;
                    if (ctrlFlow == ControlFlowStatement.Break)
                    {
                        break;
                    }

                    if (ctrlFlow == ControlFlowStatement.Continue)
                    {
                        size -= DiscardDynamicArrayEntry(entries);
                    }
                }
            }
        }
        else
        {
            while (true)
            {
                var limit = evaluator.ArrayLimit;
                if (entries.Count > limit)
                {
                    throw new Exception($"array grew past set limit of {limit}");
                }

                var pattern = _type.CreatePattern(evaluator);

                if (pattern is null)
                {
                    continue;
                }

                if (evaluator.CurrentOffset >= evaluator.Buffer.Length - pattern.Size)
                {
                    throw new Exception("reached end of file before finding end of unsized array");
                }

                size += AddDynamicArrayEntry(entries, endian, pattern);

                var ctrlFlow = evaluator.CurrentControlFlowStatement;
                if (ctrlFlow == ControlFlowStatement.Break)
                {
                    break;
                }

                if (ctrlFlow == ControlFlowStatement.Continue)
                {
                    size -= DiscardDynamicArrayEntry(entries);
                    continue;
                }


                var buffer = ExactArrayPool<byte>.Shared.Rent((int)pattern.Size);
                try
                {
                    evaluator.Buffer.Read(buffer, evaluator.CurrentOffset - pattern.Size);
                    var reachedEnd = true;
                    for (var i = 0; i < buffer.Length; i++)
                    {
                        var @byte = buffer[i];
                        if (@byte != 0x00)
                        {
                            reachedEnd = false;
                            break;
                        }
                    }

                    if (reachedEnd)
                    {
                        break;
                    }
                }
                finally
                {
                    ExactArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }
        
        arrayPattern.SetEntries(entries.ToArray());
        arrayPattern.Size = size;

        // Return to pool
        entries.Clear();
        ObjectPool<List<PatternData>>.Shared.Return(entries);

        // Copy type from first entry to the array
        if (arrayPattern.Entries.Count > 0)
        {
            arrayPattern.TypeNameIndex = arrayPattern.Entries[0].TypeNameIndex;
        }

        return arrayPattern;
    }

    public void SetInlined(bool inlined)
    {
        _inlined = inlined;
    }
}