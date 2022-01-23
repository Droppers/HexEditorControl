using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexControl.PatternLanguage.Literals
{
    internal static class Extensions
    {
        public static UInt128Literal Create(this ulong literal)
        {
            return new UInt128Literal(literal);
        }
    }
}
