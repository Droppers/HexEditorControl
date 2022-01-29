using HexControl.Core.Numerics;
using HexControl.PatternLanguage.Helpers;

namespace HexControl.PatternLanguage.Literals;

public class Char16Literal : Literal<char>
{
    public Char16Literal(char value) : base(value) { }
    public override UInt128 ToUInt128() => Value;

    public override Int128 ToInt128() => Value;

    public override long ToInt64() => Value;
    public override ulong ToUInt64() => Value;

    public override double ToDouble() => Value;

    public override bool ToBool() => Value != 0;

    public override AsciiChar ToChar() => (AsciiChar)(byte)Value;

    public override char ToChar16() => Value;

    public override string ToString() => Value.ToString();
}