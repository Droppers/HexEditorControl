using System;
using System.Collections.Generic;
using HexControl.Core;
using HexControl.Core.Helpers;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataUnion : PatternData, IInlinable
{
    private PatternData[] _members;

    public PatternDataUnion(long offset, long size, Evaluator evaluator, int color = 0)
        : base(offset, size, evaluator, color)
    {
        _members = Array.Empty<PatternData>();
    }

    private PatternDataUnion(PatternDataUnion other) : base(other)
    {
        _members = other._members.CloneAll();
    }

    public override long Offset
    {
        get => base.Offset;
        set
        {
            foreach (var member in _members)
            {
                member.Offset = member.Offset - Offset + value;
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

            for (var i = 0; i < _members.Length; i++)
            {
                var member = _members[i];
                member.Color = Color;
            }
        }
    }

    public IReadOnlyList<PatternData> Members
    {
        get => _members;
        set
        {
            _members = new PatternData[value.Count];
            for (var i = 0; i < value.Count; i++)
            {
                var member = value[i];
                if (member is null)
                {
                    continue;
                }

                member.Parent = this;
                _members[i] = member;
            }
        }
    }

    public bool Inlined
    {
        get => GetValue(BooleanValue.Inlined);
        set => SetValue(BooleanValue.Inlined, value);
    }

    public override PatternData Clone() => new PatternDataUnion(this);

    public override void CreateMarkers(List<PatternMarker> markers)
    {
        // Only take the largest member in a union type
        PatternData? largestMember = null;
        for (var i = 0; i < Members.Count; i++)
        {
            var member = Members[i];

            if (largestMember is null || largestMember.Size < member.Size)
            {
                largestMember = member;
            }
        }

        largestMember?.CreateMarkers(markers);
    }

    public override string GetFormattedName() => $"union {TypeName}";

    public override bool Equals(object? obj)
    {
        if (obj is not PatternDataUnion otherUnion)
        {
            return false;
        }

        if (_members.Length != otherUnion._members.Length)
        {
            return false;
        }

        for (var i = 0; i < _members.Length; i++)
        {
            if (!_members[i].Equals(otherUnion._members[i]))
            {
                return false;
            }
        }

        return base.Equals(obj);
    }
}