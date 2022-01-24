using System.Collections.Generic;
using HexControl.Core;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataStaticArray : PatternData, IInlinable
{
    private readonly PatternData _template = null!;

    public PatternDataStaticArray(long offset, long size, Evaluator evaluator, uint color = 0)
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
            _template = value;

            if (_template is null)
            {
                return;
            }

            _template.Endian = value.Endian;
            _template.Parent = this;

            Color = _template.Color;
        }
    }

    public int EntryCount { get; init; }
    public bool Inlined { get; set; }

    public override PatternData Clone() => new PatternDataStaticArray(this);

    public override void CreateMarkers(List<Marker> markers)
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