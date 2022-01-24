using System.Collections.Generic;
using HexControl.Core.Helpers;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataBitfield : PatternData
{
    private readonly List<PatternData> _fields;

    public PatternDataBitfield(long offset, long size, Evaluator evaluator, uint color = 0)
        : base(offset, size, evaluator, color)
    {
        _fields = new List<PatternData>();
    }

    private PatternDataBitfield(PatternDataBitfield other) : base(other)
    {
        _fields = other._fields.Clone();
    }

    public IReadOnlyList<PatternData> Fields
    {
        get => _fields;
        set
        {
            _fields.Clear();

            foreach (var field in value)
            {
                _fields.Add(field);
                field.Size = Size;
                field.Color = Color;
                field.Parent = this;
            }
        }
    }


    public override PatternData Clone() => new PatternDataBitfield(this);

    public override string GetFormattedName() => $"bitfield {TypeName}";

    public override bool Equals(object? obj)
    {
        if (obj is not PatternDataBitfield otherField)
        {
            return false;
        }

        if (_fields.Count != otherField._fields.Count)
        {
            return false;
        }

        for (var i = 0; i < _fields.Count; i++)
        {
            if (!_fields[i].Equals(otherField._fields[i]))
            {
                return false;
            }
        }

        return base.Equals(obj);
    }
}