using System;
using System.Collections.Generic;
using HexControl.Core.Buffers.Extensions;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeVariableVariableLengthDecl : AttributableASTNode
{
    public ASTNodeVariableVariableLengthDecl(string name, ASTNode type, ASTNode? placementOffset = null,
        bool inVariable = false,
        bool outVariable = false)
    {
        Name = name;
        Type = type;
        PlacementOffset = placementOffset;
        IsInVariable = inVariable;
        IsOutVariable = outVariable;
    }

    private ASTNodeVariableVariableLengthDecl(ASTNodeVariableVariableLengthDecl other) : base(other)
    {
        Name = other.Name;
        Type = other.Type.Clone();
        PlacementOffset = other.PlacementOffset?.Clone();
    }

    public override bool MultiPattern => false;

    public string Name { get; }
    public ASTNode Type { get; }

    public ASTNode? PlacementOffset { get; }

    public bool IsInVariable { get; }

    public bool IsOutVariable { get; }

    public override ASTNode Clone() => null;

    public override IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator)
    {
        return new[] {CreatePattern(evaluator)};
    }

    public override PatternData CreatePattern(Evaluator evaluator)
    {
        if (PlacementOffset is not null)
        {
            var offsetNode = (ASTNodeLiteral)PlacementOffset.Evaluate(evaluator);

            evaluator.CurrentOffset = offsetNode.Literal switch
            {
                StringLiteral => throw new Exception("placement offset cannot be a string"), // this
                PatternDataLiteral => throw new Exception("placement offset cannot be a custom type"), // this
                _ => offsetNode.Literal.ToInt64()
            };
        }

        var pattern = Type.CreatePattern(evaluator);
        pattern.VariableName = Name;

        ApplyVariableAttributes(evaluator, this, pattern);

        return pattern;
    }

    public override Literal? Execute(Evaluator evaluator)
    {
        var typePattern = Type.CreatePattern(evaluator); // TODO: endian

        var result = 0;
        var shift = 0;
        while (true)
        {
            var b = evaluator.Buffer.ReadUByte(evaluator.CurrentOffset);
            result |= (b & 0b1111111) << shift;
            shift += 7;

            evaluator.CurrentOffset++;

            if ((b & 0b10000000) == 0)
            {
                evaluator.CreateVariable(Name, Type, 1);
                return result;
            }
        }
    }
}