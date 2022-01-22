using System.Collections.Generic;
using HexControl.PatternLanguage.Literals;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataEnum : PatternData
{
    public PatternDataEnum(long offset, long size, Evaluator evaluator, uint color = 0)
        : base(offset, size, evaluator, color)
    {
        EnumValues = new List<(Literal, string)>();
    }

    private PatternDataEnum(PatternDataEnum other) : base(other)
    {
        EnumValues = new List<(Literal, string)>(other.EnumValues);
        // TODO: should copy values?
    }

    public override PatternData Clone()
    {
        return new PatternDataEnum(this);
    }

    public override string GetFormattedName()
    {
        return $"enum {TypeName}";
    }

    public IReadOnlyList<(Literal, string)> EnumValues { set; get; }

    public override bool Equals(object? obj)
    {
        if (obj is not PatternDataEnum otherEnum)
        {
            return false;
        }

        if (EnumValues.Count != otherEnum.EnumValues.Count)
        {
            return false;
        }

        for (var i = 0; i < EnumValues.Count; i++)
        {
            if (!EnumValues[i].Equals(otherEnum.EnumValues[i]))
            {
                return false;
            }
        }

        return base.Equals(obj);
    }
}