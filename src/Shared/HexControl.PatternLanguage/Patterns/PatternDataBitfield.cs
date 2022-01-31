using System;
using System.Collections.Generic;
using HexControl.Core.Helpers;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataBitfield : PatternData
{
    private PatternData[] _fields;

    public PatternDataBitfield(long offset, long size, Evaluator evaluator, IntegerColor? color = null)
        : base(offset, size, evaluator, color)
    {
        _fields = Array.Empty<PatternData>();
    }

    private PatternDataBitfield(PatternDataBitfield other) : base(other)
    {
        _fields = other._fields.CloneAll();
    }

    public IReadOnlyList<PatternData> Fields
    {
        get => _fields;
        set
        {
            _fields = new PatternData[value.Count];

            for (var i = 0; i < value.Count; i++)
            {
                var field = value[i];
                field.Size = Size;
                field.Color = Color;
                field.Parent = this;

                _fields[i] = field;
            }
        }
    }

    public override long Offset
    {
        get => base.Offset;
        set
        {
            foreach (var member in _fields)
            {
                member.Offset = member.Offset - Offset + value;
            }

            base.Offset = value;
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

        if (_fields.Length != otherField._fields.Length)
        {
            return false;
        }

        for (var i = 0; i < _fields.Length; i++)
        {
            if (!_fields[i].Equals(otherField._fields[i]))
            {
                return false;
            }
        }

        return base.Equals(obj);
    }
}