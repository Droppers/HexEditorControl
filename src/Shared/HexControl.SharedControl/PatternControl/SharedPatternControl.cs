using System.Drawing;
using System.Text;
using HexControl.PatternLanguage.Patterns;
using HexControl.SharedControl.Framework.Drawing;
using HexControl.SharedControl.Framework.Drawing.Text;
using HexControl.SharedControl.Framework.Host;
using HexControl.SharedControl.Framework.Host.Controls;
using HexControl.SharedControl.Framework.Host.EventArgs;
using HexControl.SharedControl.Framework.Host.Typeface;
using HexControl.SharedControl.Framework.Visual;
using HexControl.SharedControl.PatternControl.Entries;

namespace HexControl.SharedControl.PatternControl;

internal class SharedPatternControl : VisualElement
{
    public const string VerticalScrollBarName = "VerticalScrollBar";

    private const int ROW_HEIGHT = 27;

    private readonly ColumnDefinition[] _columns =
    {
        new("Name", 0.4f),
        new("Offset", 0.125f),
        new("Size", 0.125f),
        new("Type", 0.175f),
        new("Value", 0.175f)
    };

    private int _calculatedHeight;
    private bool _dividerMouseOver;

    private int _draggingColumnIndex = -1;

    private List<PatternEntry> _entries = new();

    private int _fontSize = 12;

    private SharedPoint? _mouseDown;
    private SharedPoint? _mousePosition;

    private List<PatternData>? _patterns;
    private int _scrollOffset;

    private IGlyphTypeface? _typeface;

    public SharedPatternControl() : base(true)
    {
        SizeChanged += OnSizeChanged;

        MouseWheel += OnMouseWheel;
        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;
        MouseLeave += OnMouseLeave;
    }

    public int FontSize
    {
        get => Get(ref _fontSize);
        set => Set(ref _fontSize, value);
    }

    public override double Width => Host?.Width ?? 0;
    public override double Height => Host?.Height ?? 0;

    public List<PatternData>? Patterns
    {
        get => _patterns;
        set
        {
            _entries.Clear();
            _patterns = value;

            if (value is null)
            {
                return;
            }

            _entries = CreateEntries(value);
            RecalculateHeight();
        }
    }

    private static float AntiAliasOffset => 0.5F;

    private IHostScrollBar? VerticalScrollBar => Host?.GetChild<IHostScrollBar>(VerticalScrollBarName);

    protected override void OnHostAttached(IHostControl attachHost)
    {
        InitializeScrollBar();
        RecalculateHeight();
        Invalidate();
    }

    private void InitializeScrollBar()
    {
        var scrollBar = VerticalScrollBar;
        if (scrollBar is null)
        {
            return;
        }

        scrollBar.Scroll += ScrollBarOnScroll;
    }

    private void ScrollBarOnScroll(object? sender, HostScrollEventArgs e)
    {
        _scrollOffset = -(int)e.NewValue;
        Invalidate();
    }

    private void OnMouseLeave(object? sender, HandledEventArgs e)
    {
        Cursor = null;
        _mousePosition = null;
        Invalidate();
    }

    private void OnMouseUp(object? sender, HostMouseButtonEventArgs e)
    {
        Cursor = null;
        _dividerMouseOver = false;

        if (_draggingColumnIndex is not -1)
        {
            _columns[_draggingColumnIndex - 1].PreviousWidth = _columns[_draggingColumnIndex - 1].Width;
            _columns[_draggingColumnIndex].PreviousWidth = _columns[_draggingColumnIndex].Width;
            _draggingColumnIndex = -1;
        }
        else
        {
            var clickedEntry = FindMouseOverEntry();
            if (clickedEntry is not null)
            {
                OnEntryClicked(clickedEntry);
            }
        }
    }

    private void OnEntryClicked(PatternEntry entry)
    {
        if (entry is ShowMoreEntry showMore)
        {
            var lazyEntry = showMore.Entry;
            lazyEntry.LoadMore();
            RecalculateHeight();
            Invalidate();
        }
        else if (entry.CanExpand)
        {
            entry.Expanded = !entry.Expanded;
            RecalculateHeight();
            Invalidate();
        }
    }

