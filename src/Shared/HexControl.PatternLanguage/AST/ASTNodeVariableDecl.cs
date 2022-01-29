using System;
using System.Collections.Generic;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeVariableDecl : AttributableASTNode
{
    public ASTNodeVariableDecl(string name, ASTNode type, ASTNode? placementOffset = null, bool inVariable = false,
        bool outVariable = false)
    {
        Name = name;
        Type = type;
        PlacementOffset = placementOffset;
        IsInVariable = inVariable;
        IsOutVariable = outVariable;
    }

    private ASTNodeVariableDecl(ASTNodeVariableDecl other) : base(other)
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

    public override ASTNode Clone() => new ASTNodeVariableDecl(this);

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
        evaluator.CreateVariable(Name, Type);
        return null;
    }
}