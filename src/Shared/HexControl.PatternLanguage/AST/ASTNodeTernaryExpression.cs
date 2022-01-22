using System;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeTernaryExpression : ASTNode
{
    private readonly ASTNode _first;
    private readonly Token.Operator _operator;
    private readonly ASTNode _second;
    private readonly ASTNode _third;

    public ASTNodeTernaryExpression(ASTNode first, ASTNode second, ASTNode third, Token.Operator op)
    {
        _first = first;
        _second = second;
        _third = third;
        _operator = op;
    }

    private ASTNodeTernaryExpression(ASTNodeTernaryExpression other) : base(other)
    {
        _operator = other._operator;
        _first = other._first.Clone();
        _second = other._second.Clone();
        _third = other._third.Clone();
    }

    public override ASTNode Clone() => new ASTNodeTernaryExpression(this);

    public override ASTNode Evaluate(Evaluator evaluator)
    {
        if (_first is null || _second is null || _third is null)
        {
            //LogConsole.abortEvaluation("attempted to use void expression in mathematical expression", this);
            throw new Exception("attempted to use void expression in mathematical expression"); // TODO: FIX
        }

        var first = (ASTNodeLiteral)_first.Evaluate(evaluator);
        var second = (ASTNodeLiteral)_second.Evaluate(evaluator);
        var third = (ASTNodeLiteral)_third.Evaluate(evaluator);

        var condition = first.Literal.ToBool();
        if (second.Literal.GetType() != third.Literal.GetType())
        {
            throw new Exception("operands to ternary expression have different types");
        }

        return new ASTNodeLiteral(condition ? second.Literal : third.Literal);
    }
}