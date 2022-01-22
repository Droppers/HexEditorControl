using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexControl.PatternLanguage.Types;

namespace HexControl.PatternLanguage.Literals
{
    public class CharLiteral : Literal<AsciiChar>
    {
        public CharLiteral(AsciiChar value) : base(value) { }
        public override ulong ToUnsignedLong() => Value;

        public override long ToSignedLong() => Value;

        public override double ToDouble() => Value;

        public override bool ToBool() => Value != 0;

        public override AsciiChar ToChar() => Value;

        public override char ToChar16() => (char)(byte)Value;

        public override string ToString(bool cast = true) => ToChar16().ToString();
    }
}
