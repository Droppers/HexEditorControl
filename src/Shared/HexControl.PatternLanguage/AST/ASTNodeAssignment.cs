using HexControl.PatternLanguage.Literals;

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

    public override Literal? Execute(Evaluator evaluator)
    {
        var literal = (ASTNodeLiteral)_rValue.Evaluate(evaluator);
        evaluator.SetVariable(_lValueName, literal.Literal);

        return null;
    }
}