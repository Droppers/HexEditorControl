using HexControl.PatternLanguage.Types;

namespace HexControl.PatternLanguage.Literals;

public class BoolLiteral : Literal<bool>, IEqualityOperations
{
    public BoolLiteral(bool value) : base(value) { }

    public override AsciiChar ToChar() => (AsciiChar)(byte)ToUnsignedLong();
    public override char ToChar16() => (char)ToUnsignedLong();

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

    public override double ToDouble() => Value ? 1 : 0;

    public override long ToSignedLong() => Value ? 1 : 0;

    public override ulong ToUnsignedLong() => (ulong)(Value ? 1 : 0);

    public override bool ToBool() => Value;

    public static implicit operator BoolLiteral(bool value) => Create(value);

    public override string ToString() => Value ? "true" : "false";
}