using System.Collections.Generic;
using HexControl.Core;
using HexControl.Core.Helpers;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataDynamicArray : PatternData, IInlinable
{
    private readonly List<PatternData> _entries;

    public PatternDataDynamicArray(long offset, long size, Evaluator evaluator, uint color = 0) : base(offset, size,
        evaluator, color)
    {
        _entries = new List<PatternData>();
    }

    private PatternDataDynamicArray(PatternDataDynamicArray other) : base(other)
    {
        _entries = other._entries.Clone();
    }

    public override long Offset
    {
        get => base.Offset;
        set
        {
            if (!Local)
            {
                foreach (var entry in _entries)
                {
                    entry.Offset = value + (entry.Offset - Offset);
                }
            }

            base.Offset = value;
        }
    }

    public IReadOnlyList<PatternData> Entries
    {
        set
        {
            _entries.Clear();

            foreach (var entry in value)
            {
                _entries.Add(entry);
                entry.Color = Color;
                entry.Parent = this;
            }
        }
        get => _entries;
    }

    public bool Inlined { get; set; }

    public override PatternData Clone() => new PatternDataDynamicArray(this);

    public override void CreateMarkers(List<Marker> markers)
    {
        foreach (var entry in _entries)
        {
            entry.CreateMarkers(markers);
        }
    }

    public override string GetFormattedName() =>
        $"{(_entries.Count is 0 ? "unknown" : _entries[0].TypeName)}[{_entries.Count}]";

    public override bool Equals(object? obj)
    {
        if (obj is not PatternDataDynamicArray otherArray)
        {
            return false;
        }

        if (_entries.Count != otherArray._entries.Count)
        {
            return false;
        }

        for (var i = 0; i < _entries.Count; i++)
        {
            if (!_entries[i].Equals(otherArray._entries[i]))
            {
                return false;
            }
        }

        return base.Equals(obj);
    }
}