    private void OnMouseDown(object? sender, HostMouseButtonEventArgs e)
    {
        _mouseDown = e.Point;
        _draggingColumnIndex = -1;
        _dividerMouseOver = true;

        var left = 0;
        for (var i = 0; i < _columns.Length; i++)
        {
            var column = _columns[i];
            if (Math.Abs(left - e.Point.X) < 5 && i > 0)
            {
                _draggingColumnIndex = i;
                break;
            }

            left += (int)GetColumnWidth(column);
        }
    }

    private void OnMouseMove(object? sender, HostMouseEventArgs e)
    {
        const float minimumColumnWidth = 0.05f;

        _mousePosition = e.Point;

        if (_draggingColumnIndex is not -1 && _mouseDown is { } mouseDown)
        {
            Cursor = HostCursor.SizeWe;

            var difference = (e.Point.X - mouseDown.X) / Width;
            var newWidth1 = _columns[_draggingColumnIndex - 1].PreviousWidth + (float)difference;
            var newWidth2 = _columns[_draggingColumnIndex].PreviousWidth - (float)difference;

            if (newWidth1 > minimumColumnWidth && newWidth2 > minimumColumnWidth)
            {
                _columns[_draggingColumnIndex - 1].Width = newWidth1;
                _columns[_draggingColumnIndex].Width = newWidth2;
            }
        }
        else
        {
            _dividerMouseOver = false;
            var left = 0;
            for (var i = 0; i < _columns.Length; i++)
            {
                var column = _columns[i];
                if (Math.Abs(left - e.Point.X) < 5 && i >= 1)
                {
                    Cursor = HostCursor.SizeWe;
                    _dividerMouseOver = true;
                    break;
                }

                left += (int)GetColumnWidth(column);
            }

            if (!_dividerMouseOver)
            {
                Cursor = null;
            }
        }

        Invalidate();
    }

    private bool IsMouseOver(int row)
    {
        var lower = _scrollOffset + row * ROW_HEIGHT + ROW_HEIGHT; // add header height
        var upper = _scrollOffset + row * ROW_HEIGHT + ROW_HEIGHT * 2;
        return !_dividerMouseOver && _mousePosition is var (_, y) && y >= lower && y < upper;
    }

    private bool BeginRow(IRenderContext context, RowContext rowContext)
    {
        if (_scrollOffset + rowContext.Row * ROW_HEIGHT + ROW_HEIGHT < 0 ||
            _scrollOffset + rowContext.Row * ROW_HEIGHT > Height)
        {
            rowContext.IsVisible = false;
            return false;
        }

        context.PushTranslate(0, _scrollOffset + rowContext.Row * ROW_HEIGHT);

        var mouseOver = IsMouseOver(rowContext.Row);
        if (rowContext.OddRow || mouseOver)
        {
            context.DrawRectangle(new ColorBrush(mouseOver ? Color.DodgerBlue : Color.FromArgb(30, 34, 43)), null,
                new SharedRectangle(0, 0, Width, ROW_HEIGHT));
        }

        rowContext.IsVisible = true;
        return true;
    }

    private void EndRow(IRenderContext context, RowContext rowContext)
    {
        if (rowContext.IsVisible)
        {
            context.Pop();
        }

        rowContext.Next();
    }


    private void TextColumn(IRenderContext context, RowContext rowContext, ISharedBrush brush, string text)
    {
        if (_typeface is null)
        {
            return;
        }

        TextColumn(context, rowContext, brush, new SharedTextLayout(_typeface, FontSize, text));
    }

    private void TextColumn(IRenderContext context, RowContext rowContext, ISharedBrush brush, SharedTextLayout layout)
    {
        BeginColumn(context, rowContext);
        DrawTextMiddle(context, brush, layout);
        EndColumn(context, rowContext);
    }


    private void DrawTextMiddle(IRenderContext context, ISharedBrush brush, SharedPoint point, string text)
    {
        if (_typeface is null)
        {
            return;
        }

        DrawTextMiddle(context, brush, new SharedTextLayout(_typeface, FontSize, point)
        {
            Text = text
        });
    }

