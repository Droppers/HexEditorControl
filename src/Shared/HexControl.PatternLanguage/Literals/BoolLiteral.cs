using HexControl.Core.Numerics;
using HexControl.PatternLanguage.Types;

namespace HexControl.PatternLanguage.Literals;

public class BoolLiteral : Literal<bool>, IEqualityOperations
{
    public BoolLiteral(bool value) : base(value) { }

    public BoolLiteral Equal(Literal other) => Value == other.ToBool();

    public BoolLiteral Greater(Literal other) => Value && !other.ToBool();

    public BoolLiteral GreaterOrEqual(Literal other) => true;

    public BoolLiteral Less(Literal other) => !Value && other.ToBool();

    public BoolLiteral LessOrEqual(Literal other) => true;

    public BoolLiteral NotEqual(Literal other) => Value != other.ToBool();

    public BoolLiteral And(Literal other) => Value && other.ToBool();

    public BoolLiteral Xor(Literal other)
    {
        var otherValue = other.ToBool();
        return Value && !otherValue || !Value && otherValue;
    }

    public BoolLiteral Or(Literal other) => Value || other.ToBool();

    public BoolLiteral Not(Literal other) => !Value && !other.ToBool();

    public override AsciiChar ToChar() => (AsciiChar)(byte)ToUInt128();
    public override char ToChar16() => (char)ToUInt128();

    public override double ToDouble() => Value ? 1 : 0;

    public override Int128 ToInt128() => Value ? 1 : 0;

    public override UInt128 ToUInt128() => (ulong)(Value ? 1 : 0);

    public override long ToInt64() => Value ? 1 : 0;
    public override ulong ToUInt64() => (ulong)(Value ? 1 : 0);

    public override bool ToBool() => Value;

    public static implicit operator BoolLiteral(bool value) => Create(value);

    public override string ToString() => Value ? "true" : "false";
}