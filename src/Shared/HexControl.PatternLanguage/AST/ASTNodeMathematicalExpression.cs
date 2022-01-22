using System;
using HexControl.PatternLanguage.Literals;

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

    private ASTNodeMathematicalExpression(ASTNodeMathematicalExpression node) : base(node)
    {
        _left = node._left;
        _right = node._right;
        _operator = node._operator;
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
        //var leftVal = leftNode?.getValue();
        //var rightVal = rightNode?.getValue();
        //var left = leftVal?.Literal ?? 0;
        //var right = rightVal?.Literal ?? 0;

        //if (left is long or ulong or double or char or bool && right is PatternData)
        //{
        //    throw new Exception("invalid operand used in mathematical expression");
        //}
        //else if (left is PatternData && right is long or ulong or double or char or bool or string or PatternData)
        //{
        //    throw new Exception("invalid operand used in mathematical expression");
        //}
        //else if (left is not string && right is string)
        //{
        //    throw new Exception("invalid operand used in mathematical expression");
        //}
        //else if (left is string && right is ulong or long or float or double)
        //{
        //    switch (this.getOperator())
        //    {
        //        case Token.Operator.Star:
        //            {
        //                var times = Cast<long>(right);
        //                string result = "";

        //                for (var i = 0; i < times; i++)
        //                    result += left;

        //                return new ASTNodeLiteral(result);
        //            }
        //        default:
        //            throw new Exception("invalid operand used in mathematical expression");
        //            //LogConsole.abortEvaluation("invalid operand used in mathematical expression", this);
        //            break;
        //    }
        //}
        //else if (left is string ls && right is string rs)
        //{
        //    switch (getOperator())
        //    {
        //        case Token.Operator.Plus:
        //            return new ASTNodeLiteral(ls + rs);
        //        case Token.Operator.ShiftLeft:
        //            //return new ASTNodeLiteral(lf << rf);
        //            throw new Exception("Not supported.");
        //        case Token.Operator.ShiftRight:
        //            //return new ASTNodeLiteral(lf >> rf);
        //            throw new Exception("Not supported.");
        //        case Token.Operator.BitAnd:
        //            //return new ASTNodeLiteral(lf & rf);
        //            throw new Exception("Not supported.");
        //        case Token.Operator.BitXor:
        //            //return new ASTNodeLiteral(lf ^ rf);
        //            throw new Exception("Not supported.");
        //        case Token.Operator.BitOr:
        //            throw new Exception("Not supported.");
        //        //return new ASTNodeLiteral(lf | rf);
        //        case Token.Operator.BitNot:
        //            throw new Exception("Not supported.");
        //        //return new ASTNodeLiteral(~rf);
        //        case Token.Operator.BoolEquals:
        //            return new ASTNodeLiteral(ls == rs);
        //        case Token.Operator.BoolNotEquals:
        //            return new ASTNodeLiteral(ls != rs);
        //        case Token.Operator.BoolGreaterThan:
        //            return new ASTNodeLiteral(ls.CompareTo(rs) > 0);
        //        case Token.Operator.BoolLessThan:
        //            return new ASTNodeLiteral(ls.CompareTo(rs) < 0);
        //        case Token.Operator.BoolGreaterThanOrEquals:
        //            return new ASTNodeLiteral(ls.CompareTo(rs) > 0 || ls == rs);
        //        case Token.Operator.BoolLessThanOrEquals:
        //            return new ASTNodeLiteral(ls.CompareTo(rs) < 0 || ls == rs);
        //        default:
        //            throw new Exception("invalid operand used in mathematical expression");
        //    }
        //}
        //else if (left is double || right is double)
        //{
        //    double lf = left is double ? (double)left : Cast<double>(left);
        //    double rf = right is double ? (double)right : Cast<double>(right);

        //    switch (getOperator())
        //    {
        //        case Token.Operator.Plus:
        //            return new ASTNodeLiteral(lf + rf);
        //        case Token.Operator.Minus:
        //            return new ASTNodeLiteral(lf - rf);
        //        case Token.Operator.Star:
        //            return new ASTNodeLiteral(lf * rf);
        //        case Token.Operator.Slash:
        //            if (rf == 0) throw new Exception("division by zero!"); // TODO: fix exceptions
        //            return new ASTNodeLiteral(lf / rf);
        //        case Token.Operator.Percent:
        //            if (rf == 0) throw new Exception("division by zero!");
        //            return new ASTNodeLiteral(lf % rf);
        //        case Token.Operator.ShiftLeft:
        //            //return new ASTNodeLiteral(lf << rf);
        //            throw new Exception("Not supported.");
        //        case Token.Operator.ShiftRight:
        //            //return new ASTNodeLiteral(lf >> rf);
        //            throw new Exception("Not supported.");
        //        case Token.Operator.BitAnd:
        //            //return new ASTNodeLiteral(lf & rf);
        //            throw new Exception("Not supported.");
        //        case Token.Operator.BitXor:
        //            //return new ASTNodeLiteral(lf ^ rf);
        //            throw new Exception("Not supported.");
        //        case Token.Operator.BitOr:
        //            throw new Exception("Not supported.");
        //        //return new ASTNodeLiteral(lf | rf);
        //        case Token.Operator.BitNot:
        //            throw new Exception("Not supported.");
        //        //return new ASTNodeLiteral(~rf);
        //        case Token.Operator.BoolEquals:
        //            return new ASTNodeLiteral(lf == rf);
        //        case Token.Operator.BoolNotEquals:
        //            return new ASTNodeLiteral(lf != rf);
        //        case Token.Operator.BoolGreaterThan:
        //            return new ASTNodeLiteral(lf > rf);
        //        case Token.Operator.BoolLessThan:
        //            return new ASTNodeLiteral(lf < rf);
        //        case Token.Operator.BoolGreaterThanOrEquals:
        //            return new ASTNodeLiteral(lf >= rf);
        //        case Token.Operator.BoolLessThanOrEquals:
        //            return new ASTNodeLiteral(lf <= rf);
        //        case Token.Operator.BoolAnd:
        //            return new ASTNodeLiteral(lf is not 0 && rf is not 0);
        //        case Token.Operator.BoolXor:
        //            return new ASTNodeLiteral(lf is not 0 && rf is 0 || lf is 0 && rf is not 0);
        //        case Token.Operator.BoolOr:
        //            return new ASTNodeLiteral(lf is not 0 || rf is not 0);
        //        case Token.Operator.BoolNot:
        //            return new ASTNodeLiteral(rf is 0);
        //        default:
        //            throw new Exception("invalid operand used in mathematical expression");
        //    }
        //}
        //else if (left is ulong || right is ulong)
        //{
        //    ulong lf = left is ulong ? (ulong)left : Cast<ulong>(left);
        //    ulong rf = right is ulong ? (ulong)right : Cast<ulong>(right);

        //    switch (getOperator())
        //    {
        //        case Token.Operator.Plus:
        //            return new ASTNodeLiteral(lf + rf);
        //        case Token.Operator.Minus:
        //            return new ASTNodeLiteral(lf - rf);
        //        case Token.Operator.Star:
        //            return new ASTNodeLiteral(lf * rf);
        //        case Token.Operator.Slash:
        //            if (rf == 0) throw new Exception("division by zero!"); // TODO: fix exceptions
        //            return new ASTNodeLiteral(lf / rf);
        //        case Token.Operator.Percent:
        //            if (rf == 0) throw new Exception("division by zero!");
        //            return new ASTNodeLiteral(lf % rf);
        //        case Token.Operator.ShiftLeft:
        //            return new ASTNodeLiteral(lf << (int)rf);
        //        case Token.Operator.ShiftRight:
        //            return new ASTNodeLiteral(lf >> (int)rf);
        //        case Token.Operator.BitAnd:
        //            return new ASTNodeLiteral(lf & rf);
        //        case Token.Operator.BitXor:
        //            return new ASTNodeLiteral(lf ^ rf);
        //        case Token.Operator.BitOr:
        //            return new ASTNodeLiteral(lf | rf);
        //        case Token.Operator.BitNot:
        //            return new ASTNodeLiteral(~rf);
        //        case Token.Operator.BoolEquals:
        //            return new ASTNodeLiteral(lf == rf);
        //        case Token.Operator.BoolNotEquals:
        //            return new ASTNodeLiteral(lf != rf);
        //        case Token.Operator.BoolGreaterThan:
        //            return new ASTNodeLiteral(lf > rf);
        //        case Token.Operator.BoolLessThan:
        //            return new ASTNodeLiteral(lf < rf);
        //        case Token.Operator.BoolGreaterThanOrEquals:
        //            return new ASTNodeLiteral(lf >= rf);
        //        case Token.Operator.BoolLessThanOrEquals:
        //            return new ASTNodeLiteral(lf <= rf);
        //        case Token.Operator.BoolAnd:
        //            return new ASTNodeLiteral(lf is not 0 && rf is not 0);
        //        case Token.Operator.BoolXor:
        //            return new ASTNodeLiteral(lf is not 0 && rf is 0 || lf is 0 && rf is not 0);
        //        case Token.Operator.BoolOr:
        //            return new ASTNodeLiteral(lf is not 0 || rf is not 0);
        //        case Token.Operator.BoolNot:
        //            return new ASTNodeLiteral(rf is 0);
        //        default:
        //            throw new Exception("invalid operand used in mathematical expression");
        //    }
        //}
        //else if (left is long || right is long)
        //{
        //    long lf = left is long ? (long)left : Cast<long>(left);
        //    long rf = right is long ? (long)right : Cast<long>(right);

        //    switch (getOperator())
        //    {
        //        case Token.Operator.Plus:
        //            return new ASTNodeLiteral(lf + rf);
        //        case Token.Operator.Minus:
        //            return new ASTNodeLiteral(lf - rf);
        //        case Token.Operator.Star:
        //            return new ASTNodeLiteral(lf * rf);
        //        case Token.Operator.Slash:
        //            if (rf == 0) throw new Exception("division by zero!"); // TODO: fix exceptions
        //            return new ASTNodeLiteral(lf / rf);
        //        case Token.Operator.Percent:
        //            if (rf == 0) throw new Exception("division by zero!");
        //            return new ASTNodeLiteral(lf % rf);
        //        case Token.Operator.ShiftLeft:
        //            return new ASTNodeLiteral(lf << (int)rf);
        //        case Token.Operator.ShiftRight:
        //            return new ASTNodeLiteral(lf >> (int)rf);
        //        case Token.Operator.BitAnd:
        //            return new ASTNodeLiteral(lf & rf);
        //        case Token.Operator.BitXor:
        //            return new ASTNodeLiteral(lf ^ rf);
        //        case Token.Operator.BitOr:
        //            return new ASTNodeLiteral(lf | rf);
        //        case Token.Operator.BitNot:
        //            return new ASTNodeLiteral(~rf);
        //        case Token.Operator.BoolEquals:
        //            return new ASTNodeLiteral(lf == rf);
        //        case Token.Operator.BoolNotEquals:
        //            return new ASTNodeLiteral(lf != rf);
        //        case Token.Operator.BoolGreaterThan:
        //            return new ASTNodeLiteral(lf > rf);
        //        case Token.Operator.BoolLessThan:
        //            return new ASTNodeLiteral(lf < rf);
        //        case Token.Operator.BoolGreaterThanOrEquals:
        //            return new ASTNodeLiteral(lf >= rf);
        //        case Token.Operator.BoolLessThanOrEquals:
        //            return new ASTNodeLiteral(lf <= rf);
        //        case Token.Operator.BoolAnd:
        //            return new ASTNodeLiteral(lf is not 0 && rf is not 0);
        //        case Token.Operator.BoolXor:
        //            return new ASTNodeLiteral(lf is not 0 && rf is 0 || lf is 0 && rf is not 0);
        //        case Token.Operator.BoolOr:
        //            return new ASTNodeLiteral(lf is not 0 || rf is not 0);
        //        case Token.Operator.BoolNot:
        //            return new ASTNodeLiteral(rf is 0);
        //        default:
        //            throw new Exception("invalid operand used in mathematical expression");
        //    }
        //}

        //throw new Exception("invalid mathematical operation");
    }
}