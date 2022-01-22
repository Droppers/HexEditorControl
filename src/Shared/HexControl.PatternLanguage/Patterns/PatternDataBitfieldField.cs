using System;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataBitfieldField : PatternData, IInlinable
{
    public bool Inlined { get; set; }

    public PatternDataBitfieldField(long offset, byte bitOffset, byte bitSize, PatternData bitField,
        Evaluator evaluator, uint color = 0)
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

    public override PatternData Clone()
    {
        return new PatternDataBitfieldField(this);
    }
    public override string GetFormattedName()
    {
        return "bits";
    }

    public byte BitOffset { get; }

    public byte BitSize { get; }

    public override bool Equals(object? obj)
    {
        if (obj is not PatternDataBitfieldField otherField)
        {
            return false;
        }

        return BitOffset == otherField.BitOffset && BitSize == otherField.BitSize && base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(BitOffset, BitSize, base.GetHashCode());
    }

    private readonly PatternData _bitField;
}