using HexControl.PatternLanguage.Patterns;

namespace HexControl.SharedControl.PatternControl.Entries;

internal class DynamicArrayEntry : PatternEntry
{
    private readonly PatternDataDynamicArray _pattern;

    public DynamicArrayEntry(SharedPatternControl tree, PatternDataDynamicArray pattern) : base(tree, pattern)
    {
        _pattern = pattern;
        CanDisplayColor = true;
        CanExpand = true;
        CanLoadMore = false;
    }

    public override ColorRange[] FormattedType => new[]
    {
        Type(_pattern.TypeName ?? "?"),
        Regular("["),
        Integer(_pattern.Entries.Count.ToString()),
        Regular("]")
    };

    public override string FormatType() => $"{_pattern.TypeName}[{_pattern.Entries.Count}]";

    public override string FormatValue() => "[ ... ]";
}