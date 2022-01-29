using System;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataBitfieldField : PatternData, IInlinable
{
    private readonly PatternData _bitField;

    public PatternDataBitfieldField(long offset, byte bitOffset, byte bitSize, PatternData bitField,
        Evaluator evaluator, int color = 0)
        : base(offset, 0, evaluator, color)
    {
        BitOffset = bitOffset;
        BitSize = bitSize;
        _bitField = bitField;
    }

    private PatternDataBitfieldField(PatternDataBitfieldField other) : base(other)
    {
        BitOffset = other.BitOffset;
        BitSize = other.BitSize;
        _bitField = other._bitField;
    }

    public byte BitOffset { get; }
    public byte BitSize { get; }

    public bool Inlined
    {
        get => GetValue(BooleanValue.Inlined);
        set => SetValue(BooleanValue.Inlined, value);
    }

    public override PatternData Clone() => new PatternDataBitfieldField(this);

    public override string GetFormattedName() => "bits";

    public override bool Equals(object? obj)
    {
        if (obj is not PatternDataBitfieldField otherField)
        {
            return false;
        }

        return BitOffset == otherField.BitOffset && BitSize == otherField.BitSize && base.Equals(obj);
    }

    public override int GetHashCode() => HashCode.Combine(BitOffset, BitSize, base.GetHashCode());
}