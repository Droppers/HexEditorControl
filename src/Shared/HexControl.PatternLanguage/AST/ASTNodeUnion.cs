using System;
using System.Collections.Generic;
using HexControl.Core.Helpers;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeUnion : AttributableASTNode
{
    private readonly List<ASTNode> _members;

    public ASTNodeUnion()
    {
        _members = new List<ASTNode>();
    }

    private ASTNodeUnion(ASTNodeUnion other) : base(other)
    {
        _members = other._members.Clone();
    }

    public override bool MultiPattern => false;

    public override ASTNode Clone() => new ASTNodeUnion(this);

    public override IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator)
    {
        return new[] {CreatePattern(evaluator)};
    }

    public override PatternData CreatePattern(Evaluator evaluator)
    {
        var pattern = new PatternDataUnion(evaluator.CurrentOffset, 0, evaluator)
        {
            StaticData = StaticData
        };

        long size = 0;
        var startOffset = evaluator.CurrentOffset;

        var memberPatterns = evaluator.PushScope(pattern).Entries;
        for (var i = 0; i < _members.Count; i++)
        {
            var member = _members[i];
            if (member.MultiPattern)
            {
                var newPatterns = member.CreatePatterns(evaluator);
                for (var j = 0; j < newPatterns.Count; j++)
                {
                    var newPattern = newPatterns[j];
                    newPattern.Offset = startOffset;
                    memberPatterns.Add(newPattern);
                    size = Math.Max(newPattern.Size, size);
                }
            }
            else
            {
                var newPattern = member.CreatePattern(evaluator);
                if (newPattern is null)
                {
                    continue;
                }

                newPattern.Offset = startOffset;
                memberPatterns.Add(newPattern);
                size = Math.Max(newPattern.Size, size);
            }
        }

        evaluator.CurrentOffset = startOffset + size;
        pattern.SetMembers(memberPatterns.ToArray());
        pattern.Size = size;

        // MUST be called AFTER setting pattern.Members, the 'memberPatterns' collection will be cleared.
        evaluator.PopScope();

        return pattern;
    }

    public void AddMember(ASTNode node)
    {
        _members.Add(node);
    }
}