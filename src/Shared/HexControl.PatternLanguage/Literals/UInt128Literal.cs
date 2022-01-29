using System;
using HexControl.Core.Numerics;
using HexControl.PatternLanguage.Helpers;

namespace HexControl.PatternLanguage.Literals;

public class UInt128Literal : Literal<UInt128>, IArithmeticOperations, IBitwiseOperations, IEqualityOperations
{
    public UInt128Literal(UInt128 value) : base(value) { }

    public Literal Multiply(Literal other) => Value * other.ToUInt128();

    public Literal Add(Literal other) => Value + other.ToUInt128();

    public Literal Subtract(Literal other) => Value - other.ToUInt128();

    public Literal Divide(Literal other)
    {
        var otherValue = other.ToUInt128();
        if (otherValue == UInt128.Zero)
        {
            throw new InvalidOperationException("Divide by zero.");
        }

        return Value / otherValue;
    }

    public Literal Modulo(Literal other)
    {
        var otherValue = other.ToUInt128();
        if (otherValue == UInt128.Zero)
        {
            throw new InvalidOperationException("Divide by zero.");
        }

        return Value % otherValue;
    }

    public Literal BitShiftLeft(Literal other) => Value << (int)other.ToUInt128();

    public Literal BitShiftRight(Literal other) => Value >> (int)other.ToUInt128();

    public Literal BitAnd(Literal other) => Value & other.ToUInt128();

    public Literal BitXor(Literal other) => Value ^ other.ToUInt128();

    public Literal BitOr(Literal other) => Value | other.ToUInt128();

    public Literal BitNot(Literal other) => ~Value;

    public BoolLiteral Greater(Literal other) => Value > other.ToUInt128();

    public BoolLiteral Less(Literal other) => Value < other.ToUInt128();

    public BoolLiteral Equal(Literal other) => Value == other.ToUInt128();

    public BoolLiteral NotEqual(Literal other) => Value != other.ToUInt128();

    public BoolLiteral GreaterOrEqual(Literal other) => Value >= other.ToUInt128();

    public BoolLiteral LessOrEqual(Literal other) => Value <= other.ToUInt128();

    public BoolLiteral And(Literal other) => Value != UInt128.Zero && other.ToUInt128() != UInt128.Zero;

    public BoolLiteral Xor(Literal other)
    {
        var otherValue = other.ToUInt128();
        return Value != UInt128.Zero && otherValue == UInt128.Zero ||
               Value == UInt128.Zero && otherValue != UInt128.Zero;
    }

    public BoolLiteral Or(Literal other) => Value != UInt128.Zero || other.ToUInt128() != UInt128.Zero;

    public BoolLiteral Not(Literal other) => Value == UInt128.Zero;

    public override AsciiChar ToChar() => (AsciiChar)(byte)Value;
    public override char ToChar16() => (char)Value;

    public override UInt128 ToUInt128() => Value;

    public override Int128 ToInt128() => (Int128)Value;

    public override long ToInt64() => (long)Value;
    public override ulong ToUInt64() => (ulong)Value;

    public override bool ToBool() => Value > 0;

    public override string ToString() => Value.ToString();

    public override double ToDouble() => (double)Value;

    public static implicit operator UInt128Literal(UInt128 value) => Create(value);
}