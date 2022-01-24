using System.Collections.Generic;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeEnum : AttributableASTNode
{
    private readonly Dictionary<string, ASTNode> _entries;
    private readonly ASTNode _underlyingType;

    public ASTNodeEnum(ASTNode underlyingType)
    {
        _entries = new Dictionary<string, ASTNode>();
        _underlyingType = underlyingType;
    }

    private ASTNodeEnum(ASTNodeEnum other) : base(other)
    {
        _entries = new Dictionary<string, ASTNode>();
        foreach (var (name, entry) in other.Entries)
        {
            _entries.Add(name, entry.Clone());
        }

        _underlyingType = other._underlyingType.Clone();
    }

    public IReadOnlyDictionary<string, ASTNode> Entries => _entries;

    public override ASTNode Clone() => new ASTNodeEnum(this);

    public override IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator)
    {
        var pattern = new PatternDataEnum(evaluator.CurrentOffset, 0, evaluator);

        var enumEntries = new List<(Literal, string)>(_entries.Count);
        foreach (var (name, value) in _entries)
        {
            var literal = (ASTNodeLiteral)value.Evaluate(evaluator);
            enumEntries.Add((literal.Literal, name));
        }

        pattern.EnumValues = enumEntries;

        var underlying = _underlyingType.CreatePatterns(evaluator)[0];
        pattern.Size = underlying.Size;
        pattern.Endian = underlying.Endian;

        return new[] {pattern};
    }

    public void AddEntry(string name, ASTNode expression)
    {
        _entries.Add(name, expression);
    }
}