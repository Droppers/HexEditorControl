using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexControl.PatternLanguage.Patterns;
using HexControl.PatternLanguage.Types;

namespace HexControl.PatternLanguage.Literals
{
    public abstract class Literal
    {
        public abstract ulong ToUnsignedLong();
        public abstract long ToSignedLong();
        public abstract double ToDouble();
        public abstract bool ToBool();
        public abstract AsciiChar ToChar();
        public abstract char ToChar16();


        public virtual string ToString(bool cast)
        {
            if (!cast)
            {
                throw new Exception($"Literal {GetType().Name} is not a string");
            }

            return ToString()!;
        }

        public static Int64Literal Create(long value)
        {
            return new Int64Literal(value);
        }

        public static UInt64Literal Create(ulong value)
        {
            return new UInt64Literal(value);
        }

        public static DoubleLiteral Create(double value)
        {
            return new DoubleLiteral(value);
        }

        public static BoolLiteral Create(bool value)
        {
            return new BoolLiteral(value);
        }

        public static StringLiteral Create(string value)
        {
            return new StringLiteral(value);
        }

        public static PatternDataLiteral Create(PatternData value)
        {
            return new PatternDataLiteral(value);
        }

        public static CharLiteral Create(AsciiChar value)
        {
            return new CharLiteral(value);
        }

        public static Char16Literal Create(char value)
        {
            return new Char16Literal(value);
        }

        public static implicit operator Literal(long value) => Create(value);
        public static implicit operator Literal(ulong value) => Create(value);
        public static implicit operator Literal(double value) => Create(value);
        public static implicit operator Literal(string value) => Create(value);
        public static implicit operator Literal(bool value) => Create(value);
        public static implicit operator Literal(PatternData value) => Create(value);
        public static implicit operator Literal(AsciiChar value) => Create(value);
        public static implicit operator Literal(char value) => Create(value);
    }
}