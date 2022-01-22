using System;
using System.Collections.Generic;
using System.Linq;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeVariableDecl : AttributableASTNode
{
    private readonly bool _inVariable;
    private readonly bool _outVariable;

    private readonly ASTNode? _placementOffset;

    public ASTNodeVariableDecl(string name, ASTNode type, ASTNode? placementOffset = null, bool inVariable = false,
        bool outVariable = false)
    {
        Name = name;
        Type = type;
        _placementOffset = placementOffset;
        _inVariable = inVariable;
        _outVariable = outVariable;
    }

    private ASTNodeVariableDecl(ASTNodeVariableDecl other) : base(other)
    {
        Name = other.Name;
        Type = other.Type.Clone();

        _placementOffset = other._placementOffset?.Clone();
    }

    public string Name { get; }
    public ASTNode Type { get; }

    public override ASTNode Clone() => new ASTNodeVariableDecl(this);

    public ASTNode? GetPlacementOffset() => _placementOffset;

    public bool IsInVariable() => _inVariable;
    public bool IsOutVariable() => _outVariable;

    public override IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator)
    {
        if (_placementOffset is not null)
        {
            var offsetNode = (ASTNodeLiteral)_placementOffset.Evaluate(evaluator);

            evaluator.CurrentOffset = offsetNode.Literal switch
            {
                StringLiteral => throw new Exception("placement offset cannot be a string"), // this
                PatternDataLiteral => throw new Exception("placement offset cannot be a custom type"), // this
                _ => offsetNode.Literal.ToSignedLong()
            };
        }

        var pattern = Type.CreatePatterns(evaluator)[0];
        pattern.VariableName = Name;

        ApplyVariableAttributes(evaluator, this, pattern);

        return new [] {pattern};
    }

    public override Literal? Execute(Evaluator evaluator)
    {
        evaluator.CreateVariable(Name, Type);

        return null;
    }
}