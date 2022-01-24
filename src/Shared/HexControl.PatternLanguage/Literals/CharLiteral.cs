using HexControl.Core.Numerics;
using HexControl.PatternLanguage.Types;

namespace HexControl.PatternLanguage.Literals;

public class CharLiteral : Literal<AsciiChar>
{
    public CharLiteral(AsciiChar value) : base(value) { }
    public override UInt128 ToUInt128() => (byte)Value;

    public override Int128 ToInt128() => (byte)Value;

    public override long ToInt64() => Value;
    public override ulong ToUInt64() => Value;

    public override double ToDouble() => Value;

    public override bool ToBool() => Value != 0;

    public override AsciiChar ToChar() => Value;

    public override char ToChar16() => (char)(byte)Value;

    public override string ToString(bool cast = true) => ToChar16().ToString();
}