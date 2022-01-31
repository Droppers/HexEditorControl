using System;
using System.Collections.Generic;
using HexControl.Core;
using HexControl.Core.Helpers;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataStruct : PatternData, IPatternInlinable
{
    private PatternData[] _members;

    public PatternDataStruct(long offset, long size, Evaluator evaluator, IntegerColor? color = null)
        : base(offset, size, evaluator, color)
    {
        _members = Array.Empty<PatternData>();
    }

    private PatternDataStruct(PatternDataStruct other) : base(other)
    {
        _members = other._members.CloneAll();
    }

    public override long Offset
    {
        get => base.Offset;
        set
        {
            for (var i = 0; i < _members.Length; i++)
            {
                var member = _members[i];
                member.Offset = member.Offset - Offset + value;
            }

            base.Offset = value;
        }
    }

    public override IntegerColor Color
    {
        get => base.Color;
        set
        {
            base.Color = value;

            for (var i = 0; i < _members.Length; i++)
            {
                var member = _members[i];
                if (!member.UserDefinedColor)
                {
                    member.Color = Color;
                }
            }
        }
    }

    public IReadOnlyList<PatternData> Members => _members;

    //set => _members = Members.ToArray();
    //set
    //{
    //    var count = value.Count;
    //    _members = new PatternData[count];
    //    for (var i = 0; i < count; i++)
    //    {
    //        var member = value[i];
    //        member.Parent = this;
    //        _members[i] = member;
    //    }
    //}
    public void SetMembers(PatternData[] members)
    {
        _members = members;
    }

    public override PatternData Clone() => new PatternDataStruct(this);

    public override void CreateMarkers(StaticMarkerProvider markers)
    {
        for (var i = 0; i < Members.Count; i++)
        {
            var member = Members[i];
            member.CreateMarkers(markers);
        }
    }

    public override string GetFormattedName() => $"struct {TypeName}";

    public override bool Equals(object? obj)
    {
        if (obj is not PatternDataStruct otherStruct)
        {
            return false;
        }

        if (_members.Length != otherStruct._members.Length)
        {
            return false;
        }

        for (var i = 0; i < _members.Length; i++)
        {
            if (!_members[i].Equals(otherStruct._members[i]))
            {
                return false;
            }
        }

        return base.Equals(obj);
    }
}