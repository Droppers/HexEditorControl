using System.Collections.Generic;
using HexControl.Core.Helpers;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeStruct : AttributableASTNode
{
    private readonly List<ASTNode> _inheritance;
    private readonly List<ASTNode> _members;

    public ASTNodeStruct()
    {
        _inheritance = new List<ASTNode>();
        _members = new List<ASTNode>();
    }

    private ASTNodeStruct(ASTNodeStruct other) : base(other)
    {
        _inheritance = other._inheritance.Clone();
        _members = other._members.Clone();
    }

    public override bool MultiPattern => false;

    public override ASTNode Clone() => new ASTNodeStruct(this);

    public override IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator)
    {
        return new[] {CreatePattern(evaluator)};
    }

    public override PatternData CreatePattern(Evaluator evaluator)
    {
        if (_members.Count == 10)
        {
            var i = 1;
        }

        var pattern = new PatternDataStruct(evaluator.CurrentOffset, 0, evaluator)
        {
            StaticData = StaticData
        };

        var startOffset = evaluator.CurrentOffset;
        var memberPatterns = evaluator.PushScope(pattern).Entries;

        for (var i = 0; i < _inheritance.Count; i++)
        {
            var inheritance = _inheritance[i];
            var inheritancePatterns = inheritance.CreatePattern(evaluator);
            if (inheritancePatterns is not PatternDataStruct structPattern)
            {
                continue;
            }

            for (var j = 0; j < structPattern.Members.Count; j++)
            {
                var member = structPattern.Members[j];
                memberPatterns.Add(member.Clone());
            }
        }

        for (var i = 0; i < _members.Count; i++)
        {
            var member = _members[i];
            if (member.MultiPattern)
            {
                var newPatterns = member.CreatePatterns(evaluator);
                for (var j = 0; j < newPatterns.Count; j++)
                {
                    var memberPattern = newPatterns[j];
                    memberPatterns.Add(memberPattern);
                }
            }
            else
            {
                var newPattern = member.CreatePattern(evaluator);
                if (newPattern is not null)
                {
                    memberPatterns.Add(newPattern);
                }
            }
        }

        pattern.SetMembers(memberPatterns.ToArray());
        pattern.Size = evaluator.CurrentOffset - startOffset;

        // MUST be called after setting "Members" property, since memberPatterns will be cleared immediately
        evaluator.PopScope();

        return pattern;
    }

    public void AddMember(ASTNode node)
    {
        _members.Add(node);
    }

    public void AddInheritance(ASTNode node)
    {
        _inheritance.Add(node);
    }
}