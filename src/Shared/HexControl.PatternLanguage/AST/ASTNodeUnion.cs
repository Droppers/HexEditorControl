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
        var memberPatterns = new List<PatternData>();
        var startOffset = evaluator.CurrentOffset;

        evaluator.PushScope(pattern, memberPatterns);
        foreach (var member in _members)
        {
            foreach (var memberPattern in member.CreatePatterns(evaluator))
            {
                memberPattern.Offset = (startOffset);
                memberPatterns.Add(memberPattern);
                size = Math.Max(memberPattern.Size, size);
            }
        }

        evaluator.PopScope();

        evaluator.CurrentOffset = startOffset + size;
        pattern.Members = memberPatterns;
        pattern.Size = (size);

        return new [] {pattern};
    }

    public void AddMember(ASTNode node)
    {
        _members.Add(node);
    }
}