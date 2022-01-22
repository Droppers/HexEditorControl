using System;
using System.Collections.Generic;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeBitfield : AttributableASTNode
{
    private readonly List<(string, ASTNode)> _entries;

    public ASTNodeBitfield()
    {
        _entries = new List<(string, ASTNode)>();
    }

    private ASTNodeBitfield(ASTNodeBitfield other) : base(other)
    {
        _entries = new List<(string, ASTNode)>(other._entries.Count);
        foreach (var (name, entry) in other._entries)
        {
            _entries.Add((name, entry.Clone()));
        }
    }

    public override ASTNode Clone() => new ASTNodeBitfield(this);

    public void AddEntry(string name, ASTNode size)
    {
        _entries.Add((name, size));
    }

    public override IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator)
    {
        var pattern = new PatternDataBitfield(evaluator.CurrentOffset, 0, evaluator);

        byte bitOffset = 0;
        var fields = new List<PatternData>();

        evaluator.PushScope(pattern, fields);

        foreach (var (name, bitSizeNode) in _entries)
        {
            var literalNode = (ASTNodeLiteral)bitSizeNode.Evaluate(evaluator);
            var bitSize = literalNode.Literal switch
            {
                StringLiteral => throw new Exception("bitfield field size cannot be a string"), // this
                PatternDataLiteral => throw new Exception("bitfield field size cannot be a custom type"), // this
                _ => (byte)literalNode.Literal.ToUnsignedLong()
            };

            // If a field is named padding, it was created through a padding expression and only advances the bit position
            if (name != "padding")
            {
                var field = new PatternDataBitfieldField(evaluator.CurrentOffset, bitOffset, bitSize, pattern,
                    evaluator)
                {
                    VariableName = name
                };
                fields.Add(field);
            }

            bitOffset += bitSize;
        }

        evaluator.PopScope();

        pattern.Size = (bitOffset + 7) / 8;
        pattern.Fields = fields;

        evaluator.CurrentOffset += pattern.Size;

        return new [] {pattern};
    }
}