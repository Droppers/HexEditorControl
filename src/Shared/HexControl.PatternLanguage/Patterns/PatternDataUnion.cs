using System.Collections.Generic;
using HexControl.Core;
using HexControl.Core.Helpers;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataUnion : PatternData, IInlinable
{
    private readonly List<PatternData> _members;

    public PatternDataUnion(long offset, long size, Evaluator evaluator, uint color = 0)
        : base(offset, size, evaluator, color)
    {
        _members = new List<PatternData>();
    }

    private PatternDataUnion(PatternDataUnion other) : base(other)
    {
        _members = other._members.Clone();
    }

    public override long Offset
    {
        get => base.Offset;
        set
        {
            if (!Local)
            {
                foreach (var member in _members)
                {
                    member.Offset = value + (member.Offset - Offset);
                }
            }

            base.Offset = value;
        }
    }

    public IReadOnlyList<PatternData> Members
    {
        get => _members;
        set
        {
            _members.Clear();
            foreach (var member in value)
            {
                // TODO: Is this safe to remove?
                //if (member is null)
                //{
                //    continue;
                //}

                _members.Add(member);
                member.Parent = this;
            }
        }
    }

    public bool Inlined { get; set; }

    public override PatternData Clone() => new PatternDataUnion(this);

    public override void CreateMarkers(List<Marker> markers)
    {
        foreach (var member in Members)
        {
            member.CreateMarkers(markers);
        }
    }

    public override string GetFormattedName() => $"union {TypeName}";

    public override bool Equals(object? obj)
    {
        if (obj is not PatternDataUnion otherUnion)
        {
            return false;
        }

        if (_members.Count != otherUnion._members.Count)
        {
            return false;
        }

        for (var i = 0; i < _members.Count; i++)
        {
            if (!_members[i].Equals(otherUnion._members[i]))
            {
                return false;
            }
        }

        return base.Equals(obj);
    }
}