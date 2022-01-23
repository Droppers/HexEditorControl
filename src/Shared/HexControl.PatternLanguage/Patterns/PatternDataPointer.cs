using HexControl.Core;
using System.Collections.Generic;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataPointer : PatternData
{
    public PatternDataPointer(long offset, long size, Evaluator evaluator, uint color = 0) : base(offset, size,
        evaluator, color)
    {
        _pointedAt = null!;
    }

    private PatternDataPointer(PatternDataPointer other) : base(other)
    {
        _pointedAt = other._pointedAt.Clone();
    }

    public override PatternData Clone()
    {
        return new PatternDataPointer(this);
    }

    public override void CreateMarkers(List<Marker> markers)
    {
        base.CreateMarkers(markers);
        PointedAtPattern.CreateMarkers(markers);
    }

    public override string GetFormattedName()
    {
        var result = $"{_pointedAt.GetFormattedName()}* : ";
        switch (Size)
        {
            case 1:
                result += "u8";
                break;
            case 2:
                result += "u16";
                break;
            case 4:
                result += "u32";
                break;
            case 8:
                result += "u64";
                break;
            case 16:
                result += "u128";
                break;
        }

        return result;
    }

    public PatternData PointedAtPattern
    {
        get => _pointedAt;
        set
        {
            _pointedAt = value;
            _pointedAt.VariableName = $"*({VariableName})";
        }
    }

    public long PointedAtAddress { get; set; }

    private PatternData _pointedAt;
};