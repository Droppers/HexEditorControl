using System;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Tokens;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeMathematicalExpression : ASTNode
{
    private readonly ASTNode _left;
    private readonly Token.Operator _operator;
    private readonly ASTNode _right;

    public ASTNodeMathematicalExpression(ASTNode left, ASTNode right, Token.Operator op)
    {
        _left = left;
        _right = right;
        _operator = op;
    }

    private ASTNodeMathematicalExpression(ASTNodeMathematicalExpression other) : base(other)
    {
        _left = other._left;
        _right = other._right;
        _operator = other._operator;
    }

    public override ASTNode Clone() => new ASTNodeMathematicalExpression(this);

    private static (Literal left, Literal right) ImportanceCast(Literal left, Literal right)
    {
        if (right is DoubleLiteral)
        {
            return (left.ToDouble(), right);
        }

        return (left, right);
    }

    private static bool IsEqualityOperator(Token.Operator op) => op is Token.Operator.BoolEquals
        or Token.Operator.BoolNotEquals
        or Token.Operator.BoolGreaterThan
        or Token.Operator.BoolGreaterThanOrEquals
        or Token.Operator.BoolLessThan
        or Token.Operator.BoolLessThanOrEquals
        or Token.Operator.BoolAnd
        or Token.Operator.BoolNot
        or Token.Operator.BoolOr
        or Token.Operator.BoolXor;

    private static bool IsMathOperator(Token.Operator op) => op is Token.Operator.Plus
        or Token.Operator.Minus
        or Token.Operator.Star
        or Token.Operator.Slash
        or Token.Operator.Percent;

    private static bool IsBitwiseOperator(Token.Operator op) => op is Token.Operator.BitOr
        or Token.Operator.BitXor
        or Token.Operator.BitAnd
        or Token.Operator.BitNot
        or Token.Operator.ShiftLeft
        or Token.Operator.ShiftRight;

    public override ASTNode Evaluate(Evaluator evaluator)
    {
        if (_left is null || _right is null)
        {
            //LogConsole.abortEvaluation("attempted to use void expression in mathematical expression", this);
            throw new Exception("attempted to use void expression in mathematical expression");
        }

        var leftNode = (ASTNodeLiteral)_left.Evaluate(evaluator);
        var rightNode = (ASTNodeLiteral)_right.Evaluate(evaluator);

        var leftLiteral = leftNode.Literal;
        var rightLiteral = rightNode.Literal;

        (leftLiteral, rightLiteral) = ImportanceCast(leftLiteral, rightLiteral);

        Literal result;
        if (IsEqualityOperator(_operator))
        {
            if (leftLiteral is not IEqualityOperations equality)
            {
                throw new Exception($"Literal {leftLiteral} does not support equality operations.");
            }

            result = _operator switch
            {
                Token.Operator.BoolEquals => equality.Equal(rightLiteral),
                Token.Operator.BoolNotEquals => equality.NotEqual(rightLiteral),
                Token.Operator.BoolGreaterThan => equality.GreaterOrEqual(rightLiteral),
                Token.Operator.BoolGreaterThanOrEquals => equality.GreaterOrEqual(rightLiteral),
                Token.Operator.BoolLessThan => equality.Less(rightLiteral),
                Token.Operator.BoolLessThanOrEquals => equality.LessOrEqual(rightLiteral),
                Token.Operator.BoolAnd => equality.And(rightLiteral),
                Token.Operator.BoolNot => equality.Not(rightLiteral),
                Token.Operator.BoolOr => equality.Or(rightLiteral),
                Token.Operator.BoolXor => equality.Xor(rightLiteral),
                _ => throw new Exception($"{_operator} is not a equality operator.") // TODO: evaluation exception
            };
        }
        else if (IsMathOperator(_operator))
        {
            if (leftLiteral is not IArithmeticOperations math)
            {
                throw new Exception($"Literal {leftLiteral} does not support math operations.");
            }

            result = _operator switch
            {
                Token.Operator.Plus => math.Add(rightLiteral),
                Token.Operator.Minus => math.Subtract(rightLiteral),
                Token.Operator.Star => math.Multiply(rightLiteral),
                Token.Operator.Slash => math.Divide(rightLiteral),
                Token.Operator.Percent => math.Modulo(rightLiteral),
                _ => throw new Exception($"{_operator} is not a equality operator.") // TODO: evaluation exception
            };
        }
        else if (IsBitwiseOperator(_operator))
        {
            if (leftLiteral is not IBitwiseOperations bitwise)
            {
                throw new Exception($"Literal {leftLiteral} does not support bitwise operations.");
            }

            result = _operator switch
            {
                Token.Operator.BitAnd => bitwise.BitAnd(rightLiteral),
                Token.Operator.BitNot => bitwise.BitNot(rightLiteral),
                Token.Operator.BitOr => bitwise.BitOr(rightLiteral),
                Token.Operator.BitXor => bitwise.BitXor(rightLiteral),
                Token.Operator.ShiftLeft => bitwise.BitShiftLeft(rightLiteral),
                Token.Operator.ShiftRight => bitwise.BitShiftRight(rightLiteral),
                _ => throw new Exception($"{_operator} is not a equality operator.") // TODO: evaluation exception
            };
        }
        else
        {
            throw new Exception($"{_operator} is not a supported operator.");
        }

        return new ASTNodeLiteral(result);
    }
}