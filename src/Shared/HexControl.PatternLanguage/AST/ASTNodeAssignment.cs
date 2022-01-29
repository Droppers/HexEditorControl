using System;
using System.Collections.Generic;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeAssignment : ASTNode
{
    private readonly string _lValueName;
    private readonly ASTNode _rValue;

    public ASTNodeAssignment(string lValueName, ASTNode rValue)
    {
        _lValueName = lValueName;
        _rValue = rValue;
    }

    private ASTNodeAssignment(ASTNodeAssignment other) : base(other)
    {
        _lValueName = other._lValueName;
        _rValue = other._rValue.Clone();
    }

    public override ASTNode Clone() => new ASTNodeAssignment(this);

    public override IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator) => Array.Empty<PatternData>();

    public override PatternData CreatePattern(Evaluator evaluator) => null;

    public override Literal? Execute(Evaluator evaluator)
    {
        var literal = (ASTNodeLiteral)_rValue.Evaluate(evaluator);

        if (_lValueName == "$")
        {
            evaluator.CurrentOffset = literal.Literal.ToInt64();
        }
        else
        {
            evaluator.SetVariable(_lValueName, literal.Literal);
        }

        return null;
    }
}