using System.Collections.Generic;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeEnum : AttributableASTNode
{
    private readonly List<(string, ASTNode)> _entries;
    private readonly ASTNode _underlyingType;

    public ASTNodeEnum(ASTNode underlyingType)
    {
        _entries = new List<(string, ASTNode)>();
        _underlyingType = underlyingType;
    }

    private ASTNodeEnum(ASTNodeEnum other) : base(other)
    {
        _entries = new List<(string, ASTNode)>(other._entries.Count);
        foreach (var (name, entry) in other._entries)
        {
            _entries.Add((name, entry.Clone()));
        }

        _underlyingType = other._underlyingType.Clone();
    }

    public override bool MultiPattern => false;

    public IReadOnlyList<(string, ASTNode)> Entries => _entries;

    public override ASTNode Clone() => new ASTNodeEnum(this);

    public override IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator)
    {
        return new[] {CreatePattern(evaluator)};
    }

    public override PatternData CreatePattern(Evaluator evaluator)
    {
        var underlying = _underlyingType.CreatePattern(evaluator);
        var pattern = new PatternDataEnum(underlying.Offset, underlying.Size, evaluator)
        {
            StaticData = StaticData,
            Endian = underlying.Endian
        };

        var enumEntries = new (Literal, string)[_entries.Count];
        for (var i = 0; i < _entries.Count; i++)
        {
            var (name, value) = _entries[i];
            var literal = (ASTNodeLiteral)value.Evaluate(evaluator);
            enumEntries[i] = (literal.Literal, name);
        }

        pattern.EnumValues = enumEntries;

        return pattern;
    }

    public void AddEntry(string name, ASTNode expression)
    {
        _entries.Add((name, expression));
    }
}