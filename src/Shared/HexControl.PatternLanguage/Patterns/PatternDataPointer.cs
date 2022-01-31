using HexControl.Core;
using HexControl.Core.Helpers;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataPointer : PatternData
{
    private PatternData _pointedAt;

    public PatternDataPointer(long offset, long size, Evaluator evaluator, IntegerColor? color = null) : base(offset,
        size,
        evaluator, color)
    {
        _pointedAt = null!;
    }

    private PatternDataPointer(PatternDataPointer other) : base(other)
    {
        _pointedAt = other._pointedAt.Clone();
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

    public override IntegerColor Color
    {
        get => base.Color;
        set
        {
            base.Color = value;
            _pointedAt.Color = Color;
        }
    }

    public override PatternData Clone() => new PatternDataPointer(this);

    public override void CreateMarkers(StaticMarkerProvider markers)
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
}