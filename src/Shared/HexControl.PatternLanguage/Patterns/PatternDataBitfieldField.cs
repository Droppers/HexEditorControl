using System;
using HexControl.Core.Helpers;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataBitfieldField : PatternData, IPatternInlinable
{
    private readonly PatternData _bitField;

    public PatternDataBitfieldField(long offset, byte bitOffset, byte bitSize, PatternData bitField,
        Evaluator evaluator, IntegerColor? color = null)
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