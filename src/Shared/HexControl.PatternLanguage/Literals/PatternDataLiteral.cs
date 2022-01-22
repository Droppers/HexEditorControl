using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexControl.PatternLanguage.Patterns;
using HexControl.PatternLanguage.Types;

namespace HexControl.PatternLanguage.Literals
{
    public class PatternDataLiteral : Literal<PatternData>
    {
        public PatternDataLiteral(PatternData value) : base(value) { }

        public override ulong ToUnsignedLong() => throw new Exception("cannot cast custom type to uint64");

        public override long ToSignedLong() => throw new Exception("cannot cast custom type to int64");

        public override double ToDouble() => throw new Exception("cannot cast custom type to double");

        public override bool ToBool() => throw new Exception("cannot cast custom type to bool");
        
        public override AsciiChar ToChar() => throw new Exception("cannot cast custom type to char");

        public override char ToChar16() => throw new Exception("cannot cast custom type to char16");

        public override string ToString()
        {
            return Value.ToString()!;
        }
    }
}
