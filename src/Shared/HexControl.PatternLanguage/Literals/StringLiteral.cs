using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexControl.Core.Numerics;
using HexControl.PatternLanguage.Types;

namespace HexControl.PatternLanguage.Literals
{
    public class StringLiteral : Literal<string>, IArithmeticOperations, IEqualityOperations
    {
        public StringLiteral(string value) : base(value)
        {
        }

        public override AsciiChar ToChar() => throw new NotSupportedException("Cannot cast string to char.");
        public override char ToChar16() => throw new NotSupportedException("Cannot cast string to char16.");

        public BoolLiteral Greater(Literal other)
        {
            if (other is not StringLiteral otherString)
            {
                throw new NotSupportedException("Cannot compare string to non-string value.");
            }

            return string.Compare(Value, otherString.Value, StringComparison.Ordinal) > 0;
        }

        public BoolLiteral GreaterOrEqual(Literal other)
        {
            if (other is not StringLiteral otherString)
            {
                throw new NotSupportedException("Cannot compare string to non-string value.");
            }

            return string.Compare(Value, otherString.Value, StringComparison.Ordinal) > 0 || Value == otherString.Value;
        }

        public BoolLiteral Equal(Literal other)
        {
            if (other is not StringLiteral otherString)
            {
                throw new NotSupportedException("Cannot compare string to non-string value.");
            }


            return Value.Equals(otherString.Value);
        }
        public BoolLiteral NotEqual(Literal other)
        {
            if (other is not StringLiteral otherString)
            {
                throw new NotSupportedException("Cannot compare string to non-string value.");
            }

            return Value != otherString.Value;
        }

        public BoolLiteral Less(Literal other)
        {
            if (other is not StringLiteral otherString)
            {
                throw new NotSupportedException("Cannot compare string to non-string value.");
            }

            return Value.CompareTo(otherString) < 0;
        }

        public BoolLiteral LessOrEqual(Literal other)
        {
            if (other is not StringLiteral otherString)
            {
                throw new NotSupportedException("Cannot compare string to non-string value.");
            }

            return string.Compare(Value, otherString.Value, StringComparison.Ordinal) < 0 || Value == otherString.Value;
        }

        public override Int128 ToInt128()
        {
            throw new NotSupportedException("Cannot convert string to number.");
        }

        public override UInt128 ToUInt128()
        {
            throw new NotSupportedException("Cannot convert string to number.");
        }

        public override long ToInt64()
        {
            throw new NotSupportedException("Cannot convert string to number.");
        }

        public override ulong ToUInt64()
        {
            throw new NotSupportedException("Cannot convert string to number.");
        }

        public override bool ToBool()
        {
            return Value.Length > 0;
        }

        public Literal Add(Literal other)
        {
            return Create(Value + other.ToString());
        }

        public Literal Divide(Literal other)
        {
            throw new NotSupportedException("Cannot use string in divide operation.");
        }

        public Literal Multiply(Literal other)
        {
            var times = other.ToInt128();
            var sb = new StringBuilder();

            for (var i = 0; i < times; i++)
            {
                sb.Append(Value);
            }

            return Create(sb.ToString());
        }

        public Literal Subtract(Literal other)
        {
            throw new NotSupportedException("Cannot use string in subtract operation.");
        }

        public override string ToString()
        {
            return Value;
        }

        public override double ToDouble()
        {
            throw new NotSupportedException("Cannot cast string to double.");
        }

        public Literal Modulo(Literal other)
        {
            throw new NotSupportedException("Cannot use string in mathematical operations.");
        }

        public BoolLiteral And(Literal other)
        {
            if (other is not StringLiteral otherString)
            {
                throw new NotSupportedException("Cannot compare string to non-string value.");
            }

            return Value.Length > 0 && otherString.Value.Length > 0;
        }

        public BoolLiteral Xor(Literal other)
        {
            if (other is not StringLiteral otherString)
            {
                throw new NotSupportedException("Cannot compare string to non-string value.");
            }

            return (Value.Length > 0 && otherString.Value.Length == 0) || (Value.Length == 0 && otherString.Value.Length > 0);
        }

        public BoolLiteral Or(Literal other)
        {
            if (other is not StringLiteral otherString)
            {
                throw new NotSupportedException("Cannot compare string to non-string value.");
            }

            return Value.Length > 0 || otherString.Value.Length > 0;
        }

        public BoolLiteral Not(Literal other)
        {
            if (other is not StringLiteral otherString)
            {
                throw new NotSupportedException("Cannot compare string to non-string value.");
            }

            return Value.Length == 0 && otherString.Value.Length == 0;
        }

        public override string ToString(bool cast) => Value;

        public static implicit operator StringLiteral(string value) => Create(value);
    }
}
