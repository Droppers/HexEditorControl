﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexControl.Core.Numerics;
using HexControl.PatternLanguage.Types;

namespace HexControl.PatternLanguage.Literals
{
    public class UInt128Literal : Literal<UInt128>, IArithmeticOperations, IBitwiseOperations, IEqualityOperations
    {
        public UInt128Literal(UInt128 value) : base(value)
        {
        }

        public override AsciiChar ToChar() => (AsciiChar)(byte)Value;
        public override char ToChar16() => (char)Value;

        public override UInt128 ToUInt128()
        {
            return Value;
        }

        public override Int128 ToInt128()
        {
            return (Int128)Value;
        }

        public override long ToInt64() => (long)Value;
        public override ulong ToUInt64() => (ulong)Value;

        public override bool ToBool()
        {
            return Value > 0;
        }

        public Literal Multiply(Literal other)
        {
            return Value * other.ToUInt128();
        }

        public Literal Add(Literal other)
        {
            return Value + other.ToUInt128();
        }

        public Literal Subtract(Literal other)
        {
            return Value - other.ToUInt128();
        }

        public Literal Divide(Literal other)
        {
            var otherValue = other.ToUInt128();
            if (otherValue == UInt128.Zero)
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
            return (double)Value;
        }

        public BoolLiteral Greater(Literal other)
        {
            return Value > other.ToUInt128();
        }

        public BoolLiteral Less(Literal other)
        {
            return Value < other.ToUInt128();
        }

        public BoolLiteral Equal(Literal other)
        {
            return Value == other.ToUInt128();
        }

        public BoolLiteral NotEqual(Literal other)
        {
            return Value != other.ToUInt128();
        }

        public BoolLiteral GreaterOrEqual(Literal other)
        {
            return Value >= other.ToUInt128();
        }

        public BoolLiteral LessOrEqual(Literal other)
        {
            return Value <= other.ToUInt128();
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

        public Literal BitShiftLeft(Literal other)
        {
            return Value << (int)other.ToUInt128();
        }

        public Literal BitShiftRight(Literal other)
        {
            return Value >> (int)other.ToUInt128();
        }

        public Literal BitAnd(Literal other)
        {
            return Value & other.ToUInt128();
        }

        public Literal BitXor(Literal other)
        {
            return Value ^ other.ToUInt128();
        }

        public Literal BitOr(Literal other)
        {
            return Value | other.ToUInt128();
        }

        public Literal BitNot(Literal other)
        {
            return ~Value;
        }

        public BoolLiteral And(Literal other)
        {
            return Value != UInt128.Zero && other.ToUInt128() != UInt128.Zero;
        }

        public BoolLiteral Xor(Literal other)
        {
            var otherValue = other.ToUInt128();
            return Value != UInt128.Zero && otherValue == UInt128.Zero || Value == UInt128.Zero && otherValue != UInt128.Zero;
        }

        public BoolLiteral Or(Literal other)
        {
            return Value != UInt128.Zero || other.ToUInt128() != UInt128.Zero;
        }

        public BoolLiteral Not(Literal other)
        {
            return Value == UInt128.Zero;
        }

        public static implicit operator UInt128Literal(UInt128 value) => Create(value);
    }
}