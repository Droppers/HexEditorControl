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

    public override ASTNode Clone() => new ASTNodeUnion(this);

    public override IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator)
    {
        var pattern = new PatternDataUnion(evaluator.CurrentOffset, 0, evaluator);

        long size = 0;
        var startOffset = evaluator.CurrentOffset;

        var memberPatterns = evaluator.PushScope(pattern).Entries;
        for (var i = 0; i < _members.Count; i++)
        {
            var member = _members[i];
            var patterns = member.CreatePatterns(evaluator);
            for (var j = 0; j < patterns.Count; j++)
            {
                var memberPattern = patterns[j];
                memberPattern.Offset = startOffset;
                memberPatterns.Add(memberPattern);
                size = Math.Max(memberPattern.Size, size);
            }
        }

        evaluator.PopScope();

        evaluator.CurrentOffset = startOffset + size;
        pattern.Members = memberPatterns;
        pattern.Size = size;

        return new[] {pattern};
    }

    public void AddMember(ASTNode node)
    {
        _members.Add(node);
    }
}