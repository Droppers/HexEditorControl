using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexControl.PatternLanguage.Types
{
    // A wrapper around a byte to represent a single byte character, since this does not exist in C#.
    public readonly struct AsciiChar
    {
        private byte Value { get; }

        public AsciiChar(byte value)
        {
            Value = value;
        }

        public static implicit operator byte(AsciiChar d) => d.Value;
        public static explicit operator AsciiChar(byte b) => new(b);
    }
}
