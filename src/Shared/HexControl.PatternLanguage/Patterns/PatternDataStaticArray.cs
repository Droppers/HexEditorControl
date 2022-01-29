using System.Collections.Generic;
using HexControl.Core;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataStaticArray : PatternData, IPatternInlinable
{
    private readonly PatternData _template = null!;

    public PatternDataStaticArray(long offset, long size, Evaluator evaluator, int color = 0)
        : base(offset, size, evaluator, color)
    {
        // TODO: Remove whenever required init is a thing
        Template = null!;
    }

    private PatternDataStaticArray(PatternDataStaticArray other) : base(other)
    {
        Template = other.Template.Clone();
        EntryCount = other.EntryCount;
    }

    public PatternData Template
    {
        get => _template;
        init
        {
            if (value is not null)
            {

                _template = value;
                _template.Endian = value.Endian;
                _template.Parent = this;

                if (_template.UserDefinedColor)
                {
                    Color = _template.Color;
                }
            }
        }
    }

    public override long Offset
    {
        get => base.Offset;
        set
        {
            base.Offset = Offset;
            _template.Offset = Offset;
        }
    }

    public override int Color
    {
        get => base.Color;
        set
        {
            base.Color = Color;
            _template.Color = Color;
        }
    }

    public int EntryCount { get; init; }
    
    public override PatternData Clone() => new PatternDataStaticArray(this);

    public override void CreateMarkers(List<PatternMarker> markers)
    {
        var entry = Template.Clone();

        for (var address = Offset; address < Offset + Size; address += entry.Size)
        {
            entry.Offset = address;
            entry.CreateMarkers(markers);
        }
    }

    public override string GetFormattedName() => $"{Template.TypeName}[{EntryCount}]";

    public override bool Equals(object? obj)
    {
        if (obj is not PatternDataStaticArray otherArray)
        {
            return false;
        }

        return Template.Equals(otherArray.Template) && EntryCount == otherArray.EntryCount &&
               base.Equals(obj);
    }
}