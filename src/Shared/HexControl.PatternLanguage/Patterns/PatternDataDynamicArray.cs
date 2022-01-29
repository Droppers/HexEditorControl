using System;
using System.Collections.Generic;
using HexControl.Core;
using HexControl.Core.Helpers;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataDynamicArray : PatternData, IInlinable
{
    private PatternData[] _entries;

    public PatternDataDynamicArray(long offset, long size, Evaluator evaluator, int color = 0) : base(offset, size,
        evaluator, color)
    {
        _entries = Array.Empty<PatternData>();
    }

    private PatternDataDynamicArray(PatternDataDynamicArray other) : base(other)
    {
        _entries = other._entries.CloneAll();
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

    public override int Color
    {
        get => base.Color;
        set
        {
            base.Color = value;

            for (var i = 0; i < _entries.Length; i++)
            {
                var member = _entries[i];
                member.Color = Color;
            }
        }
    }

    public IReadOnlyList<PatternData> Entries
    {
        get => _entries;
        set
        {
            _entries = new PatternData[value.Count];

            for (var i = 0; i < value.Count; i++)
            {
                var entry = value[i];
                entry.Color = Color;
                entry.Parent = this;
                _entries[i] = entry;
            }
        }
    }

    public bool Inlined
    {
        get => GetValue(BooleanValue.Inlined);
        set => SetValue(BooleanValue.Inlined, value);
    }

    public override PatternData Clone() => new PatternDataDynamicArray(this);

    public override void CreateMarkers(List<PatternMarker> markers)
    {
        for (var i = 0; i < _entries.Length; i++)
        {
            var entry = _entries[i];
            entry.CreateMarkers(markers);
        }
    }

    public override string GetFormattedName() =>
        $"{(_entries.Length is 0 ? "unknown" : _entries[0].TypeName)}[{_entries.Length}]";

    public override bool Equals(object? obj)
    {
        if (obj is not PatternDataDynamicArray otherArray)
        {
            return false;
        }

        if (_entries.Length != otherArray._entries.Length)
        {
            return false;
        }

        for (var i = 0; i < _entries.Length; i++)
        {
            if (!_entries[i].Equals(otherArray._entries[i]))
            {
                return false;
            }
        }

        return base.Equals(obj);
    }
}