    private void DrawTextMiddle(IRenderContext context, ISharedBrush brush, SharedTextLayout layout)
    {
        layout.Position = new SharedPoint(layout.Position.X, ROW_HEIGHT / 2f - layout.Typeface.GetCapHeight(FontSize) / 2);//(int)(ROW_HEIGHT / 2f - layout.Typeface.GetCapHeight(12) / 2));
        context.DrawTextLayout(brush, layout);
    }

    private ColumnDefinition BeginColumn(IRenderContext context, RowContext rowContext)
    {
        var column = _columns[rowContext.Column];

        context.PushTranslate((int)rowContext.ColumnLeft + 5, 0);
        context.PushClip(new SharedRectangle(0, 0, GetColumnWidth(column) - 10, ROW_HEIGHT));

        return column;
    }

    private void EndColumn(IRenderContext context, RowContext rowContext)
    {
        context.Pop();
        context.Pop();

        rowContext.ColumnLeft += GetColumnWidth(_columns[rowContext.Column]);
        rowContext.Column++;
    }

    private void OnMouseWheel(object? sender, HostMouseWheelEventArgs e)
    {
        if (_calculatedHeight < Height)
        {
            return;
        }

        _scrollOffset += ROW_HEIGHT * 2 * (e.Delta > 0 ? 1 : -1);
        _scrollOffset = Math.Min(0, Math.Max((int)Height - _calculatedHeight, _scrollOffset));

        if (VerticalScrollBar is { } scrollBar)
        {
            scrollBar.Value = Math.Abs(_scrollOffset);
        }

        Invalidate();
    }

    public List<PatternEntry> CreateEntries(IReadOnlyList<PatternData> patterns, int startDepth = -1) =>
        CreateEntries(patterns, ref startDepth);

    private List<PatternEntry> CreateEntries(IReadOnlyList<PatternData> patterns, ref int currentDepth)
    {
        var patternEntries = new List<PatternEntry>();

        currentDepth++;
        foreach (var pattern in patterns)
        {
            PatternEntry? entry = null;
            if (pattern is PatternDataStruct @struct)
            {
                entry = new StructEntry(this, @struct);
                entry.Entries = CreateEntries(@struct.Members, ref currentDepth);
            }
            else if (pattern is PatternDataUnion union)
            {
                entry = new UnionEntry(this, union);
                entry.Entries = CreateEntries(union.Members, ref currentDepth);
            }
            else if (pattern is PatternDataStaticArray staticArray)
            {
                var staticArrayEntry = new StaticArrayEntry(this, staticArray);
                staticArrayEntry.Depth = currentDepth;
                staticArrayEntry.LoadMore();
                entry = staticArrayEntry;
                //dynamicArrayEntry.Entries = Populate(dynamicArray.Entries, ref currentDepth);
            }
            else if (pattern is PatternDataDynamicArray dynamicArray)
            {
                entry = new DynamicArrayEntry(this, dynamicArray);
                entry.Entries = CreateEntries(dynamicArray.Entries, ref currentDepth);
            }
            else if (pattern is PatternDataUnsigned unsigned)
            {
                entry = new UnsignedEntry(this, unsigned);
            }

            if (entry is not null)
            {
                entry.Depth = currentDepth;
                patternEntries.Add(entry);
            }
        }

        currentDepth--;

        return patternEntries;
    }

    private PatternEntry? FindMouseOverEntry(IReadOnlyList<PatternEntry>? entries = null, RowContext? context = null)
    {
        entries ??= _entries;
        context ??= new RowContext();

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (IsMouseOver(context.Row))
            {
                return entry;
            }

            context.Row++;

            var matchedEntry = entry.Expanded ? FindMouseOverEntry(entry.Entries, context) : null;
            if (matchedEntry is not null)
            {
                return matchedEntry;
            }
        }

