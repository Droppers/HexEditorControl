using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexControl.PatternLanguage.Types;

namespace HexControl.PatternLanguage.Literals
{
    public class Char16Literal : Literal<char>
    {
        public Char16Literal(char value) : base(value) { }
        public override ulong ToUnsignedLong() => Value;

        public override long ToSignedLong() => Value;

        public override double ToDouble() => Value;

        public override bool ToBool() => Value != 0;

        public override AsciiChar ToChar() => (AsciiChar)(byte)Value;

        public override char ToChar16() => Value;

        public override string ToString() => Value.ToString();
    }
}
