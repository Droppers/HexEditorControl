using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexControl.PatternLanguage.Types;

namespace HexControl.PatternLanguage.Literals
{
    public class Int64Literal : Literal<long>, IArithmeticOperations, IBitwiseOperations, IEqualityOperations
    {
        public Int64Literal(long value) : base(value)
        {
        }

        public override AsciiChar ToChar() => (AsciiChar)Value;
        public override char ToChar16() => (char)Value;

        public override ulong ToUnsignedLong()
        {
            return (ulong)Value;
        }

        public override long ToSignedLong()
        {
            return Value;
        }

        public override bool ToBool()
        {
            return Value > 0;
        }

        public Literal Multiply(Literal other)
        {
            return Create(Value * other.ToSignedLong());
        }

        public Literal Add(Literal other)
        {
            return Create(Value + other.ToSignedLong());
        }

        public Literal Subtract(Literal other)
        {
            return Create(Value - other.ToSignedLong());
        }

        public Literal Divide(Literal other)
        {
            var otherValue = other.ToSignedLong();
            if (otherValue is 0)
            {
                throw new InvalidOperationException("Divide by zero.");
            }
            return Create(Value / otherValue);
        }

        public override double ToDouble()
        {
            return Value;
        }

        public BoolLiteral Greater(Literal other)
        {
            return Value > other.ToSignedLong();
        }

        public BoolLiteral Less(Literal other)
        {
            return Value < other.ToSignedLong();
        }

        public BoolLiteral Equal(Literal other)
        {
            return Value == other.ToSignedLong();
        }

        public BoolLiteral NotEqual(Literal other)
        {
            return Value != other.ToSignedLong();
        }

        public BoolLiteral GreaterOrEqual(Literal other)
        {
            return Value >= other.ToSignedLong();
        }

        public BoolLiteral LessOrEqual(Literal other)
        {
            return Value <= other.ToSignedLong();
        }

        public Literal Modulo(Literal other)
        {
            var otherValue = other.ToSignedLong();
            if (otherValue is 0)
            {
                throw new InvalidOperationException("Divide by zero.");
            }
            return Create(Value % otherValue);
        }

        public Literal BitShiftLeft(Literal other)
        {
            return Create(Value << (int)other.ToSignedLong());
        }

        public Literal BitShiftRight(Literal other)
        {
            return Create(Value >> (int)other.ToSignedLong());
        }

        public Literal BitAnd(Literal other)
        {
            return Create(Value & other.ToSignedLong());
        }

        public Literal BitXor(Literal other)
        {
            return Create(Value ^ other.ToSignedLong());
        }

        public Literal BitOr(Literal other)
        {
            return Create(Value | other.ToSignedLong());
        }

        public Literal BitNot(Literal other)
        {
            return Create(~Value);
        }

        public BoolLiteral And(Literal other)
        {
            return Value is not 0 && other.ToSignedLong() is not 0;
        }

        public BoolLiteral Xor(Literal other)
        {
            var otherValue = other.ToSignedLong();
            return Value is not 0 && otherValue is 0 || Value is 0 && otherValue is not 0;
        }

        public BoolLiteral Or(Literal other)
        {
            return Value is not 0 || other.ToSignedLong() is not 0;
        }

        public BoolLiteral Not(Literal other)
        {
            return Value is 0;
        }

        public override string ToString() => Value.ToString();

        public static implicit operator Int64Literal(long value) => Create(value);
    }
}
