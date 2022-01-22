using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexControl.PatternLanguage.Types;

namespace HexControl.PatternLanguage.Literals
{
    public class UInt64Literal : Literal<ulong>, IArithmeticOperations, IBitwiseOperations, IEqualityOperations
    {
        public UInt64Literal(ulong value) : base(value)
        {
        }

        public override AsciiChar ToChar() => (AsciiChar)(byte)Value;
        public override char ToChar16() => (char)Value;

        public override ulong ToUnsignedLong()
        {
            return Value;
        }

        public override long ToSignedLong()
        {
            return (long)Value;
        }

        public override bool ToBool()
        {
            return Value > 0;
        }

        public Literal Multiply(Literal other)
        {
            return Value * other.ToUnsignedLong();
        }

        public Literal Add(Literal other)
        {
            return Value + other.ToUnsignedLong();
        }

        public Literal Subtract(Literal other)
        {
            return Value - other.ToUnsignedLong();
        }

        public Literal Divide(Literal other)
        {
            var otherValue = other.ToUnsignedLong();
            if (otherValue is 0)
            {
                throw new InvalidOperationException("Divide by zero.");
            }
            return Value / otherValue;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override double ToDouble()
        {
            return Value;
        }

        public BoolLiteral Greater(Literal other)
        {
            return Value > other.ToUnsignedLong();
        }

        public BoolLiteral Less(Literal other)
        {
            return Value < other.ToUnsignedLong();
        }

        public BoolLiteral Equal(Literal other)
        {
            return Value == other.ToUnsignedLong();
        }

        public BoolLiteral NotEqual(Literal other)
        {
            return Value != other.ToUnsignedLong();
        }

        public BoolLiteral GreaterOrEqual(Literal other)
        {
            return Value >= other.ToUnsignedLong();
        }

        public BoolLiteral LessOrEqual(Literal other)
        {
            return Value <= other.ToUnsignedLong();
        }

        public Literal Modulo(Literal other)
        {
            var otherValue = other.ToUnsignedLong();
            if (otherValue is 0)
            {
                throw new InvalidOperationException("Divide by zero.");
            }
            return Value % otherValue;
        }

        public Literal BitShiftLeft(Literal other)
        {
            return Value << (int)other.ToUnsignedLong();
        }

        public Literal BitShiftRight(Literal other)
        {
            return Value >> (int)other.ToUnsignedLong();
        }

        public Literal BitAnd(Literal other)
        {
            return Value & other.ToUnsignedLong();
        }

        public Literal BitXor(Literal other)
        {
            return Value ^ other.ToUnsignedLong();
        }

        public Literal BitOr(Literal other)
        {
            return Value | other.ToUnsignedLong();
        }

        public Literal BitNot(Literal other)
        {
            return ~Value;
        }

        public BoolLiteral And(Literal other)
        {
            return Value is not 0 && other.ToUnsignedLong() is not 0;
        }

        public BoolLiteral Xor(Literal other)
        {
            var otherValue = other.ToUnsignedLong();
            return Value is not 0 && otherValue is 0 || Value is 0 && otherValue is not 0;
        }

        public BoolLiteral Or(Literal other)
        {
            return Value is not 0 || other.ToUnsignedLong() is not 0;
        }

        public BoolLiteral Not(Literal other)
        {
            return Value is 0;
        }

        public static implicit operator UInt64Literal(ulong value) => Create(value);
    }
}
