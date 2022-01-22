using System;
using System.Linq;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeTypeOperator : ASTNode
{
    private readonly ASTNode _expression;


    private readonly Token.Operator _operator;

    public ASTNodeTypeOperator(Token.Operator @operator, ASTNode expression)
    {
        _operator = @operator;
        _expression = expression;
    }

    private ASTNodeTypeOperator(ASTNodeTypeOperator other) : base(other)
    {
        _operator = other._operator;
        _expression = other._expression.Clone();
    }

    public override ASTNode Clone() => new ASTNodeTypeOperator(this);

    public override ASTNode Evaluate(Evaluator evaluator)
    {
        var pattern = _expression.CreatePatterns(evaluator)[0];

        return _operator switch
        {
            Token.Operator.AddressOf => new ASTNodeLiteral((ulong)pattern.Offset),
            Token.Operator.SizeOf => new ASTNodeLiteral((ulong)pattern.Size),
            _ => throw new Exception("invalid type operator")
        };
    }
}