        return null;
    }

    private void RecalculateHeight()
    {
        _calculatedHeight = 0;
        RecalculateHeight(_entries);

        var scrollBar = VerticalScrollBar;
        if (scrollBar is null)
        {
            return;
        }

        if (_calculatedHeight < Height - ROW_HEIGHT)
        {
            scrollBar.Visible = false;
        }
        else
        {
            scrollBar.Visible = true;
            scrollBar.Minimum = 0;
            scrollBar.Viewport = (int)Height;
            scrollBar.Maximum = _calculatedHeight - (int)Height;
        }
    }

    private void RecalculateHeight(IReadOnlyList<PatternEntry> entries)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            _calculatedHeight += ROW_HEIGHT;

            if (entry.Expanded)
            {
                RecalculateHeight(entry.Entries);
            }
        }
    }

    private void RenderHeader(IRenderContext context)
    {
        var left = 0f;
        var red = new ColorBrush(Color.White);
        var pen = new SharedPen(new ColorBrush(Color.FromArgb(120, 0, 0, 0)), 1);

        for (var i = 0; i < _columns.Length; i++)
        {
            var column = _columns[i];

            context.PushClip(new SharedRectangle((int)left, 0, (int)GetColumnWidth(column) - 5, ROW_HEIGHT));

            DrawTextMiddle(context, red, new SharedPoint((int)left + 5, 0), column.Name);

            context.Pop();

            left += GetColumnWidth(column);
        }


        context.DrawLine(pen, new SharedPoint(0, ROW_HEIGHT - AntiAliasOffset),
            new SharedPoint(Width, ROW_HEIGHT - AntiAliasOffset));
    }

    private void DrawArrow(IRenderContext context, ISharedBrush brush, SharedRectangle rectangle,
        ArrowDirection direction)
    {
        SharedPoint[] points;
        if (direction is ArrowDirection.Down)
        {
            points = new[]
            {
                new SharedPoint(rectangle.X, rectangle.Y),
                new SharedPoint(rectangle.X + rectangle.Width, rectangle.Y),
                new SharedPoint(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height)
            };
        }
        else
        {
            points = new[]
            {
                new SharedPoint(rectangle.X, rectangle.Y),
                new SharedPoint(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height / 2),
                new SharedPoint(rectangle.X, rectangle.Y + rectangle.Height)
            };
        }

        context.DrawPolygon(brush, null, points);
    }

    private float GetColumnWidth(ColumnDefinition column) => (float)Width * column.Width;

    private void RenderEntry(IRenderContext context, RowContext rowContext, PatternEntry entry)
    {
        var red = new ColorBrush(Color.White);

        var visible = BeginRow(context, rowContext);
        if (!visible)
        {
            EndRow(context, rowContext);
            RenderChildEntries(context, rowContext, entry);
            return;
        }

        var column = BeginColumn(context, rowContext);
        var columnWidth = (int)GetColumnWidth(column);

        var leftOffset = entry.Depth * 10;
        if (entry.CanExpand)
        {
            DrawArrow(context, red, new SharedRectangle(leftOffset, ROW_HEIGHT / 2 - 9 / 2, 9, 9),
                entry.Expanded ? ArrowDirection.Down : ArrowDirection.Right);
        }

        leftOffset += 14;
        DrawTextMiddle(context, red, new SharedPoint(leftOffset, 0), entry.FormatName());

        if (entry.CanDisplayColor)
        {
            var color = entry.Pattern.Color;
            context.DrawRectangle(new ColorBrush(Color.FromArgb(color.R, color.G, color.B)), null,
                new SharedRectangle(columnWidth - 20, ROW_HEIGHT / 2 - 10 / 2, 10, 10));
        }

        EndColumn(context, rowContext);

        if (entry is ShowMoreEntry)
        {
            EndRow(context, rowContext);
            return;
        }

        TextColumn(context, rowContext, red, $"{entry.Pattern.Offset}");
        TextColumn(context, rowContext, red, $"{entry.Pattern.Size}");
        if (entry.FormattedType.Length > 0)
        {
            TextColumn(context, rowContext, red, ColorRangesToTextLayout(entry.FormattedType));
        }
        else
        {
            BeginColumn(context, rowContext);
            EndColumn(context, rowContext);
        }

        TextColumn(context, rowContext, red, entry.FormatValue());
        EndRow(context, rowContext);

        RenderChildEntries(context, rowContext, entry);
    }

    private ISharedBrush ColorTypeToBrush(PatternEntry.ColorType type)
    {
        return new ColorBrush(type switch
        {
            PatternEntry.ColorType.Keyword => Color.FromArgb(255, 123, 114),
            PatternEntry.ColorType.Builtin => Color.FromArgb(210, 168, 255),
            PatternEntry.ColorType.Integer => Color.FromArgb(121, 192, 255),
            PatternEntry.ColorType.Regular => Color.White,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        });
    }

    private SharedTextLayout ColorRangesToTextLayout(PatternEntry.ColorRange[] ranges)
    {
        var colors = new SharedTextLayout.BrushRange[ranges.Length];
        var sb = new StringBuilder();
        var rangeOffset = 0;
        for (var i = 0; i < ranges.Length; i++)
        {
            var range = ranges[i];
            colors[i] = new SharedTextLayout.BrushRange(ColorTypeToBrush(range.Type), rangeOffset)
            {
                Length = range.Text.Length
            };
            sb.Append(range.Text);
            rangeOffset += range.Text.Length;
        }

        var layout = new SharedTextLayout(_typeface!, FontSize, sb.ToString());
        for (var i = 0; i < colors.Length; i++)
        {
            var color = colors[i];
            layout.AddRange(color);
        }

        return layout;
    }

    private void RenderChildEntries(IRenderContext context, RowContext rowContext, PatternEntry entry)
    {
        if (!entry.Expanded)
        {
            return;
        }

        for (var i = 0; i < entry.Entries.Count; i++)
        {
            var subEntry = entry.Entries[i];
            RenderEntry(context, rowContext, subEntry);
        }
    }

    private void RenderEntries(IRenderContext context)
    {
        var rowContext = new RowContext();

        context.PushTranslate(0, ROW_HEIGHT);
        context.PushClip(new SharedRectangle(0, 0, Width, Height - ROW_HEIGHT));

        foreach (var entry in _entries)
        {
            RenderEntry(context, rowContext, entry);
        }

        context.Pop();
        context.Pop();
    }

    private void RenderDividers(IRenderContext context)
    {
        var highlightBrush = new ColorBrush(Color.DodgerBlue);
        float left = 0;
        for (var i = 0; i < _columns.Length; i++)
        {
            var column = _columns[i];
            if (i >= 1)
            {
                var highlight = _draggingColumnIndex == i || _draggingColumnIndex is -1 &&
                    _mousePosition is { } position &&
                    Math.Abs(left - position.X) < 5;
                var brush = highlight ? highlightBrush : new ColorBrush(Color.FromArgb(120, 0, 0, 0));
                context.DrawLine(new SharedPen(brush, 1), new SharedPoint((int)left + AntiAliasOffset, 0),
                    new SharedPoint((int)left + AntiAliasOffset, Height));
            }

            left += GetColumnWidth(column);
        }
    }


    protected override void Render(IRenderContext context)
    {
        _typeface ??= context.Factory.CreateGlyphTypeface("Segoe UI");

        context.DrawRectangle(new ColorBrush(Color.FromArgb(24, 27, 32)), null,
            new SharedRectangle(0, 0, Width, Height));

        AddDirtyRect(new SharedRectangle(0, 0, Width, Height));

        RenderHeader(context);
        RenderEntries(context);
        RenderDividers(context);
    }

    private void OnSizeChanged(object? sender, HostSizeChangedEventArgs e)
    {
        RecalculateHeight();
        Invalidate();
    }

    private enum ArrowDirection
    {
        Right,
        Down
    }

    private record struct ColumnDefinition(string Name, float Width)
    {
        public float PreviousWidth { get; set; } = Width;
    }

    private class RowContext
    {
        public bool IsVisible { get; set; }
        public int Row { get; set; }

        public int Column { get; set; }
        public float ColumnLeft { get; set; }

        public bool OddRow { get; private set; } = true;

        public void Next()
        {
            Column = 0;
            ColumnLeft = 0;
            OddRow = !OddRow;
            Row++;
        }
    }
}