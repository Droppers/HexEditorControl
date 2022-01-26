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

    public override ASTNode Clone() => new ASTNodeStruct(this);

    public override IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator)
    {
        var pattern = new PatternDataStruct(evaluator.CurrentOffset, 0, evaluator);

        var startOffset = evaluator.CurrentOffset;
        var memberPatterns = evaluator.PushScope(pattern).Entries;

        for (var i = 0; i < _inheritance.Count; i++)
        {
            var inheritance = _inheritance[i];
            var inheritancePatterns = inheritance.CreatePatterns(evaluator)[0];
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
            var list = member.CreatePatterns(evaluator);
            for (var j = 0; j < list.Count; j++)
            {
                var memberPattern = list[j];
                memberPatterns.Add(memberPattern);
            }
        }

        evaluator.PopScope();

        pattern.Members = memberPatterns;
        pattern.Size = evaluator.CurrentOffset - startOffset;

        return new[] {pattern};
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