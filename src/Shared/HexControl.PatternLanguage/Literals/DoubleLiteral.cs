using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexControl.Core.Numerics;
using HexControl.PatternLanguage.Types;

namespace HexControl.PatternLanguage.Literals
{
    public class DoubleLiteral : Literal<double>, IArithmeticOperations, IEqualityOperations
    {
        public DoubleLiteral(double value) : base(value) { }

        public override AsciiChar ToChar() => (AsciiChar)(byte)ToUInt128();
        public override char ToChar16() => (char)ToUInt128();

        public override Int128 ToInt128()
        {
            return (Int128)Value;
        }

        public override UInt128 ToUInt128()
        {
            return (UInt128)Value;
        }

        public override long ToInt64() => (long)Value;
        public override ulong ToUInt64() => (ulong)Value;

        public override bool ToBool()
        {
            return Value > 0;
        }

        public override double ToDouble()
        {
            return Value;
        }

        public Literal Multiply(Literal other)
        {
            return Value * other.ToDouble();
        }

        public Literal Divide(Literal other)
        {
            return Value / other.ToDouble();
        }

        public Literal Add(Literal other)
        {
            return Value + other.ToDouble();
        }

        public Literal Subtract(Literal other)
        {
            return Value / other.ToDouble();
        }

        public BoolLiteral Greater(Literal other)
        {
            return Value > other.ToDouble();
        }

        public BoolLiteral Less(Literal other)
        {
            return Value < other.ToDouble();
        }

        public BoolLiteral Equal(Literal other)
        {
            return Math.Abs(Value - other.ToDouble()) < double.Epsilon;
        }

        public BoolLiteral NotEqual(Literal other)
        {
            return Math.Abs(Value - other.ToDouble()) > double.Epsilon;
        }

        public BoolLiteral GreaterOrEqual(Literal other)
        {
            return Value >= other.ToDouble();
        }

        public BoolLiteral LessOrEqual(Literal other)
        {
            return Value <= other.ToDouble();
        }

        public Literal Modulo(Literal other)
        {
            return Create(Value % other.ToDouble());
        }
        
        public BoolLiteral And(Literal other)
        {
            return Value is not 0 && other.ToDouble() is not 0;
        }

        public BoolLiteral Xor(Literal other)
        {
            var otherValue = other.ToDouble();
            return Value is not 0 && otherValue is 0 || Value is 0 && otherValue is not 0;
        }

        public BoolLiteral Or(Literal other)
        {
            return Value is not 0 || other.ToDouble() is not 0;
        }

        public BoolLiteral Not(Literal other)
        {
            return Value is 0;
        }

        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

        public static implicit operator DoubleLiteral(double value) => Create(value);
    }
}
