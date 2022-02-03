using HexControl.PatternLanguage.Patterns;

namespace HexControl.SharedControl.PatternControl.Entries;

internal class UnsignedEntry : PatternEntry
{
    private readonly PatternDataUnsigned _pattern;

    public UnsignedEntry(SharedPatternControl tree, PatternDataUnsigned pattern) : base(tree, pattern)
    {
        _pattern = pattern;
        CanDisplayColor = true;
    }

    public override ColorRange[] FormattedType => new ColorRange[]
    {
        Type(_pattern.TypeName ?? "?")
    };

    public override string FormatType() => $"{_pattern.TypeName}";
}