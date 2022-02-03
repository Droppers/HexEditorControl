using HexControl.PatternLanguage.Patterns;

namespace HexControl.SharedControl.PatternControl.Entries;

internal class UnionEntry : PatternEntry
{
    private readonly PatternDataUnion _pattern;

    public UnionEntry(SharedPatternControl tree, PatternDataUnion pattern) : base(tree, pattern)
    {
        _pattern = pattern;

        CanExpand = true;
        CanLoadMore = false;
    }

    public override ColorRange[] FormattedType => new[] {
        Keyword("union"),
        Space(),
        Regular(_pattern.TypeName ?? "?")
    };

    public override string FormatType() => $"struct {_pattern.TypeName}";

    public override string FormatValue() => "{ ... }";
}