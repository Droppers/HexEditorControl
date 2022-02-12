using HexControl.PatternLanguage.Patterns;

namespace HexControl.SharedControl.PatternControl.Entries;

internal abstract class PatternEntry
{
    public enum ColorType
    {
        Keyword,
        Builtin,
        Integer,
        Regular
    }

    protected const int LoadMoreCount = 5;

    protected PatternEntry(SharedPatternControl tree, PatternData pattern)
    {
        Tree = tree;
        Pattern = pattern;
    }

    public virtual ColorRange[] FormattedType => Array.Empty<ColorRange>();

    public SharedPatternControl Tree { get; }
    public PatternData Pattern { get; set; }
    public int Depth { get; set; } = 0;
    public bool CanExpand { get; set; } = false;
    public bool Expanded { get; set; } = false;
    public virtual bool CanLoadMore { get; set; } = false;

    public bool CanDisplayColor { get; set; }

    public List<PatternEntry> Entries { get; set; } = new();

    protected ColorRange Keyword(string text) => new ColorRange(ColorType.Keyword, text);

    protected ColorRange Integer(string text) => new ColorRange(ColorType.Integer, text);

    protected ColorRange Type(string text) => new ColorRange(ColorType.Builtin, text);

    protected ColorRange Space() => new ColorRange(ColorType.Regular, " ");

    protected ColorRange Regular(string text) => new ColorRange(ColorType.Regular, text);

    public virtual void LoadMore() { }

    public virtual string FormatName() => Pattern.DisplayName ?? "???";

    public virtual string FormatType() => "???";

    public virtual string FormatValue() => "{ ... }";

    public struct ColorRange
    {
        public ColorRange(ColorType type, string text)
        {
            Type = type;
            Text = text;
        }

        public ColorType Type { get; set; }
        public string Text { get; }
    }
}