using System;
using HexControl.Core.Numerics;
using HexControl.PatternLanguage.Helpers;

namespace HexControl.PatternLanguage.Literals;

public class Int128Literal : Literal<Int128>, IArithmeticOperations, IBitwiseOperations, IEqualityOperations
{
    public Int128Literal(Int128 value) : base(value) { }

    public Literal Multiply(Literal other) => Create(Value * other.ToInt128());

    public Literal Add(Literal other) => Create(Value + other.ToInt128());

    public Literal Subtract(Literal other) => Create(Value - other.ToInt128());

    public Literal Divide(Literal other)
    {
        var otherValue = other.ToInt128();
        if (otherValue == Int128.Zero)
        {
            throw new InvalidOperationException("Divide by zero.");
        }

        return Create(Value / otherValue);
    }

    public Literal Modulo(Literal other)
    {
        var otherValue = other.ToInt128();
        if (otherValue == Int128.Zero)
        {
            throw new InvalidOperationException("Divide by zero.");
        }

        return Create(Value % otherValue);
    }

    public Literal BitShiftLeft(Literal other) => Create(Value << (int)other.ToInt128());

    public Literal BitShiftRight(Literal other) => Create(Value >> (int)other.ToInt128());

    public Literal BitAnd(Literal other) => Create(Value & other.ToInt128());

    public Literal BitXor(Literal other) => Create(Value ^ other.ToInt128());

    public Literal BitOr(Literal other) => Create(Value | other.ToInt128());

    public Literal BitNot(Literal other) => Create(~Value);

    public BoolLiteral Greater(Literal other) => Value > other.ToInt128();

    public BoolLiteral Less(Literal other) => Value < other.ToInt128();

    public BoolLiteral Equal(Literal other) => Value == other.ToInt128();

    public BoolLiteral NotEqual(Literal other) => Value != other.ToInt128();

    public BoolLiteral GreaterOrEqual(Literal other) => Value >= other.ToInt128();

    public BoolLiteral LessOrEqual(Literal other) => Value <= other.ToInt128();

    public BoolLiteral And(Literal other) => Value != Int128.Zero && other.ToInt128() != Int128.Zero;

    public BoolLiteral Xor(Literal other)
    {
        var otherValue = other.ToInt128();
        return Value != Int128.Zero && otherValue == Int128.Zero || Value == Int128.Zero && otherValue != Int128.Zero;
    }

    public BoolLiteral Or(Literal other) => Value != Int128.Zero || other.ToInt128() != Int128.Zero;

    public BoolLiteral Not(Literal other) => Value == Int128.Zero;

    public override AsciiChar ToChar() => (AsciiChar)(int)Value;
    public override char ToChar16() => (char)Value;

    public override UInt128 ToUInt128() => (ulong)Value;

    public override Int128 ToInt128() => Value;

    public override long ToInt64() => (long)Value;
    public override ulong ToUInt64() => (ulong)Value;

    public override bool ToBool() => Value > 0;

    public override double ToDouble() => (double)Value;

    public override string ToString() => Value.ToString();

    public static implicit operator Int128Literal(Int128 value) => Create(value);
}