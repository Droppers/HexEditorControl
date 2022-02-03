using HexControl.PatternLanguage.Patterns;

namespace HexControl.SharedControl.PatternControl.Entries;

internal class StaticArrayEntry : PatternEntry
{
    private readonly PatternDataStaticArray _pattern;

    public StaticArrayEntry(SharedPatternControl tree, PatternDataStaticArray pattern) : base(tree, pattern)
    {
        _pattern = pattern;
        CanDisplayColor = true;
        CanExpand = true;
    }

    public override ColorRange[] FormattedType => new[]
    {
        Type(_pattern.TypeName ?? "?"),
        Regular("["),
        Integer(_pattern.EntryCount.ToString()),
        Regular("]")
    };

    public override bool CanLoadMore => _pattern.EntryCount - Entries.Count > 0;

    public override void LoadMore()
    {
        if (!CanLoadMore)
        {
            return;
        }

        var entriesToLoad = Math.Min(_pattern.EntryCount - Entries.Count, LoadMoreCount);
        var newPatterns = new PatternData[entriesToLoad];
        for (var i = 0; i < entriesToLoad; i++)
        {
            var pattern = _pattern.Template.Clone();
            var index = Entries.Count + i;
            pattern.ArrayIndex = index;
            pattern.Offset = _pattern.Offset + index * pattern.Size;
            newPatterns[i] = pattern;
        }

        if (Entries.Count > 0)
        {
            Entries.RemoveAt(Entries.Count - 1);
        }

        Entries.AddRange(Tree.CreateEntries(newPatterns, Depth));

        if (entriesToLoad >= LoadMoreCount)
        {
            Entries.Add(new ShowMoreEntry(Tree, _pattern, this) {Depth = Depth + 1});
        }
    }

    public override string FormatType() => $"{_pattern.TypeName}[{_pattern.EntryCount}]";

    public override string FormatValue() => "[ ... ]";
}