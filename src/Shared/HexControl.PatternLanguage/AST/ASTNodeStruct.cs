using System.Collections.Generic;
using System.Linq;
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
        List<PatternData> memberPatterns = new();

        evaluator.PushScope(pattern, memberPatterns);

        foreach (var inheritance in _inheritance)
        {
            var inheritancePatterns = inheritance.CreatePatterns(evaluator)[0];
            if (inheritancePatterns is PatternDataStruct structPattern)
            {
                foreach (var member in structPattern.Members)
                {
                    memberPatterns.Add(member.Clone());
                }
            }
        }

        foreach (var member in _members)
        {
            foreach (var memberPattern in member.CreatePatterns(evaluator))
            {
                memberPatterns.Add(memberPattern);
            }
        }

        evaluator.PopScope();

        pattern.Members = memberPatterns;
        pattern.Size = (evaluator.CurrentOffset - startOffset);

        return new [] {pattern};
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