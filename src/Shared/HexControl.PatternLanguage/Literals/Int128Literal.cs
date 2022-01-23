using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexControl.Core.Numerics;
using HexControl.PatternLanguage.Types;

namespace HexControl.PatternLanguage.Literals
{
    public class Int128Literal : Literal<Int128>, IArithmeticOperations, IBitwiseOperations, IEqualityOperations
    {
        public Int128Literal(Int128 value) : base(value)
        {
        }

        public override AsciiChar ToChar() => (AsciiChar)(int)Value;
        public override char ToChar16() => (char)Value;

        public override UInt128 ToUInt128()
        {
            return (ulong)Value;
        }

        public override Int128 ToInt128()
        {
            return Value;
        }

        public override long ToInt64() => (long)Value;
        public override ulong ToUInt64() => (ulong)Value;

        public override bool ToBool()
        {
            return Value > 0;
        }

        public Literal Multiply(Literal other)
        {
            return Create(Value * other.ToInt128());
        }

        public Literal Add(Literal other)
        {
            return Create(Value + other.ToInt128());
        }

        public Literal Subtract(Literal other)
        {
            return Create(Value - other.ToInt128());
        }

        public Literal Divide(Literal other)
        {
            var otherValue = other.ToInt128();
            if (otherValue == Int128.Zero)
            {
                throw new InvalidOperationException("Divide by zero.");
            }
            return Create(Value / otherValue);
        }

        public override double ToDouble()
        {
            return (double)Value;
        }

        public BoolLiteral Greater(Literal other)
        {
            return Value > other.ToInt128();
        }

        public BoolLiteral Less(Literal other)
        {
            return Value < other.ToInt128();
        }

        public BoolLiteral Equal(Literal other)
        {
            return Value == other.ToInt128();
        }

        public BoolLiteral NotEqual(Literal other)
        {
            return Value != other.ToInt128();
        }

        public BoolLiteral GreaterOrEqual(Literal other)
        {
            return Value >= other.ToInt128();
        }

        public BoolLiteral LessOrEqual(Literal other)
        {
            return Value <= other.ToInt128();
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

        public Literal BitShiftLeft(Literal other)
        {
            return Create(Value << (int)other.ToInt128());
        }

        public Literal BitShiftRight(Literal other)
        {
            return Create(Value >> (int)other.ToInt128());
        }

        public Literal BitAnd(Literal other)
        {
            return Create(Value & other.ToInt128());
        }

        public Literal BitXor(Literal other)
        {
            return Create(Value ^ other.ToInt128());
        }

        public Literal BitOr(Literal other)
        {
            return Create(Value | other.ToInt128());
        }

        public Literal BitNot(Literal other)
        {
            return Create(~Value);
        }

        public BoolLiteral And(Literal other)
        {
            return Value != Int128.Zero && other.ToInt128() != Int128.Zero;
        }

        public BoolLiteral Xor(Literal other)
        {
            var otherValue = other.ToInt128();
            return Value != Int128.Zero && otherValue == Int128.Zero || Value == Int128.Zero && otherValue != Int128.Zero;
        }

        public BoolLiteral Or(Literal other)
        {
            return Value != Int128.Zero || other.ToInt128() != Int128.Zero;
        }

        public BoolLiteral Not(Literal other)
        {
            return Value == Int128.Zero;
        }

        public override string ToString() => Value.ToString();

        public static implicit operator Int128Literal(Int128 value) => Create(value);
    }
}
