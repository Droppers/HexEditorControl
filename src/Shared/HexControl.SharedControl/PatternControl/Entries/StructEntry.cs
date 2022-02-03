using HexControl.PatternLanguage.Patterns;

namespace HexControl.SharedControl.PatternControl.Entries;

internal class StructEntry : PatternEntry
{
    private readonly PatternDataStruct _pattern;

    public StructEntry(SharedPatternControl tree, PatternDataStruct pattern) : base(tree, pattern)
    {
        _pattern = pattern;

        CanExpand = true;
        CanLoadMore = false;
    }

    public override ColorRange[] FormattedType => new[] {
        Keyword("struct"),
        Space(),
        Regular(_pattern.TypeName ?? "?")
    };

    public override string FormatType() => $"struct {_pattern.TypeName}";

    public override string FormatValue() => "{ ... }";
}