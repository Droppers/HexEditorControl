using System;
using System.Collections.Generic;
using System.Linq;
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

    public override ASTNode Clone() => new ASTNodeArrayVariableDecl(this);

    public override IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator)
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
        return new[] {pattern};
    }

    private PatternData CreateStaticArray(Evaluator evaluator)
    {
        var startOffset = evaluator.CurrentOffset;

        var templatePattern = _type.CreatePatterns(evaluator)[0];

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

                evaluator.Buffer.Read(evaluator.CurrentOffset, buffer);
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

        PatternData outputPattern;
        if (templatePattern is PatternDataPadding)
        {
            outputPattern = new PatternDataPadding(startOffset, 0, evaluator);
        }
        else if (templatePattern is PatternDataCharacter)
        {
            outputPattern = new PatternDataString(startOffset, 0, evaluator);
        }
        else if (templatePattern is PatternDataCharacter16)
        {
            outputPattern = new PatternDataString16(startOffset, 0, evaluator);
        }
        else
        {
            var arrayPattern = new PatternDataStaticArray(startOffset, 0, evaluator)
            {
                Template = templatePattern.Clone(),
                EntryCount = entryCount
            };
            outputPattern = arrayPattern;
        }

        outputPattern.VariableName = _name;
        outputPattern.Endian = templatePattern.Endian;
        outputPattern.Color = templatePattern.Color;
        outputPattern.TypeName = templatePattern.TypeName;
        outputPattern.Size = templatePattern.Size * entryCount;

        evaluator.CurrentOffset = startOffset + outputPattern.Size;

        return outputPattern;
    }

    private PatternData CreateDynamicArray(Evaluator evaluator)
    {
        var arrayPattern = new PatternDataDynamicArray(evaluator.CurrentOffset, 0, evaluator)
        {
            VariableName = _name
        };

        var entries = new List<PatternData>();

        long size = 0;
        long entryIndex = 0;

        var addEntry = (PatternData pattern) =>
        {
            pattern.VariableName = $"[{entryIndex}]";
            pattern.Endian = arrayPattern.Endian;
            entries.Add(pattern);

            size += pattern.Size;
            entryIndex++;

            evaluator.HandleAbort();
        };

        var discardEntry = () =>
        {
            entries.RemoveAt(entries.Count - 1);
            entryIndex--;
        };

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
                    var patterns = _type.CreatePatterns(evaluator);

                    if (patterns.Count > 0)
                    {
                        addEntry(patterns[0]);
                    }

                    var ctrlFlow = evaluator.CurrentControlFlowStatement;
                    if (ctrlFlow == ControlFlowStatement.Break)
                    {
                        break;
                    }

                    if (ctrlFlow == ControlFlowStatement.Continue)
                    {
                        discardEntry();
                    }
                }
            }
            else if (sizeNode is ASTNodeWhileStatement whileStatement)
            {
                while (whileStatement.EvaluateCondition(evaluator))
                {
                    var limit = evaluator.ArrayLimit;
                    if (entryIndex > limit)
                    {
                        throw new Exception($"array grew past set limit of {limit}");
                    }

                    var patterns = _type.CreatePatterns(evaluator);

                    if (patterns.Count > 0)
                    {
                        addEntry(patterns[0]);
                    }

                    var ctrlFlow = evaluator.CurrentControlFlowStatement;
                    if (ctrlFlow == ControlFlowStatement.Break)
                    {
                        break;
                    }

                    if (ctrlFlow == ControlFlowStatement.Continue)
                    {
                        discardEntry();
                    }
                }
            }
        }
        else
        {
            while (true)
            {
                var limit = evaluator.ArrayLimit;
                if (entryIndex > limit)
                {
                    throw new Exception($"array grew past set limit of {limit}");
                }

                var patterns = _type.CreatePatterns(evaluator);

                if (patterns.Count > 0)
                {
                    var pattern = patterns[0];

                    var buffer = new byte[pattern.Size];

                    if (evaluator.CurrentOffset >= evaluator.Buffer.Length - buffer.Length)
                    {
                        throw new Exception("reached end of file before finding end of unsized array");
                    }

                    addEntry(pattern);

                    var ctrlFlow = evaluator.CurrentControlFlowStatement;
                    if (ctrlFlow == ControlFlowStatement.Break)
                    {
                        break;
                    }

                    if (ctrlFlow == ControlFlowStatement.Continue)
                    {
                        discardEntry();
                        continue;
                    }

                    evaluator.Buffer.Read(evaluator.CurrentOffset - pattern.Size, buffer);
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
                }
            }
        }

        arrayPattern.Entries = entries;
        arrayPattern.Size = size;

        var tmpEntries = arrayPattern.Entries;
        if (tmpEntries.Count > 0)
        {
            arrayPattern.TypeName = tmpEntries.First().TypeName;
        }

        return arrayPattern;
    }

    public void SetInlined(bool inlined)
    {
        _inlined = inlined;
    }
}