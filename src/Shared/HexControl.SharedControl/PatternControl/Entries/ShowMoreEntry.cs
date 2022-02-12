using HexControl.PatternLanguage.Patterns;

namespace HexControl.SharedControl.PatternControl.Entries;

internal class ShowMoreEntry : PatternEntry
{
    public ShowMoreEntry(SharedPatternControl tree, PatternData pattern, PatternEntry entry) : base(tree, pattern)
    {
        Entry = entry;
    }

    public PatternEntry Entry { get; }

    public override string FormatName() => "Show more items...";
}