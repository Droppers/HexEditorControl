using System.Drawing;
using HexControl.Framework.Observable;
using HexControl.SharedControl.Control.Helpers;
using HexControl.SharedControl.Documents.Helpers;
using HexControl.Framework.Drawing;
using HexControl.Framework.Drawing.Text;
using HexControl.Framework.Host;
using HexControl.Framework.Host.Controls;
using HexControl.Framework.Host.Typeface;
using HexControl.Framework.Visual;
using HexControl.SharedControl.Characters;
using Caret = HexControl.SharedControl.Documents.Caret;
using ColumnSide = HexControl.SharedControl.Documents.ColumnSide;
using Document = HexControl.SharedControl.Documents.Document;
using DocumentConfiguration = HexControl.SharedControl.Documents.DocumentConfiguration;
using IDocumentMarker = HexControl.SharedControl.Documents.IDocumentMarker;
using Marker = HexControl.SharedControl.Documents.Marker;
using NewCaretLocation = HexControl.SharedControl.Documents.NewCaretLocation;
using Timer = System.Timers.Timer;
using VisibleColumns = HexControl.SharedControl.Documents.VisibleColumns;
using HexControl.Buffers;
using HexControl.Framework.Host.Events;
using HexControl.Framework.Optimizations;

namespace HexControl.SharedControl.Control.Elements;

internal class EditorColumn : VisualElement
{
    private const int SPACING_BETWEEN_COLUMNS = 2;

    private readonly char[] _characterBuffer;
    private readonly ObjectCache<Color, ISharedBrush> _colorToBrushCache;

    private readonly SharedHexControl _parent;
    private readonly Dictionary<IDocumentMarker, MarkerRange> _previousMarkers;
    private readonly byte[] _readBuffer;

    private readonly SynchronizationContext? _syncContext;

    private ColumnSide _activeColumn = ColumnSide.Left;

    private IDocumentMarker? _activeMarker;
    private bool _caretTick;

    private Timer? _caretTimer;
    private bool _caretUpdated;
    private DocumentConfiguration _configuration = DocumentConfiguration.Default;

    private Document? _document;
    private int _horizontalCharacterOffset;

    private int _horizontalOffset;
    private IDocumentMarker? _inactiveMarker;

    private bool _keyboardSelectMode;
    private CharacterSet _leftCharacterSet = null!;
    private ISharedBrush?[] _markerForegroundLookup;

    private SharedPoint? _mouseDownPosition;
    private bool _mouseSelectMode;
    private long _previousCaretOffset;
    private CharacterSet? _rightCharacterSet;
    private long? _startSelectionOffset;

    public EditorColumn(
        SharedHexControl parent)
    {
        _parent = parent;

        _colorToBrushCache = new ObjectCache<Color, ISharedBrush>(color => new ColorBrush(color));
        _markerForegroundLookup = Array.Empty<ISharedBrush?>();
        _characterBuffer = new char[8];
        _readBuffer = new byte[8];
        _previousMarkers = new Dictionary<IDocumentMarker, MarkerRange>(50);

        Bytes = Array.Empty<byte>();
        Modifications = new List<ModifiedRange>(25);

        Configuration = new DocumentConfiguration();

        _syncContext = SynchronizationContext.Current;
    }

    public long MaxVisibleOffset
    {
        get
        {
            var height = Height - _parent.HeaderHeight;
            var rows = height / _parent.RowHeight;
            var offset = (long)rows * Configuration.BytesPerRow;
            offset = Offset + Math.Min(offset, Bytes.Length);
            return (long)(Math.Ceiling(offset / (double)Configuration.BytesPerRow) * Configuration.BytesPerRow);
        }
    }

    public Document? Document
    {
        get => _document;
        set
        {
            _document = value;
            OnDocumentChanged();
        }
    }

    public DocumentConfiguration Configuration
    {
        get => _configuration;
        set
        {
            _configuration.PropertyChanged -= OnPropertyChanged;
            _configuration = value;
            _configuration.PropertyChanged += OnPropertyChanged;

            OnConfigurationChanged();
        }
    }

    public int TotalWidth =>
        GetColumnCharacterCount(ColumnSide.Left) +
        (Configuration.ColumnsVisible is VisibleColumns.HexText
            ? GetColumnCharacterCount(ColumnSide.Right) + SPACING_BETWEEN_COLUMNS
            : 0);

    public long Offset { get; set; }
    public byte[] Bytes { get; set; }
    public List<ModifiedRange> Modifications { get; }

    public int HorizontalOffset
    {
        get => _horizontalOffset;
        set
        {
            _horizontalOffset = value;
            _horizontalCharacterOffset = GetFirstVisibleColumn();
        }
    }

    private bool IsReadOnly => Document?.Buffer.IsReadOnly == true;

    public ITextBuilder? TextBuilder { get; set; }

    private int RowHeight => _parent.RowHeight;
    private int CharacterWidth => _parent.CharacterWidth;

    protected override void OnHostAttached(IHostControl attachHost)
    {
        InitCaretTimer();
        InitTextBox();
    }

    private void InitCaretTimer()
    {
        _caretTimer = new Timer
        {
            Interval = 500,
            Enabled = true
        };
        _caretTimer.Elapsed += (_, _) =>
        {
            _caretTick = !_caretTick;
            _caretUpdated = true;

            if (_syncContext is not null)
            {
                _syncContext.Post(_ => Invalidate(), null);
            }
            else
            {
                Invalidate();
            }
        };
    }

    private void InitTextBox()
    {
        var textBox = Host?.GetChild<IHostTextBox>(SharedHexControl.FakeTextBoxName);
        if (textBox is null)
        {
            return;
        }

        textBox.TextChanged += TextBoxOnTextChanged;
        textBox.KeyDown += TextBoxOnKeyDown;
        textBox.KeyUp += TextBoxOnKeyUp;
    }

    protected override void OnTreeAttached()
    {
        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;
        MouseLeave += OnMouseLeave;
    }

    protected override void OnTreeDetached()
    {
        MouseDown -= OnMouseDown;
        MouseMove -= OnMouseMove;
        MouseUp -= OnMouseUp;
        MouseLeave -= OnMouseLeave;
    }

    private void OnDocumentChanged()
    {
        UpdateCharacterSets();
    }

    private void OnConfigurationChanged()
    {
        UpdateCharacterSets();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.Property is nameof(DocumentConfiguration.ColumnsVisible) or nameof(DocumentConfiguration.LeftCharacterSet)
            or nameof(DocumentConfiguration.RightCharacterSet))
        {
            UpdateCharacterSets();
        }
    }

    private void UpdateCharacterSets()
    {
        _leftCharacterSet = Configuration.ColumnsVisible is VisibleColumns.Hex or VisibleColumns.HexText
            ? Configuration.LeftCharacterSet
            : Configuration.RightCharacterSet;

        _rightCharacterSet = Configuration.ColumnsVisible is VisibleColumns.HexText
            ? Configuration.RightCharacterSet
            : null;
    }

    private int GetVisibleColumnWidth(ColumnSide column)
    {
        var startColumn = 0;
        if (column is ColumnSide.Left)
        {
            startColumn = _horizontalCharacterOffset;
        }
        else if (_horizontalCharacterOffset > Configuration.BytesPerRow)
        {
            startColumn = _horizontalCharacterOffset - Configuration.BytesPerRow;
        }

        return GetColumnCharacterCount(column) * CharacterWidth - GetLeft(Math.Max(0, startColumn), column);
    }

    private int GetColumnCharacterCount(ColumnSide column)
    {
        var characterSet = GetCharacterSetForColumn(column);
        if (!characterSet.Groupable)
        {
            return Configuration.BytesPerRow * characterSet.Width;
        }

        return Configuration.BytesPerRow * characterSet.Width + Configuration.BytesPerRow / Configuration.GroupSize -
               1;
    }

    private int GetLeft(int offsetFromLeft, ColumnSide column, bool excludeLastGroup = false)
    {
        var isLastOfGroup = offsetFromLeft % Configuration.GroupSize == 0;
        var characterSet = GetCharacterSetForColumn(column);
        var groups = Math.Max(0, (characterSet.Groupable ? offsetFromLeft / Configuration.GroupSize : 0) -
                                 (excludeLastGroup && characterSet.Groupable && isLastOfGroup ? 1 : 0));
        return (offsetFromLeft * characterSet.Width + groups) * CharacterWidth;
    }

    protected override void Render(IRenderContext context)
    {
        if (Document is null)
        {
            return;
        }

        // 'High performance' static markers (do not change when modifying document)
        ResizeForegroundLookup();
        DrawStaticMarkers(Document, context);

        // Different foreground for modified bytes
        UpdateModificationBrushOverrides();

        // Draw the markers that should be drawn behind the text
        UpdateSelectionMarkers();
        DrawMarkers(context, marker => marker.BehindText);


        if (TextBuilder is not null)
        {
            TextBuilder.Clear();
            if (Configuration.ColumnsVisible is VisibleColumns.Hex or VisibleColumns.HexText &&
                _leftCharacterSet.Groupable)
            {
                WriteHexOffsetHeader(TextBuilder);
            }

            if (Configuration.ColumnsVisible is VisibleColumns.Text or VisibleColumns.HexText)
            {
                WriteTextHeader(TextBuilder);
            }

            WriteContentBytes(TextBuilder);
            TextBuilder.Draw(context);
        }

        // Draw the markers that should be drawn in front of the text
        DrawMarkers(context, marker => !marker.BehindText);

        // Draw the carets
        DrawCarets(context, Document);
    }

    private bool ShouldAddMarkerDirtyRect()
    {
        var count = GetMarkerCount();
        if (count != _previousMarkers.Count)
        {
            return true;
        }

        for (var i = 0; i < count; i++)
        {
            var marker = GetMarkerAt(i);
            if (_previousMarkers.TryGetValue(marker, out var previousRange))
            {
                if (previousRange.Offset != marker.Offset || previousRange.Length != marker.Length)
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateSelectionMarkers()
    {
        if (Document?.Selection is null)
        {
            return;
        }

        // Active
        _activeMarker ??= new Marker(Document.Selection.Start, Document.Selection.Length);
        _activeMarker.Offset = Document.Selection.Start;
        _activeMarker.Length = Document.Selection.Length;
        _activeMarker.Foreground = Color.White;
        _activeMarker.Background = Color.FromArgb(255, 21, 103, 210);
        _activeMarker.BehindText = true;
        _activeMarker.Column = Document.Selection.Column is ColumnSide.Left ? ColumnSide.Left : ColumnSide.Right;

        // Inactive
        _inactiveMarker ??= new Marker(Document.Selection.Start, Document.Selection.Length);
        _inactiveMarker.Offset = Document.Selection.Start;
        _inactiveMarker.Length = Document.Selection.Length;
        _inactiveMarker.Border = Color.FromArgb(255, 21, 103, 210);
        _inactiveMarker.Background = Color.FromArgb(100, 21, 103, 210);
        _inactiveMarker.BehindText = true;
        _inactiveMarker.Column = Document.Selection.Column is ColumnSide.Right ? ColumnSide.Left : ColumnSide.Right;
    }

    private void ResizeForegroundLookup()
    {
        if (Bytes.Length * 2 > _markerForegroundLookup.Length)
        {
            _markerForegroundLookup = new ISharedBrush[Bytes.Length * 2];
        }
        else
        {
            for (var i = 0; i < _markerForegroundLookup.Length; i++)
            {
                _markerForegroundLookup[i] = null;
            }
        }
    }

    private void UpdateModificationBrushOverrides()
    {
        var rightOffset = _markerForegroundLookup.Length / 2;
        for (var i = 0; i < Modifications.Count; i++)
        {
            var modification = Modifications[i];
            var startOffset = Math.Max(0, modification.StartOffset - Offset);
            var length = Math.Min(Bytes.Length - startOffset,
                modification.Length - (modification.StartOffset < Offset ? Offset - modification.StartOffset : 0));
            for (var j = 0; j < length; j++)
            {
                _markerForegroundLookup[startOffset + j] = _parent.ModifiedForeground;
                _markerForegroundLookup[rightOffset + startOffset + j] = _parent.ModifiedForeground;
            }
        }
    }

    private static Color? DetermineTextColor(Color? background)
    {
        if (background is null || background.Value.A != 255)
        {
            return null;
        }

        var threshold = 105;
        var delta = Convert.ToInt32(background.Value.R * 0.299 + background.Value.G * 0.587 +
                                    background.Value.B * 0.114);

        return 255 - delta < threshold ? Color.Black : Color.White;
    }

    private int GetMarkerCount()
    {
        if (Document is null)
        {
            return 0;
        }

        if (Document.Selection is null || Document.Selection.End < Offset ||
            Document.Selection.Start > Offset + Bytes.Length || Document.Selection.Length <= 0)
        {
            return Document.Markers.Count;
        }

        return Document.Markers.Count + 2;
    }

    private IDocumentMarker GetMarkerAt(int index)
    {
        var count = Document?.Markers.Count ?? 0;
        if (index < count)
        {
            return Document?.Markers[index]!;
        }

        var selectionIndex = index - count;
        return selectionIndex switch
        {
            0 => _activeMarker!,
            1 => _inactiveMarker!,
            _ => throw new IndexOutOfRangeException("Marker index out of range.")
        };
    }

    private void UpdateMarkerBrushOverrides(IDocumentMarker marker)
    {
        var rightOffset = _markerForegroundLookup.Length / 2;
        var foreground = marker.Foreground ?? DetermineTextColor(marker.Background);
        if (foreground is null || _colorToBrushCache[foreground.Value] is not { } foregroundBrush)
        {
            return;
        }

        var startOffset = Math.Max(0, marker.Offset - Offset);
        var length = Math.Min(Bytes.Length - startOffset,
            marker.Length - (marker.Offset < Offset ? Offset - marker.Offset : 0));
        for (var j = 0; j < length; j++)
        {
            if (marker.Column is ColumnSide.Both)
            {
                _markerForegroundLookup[startOffset + j] = foregroundBrush;
                _markerForegroundLookup[rightOffset + startOffset + j] = foregroundBrush;
            }
            else
            {
                var offset = marker.Column is ColumnSide.Left ? 0 : rightOffset;
                _markerForegroundLookup[offset + startOffset + j] = foregroundBrush;
            }
        }
    }

    private void DrawStaticMarkers(Document document, IRenderContext context)
    {
        if (document.StaticMarkerProvider is null)
        {
            return;
        }

        ResizeForegroundLookup();

        var rightOffset = _markerForegroundLookup.Length / 2;

        // A fake marker that can be passed to the drawing methods (used as drawing parameters)
        var fakeMarker = new Marker(0, 0);
        var startOffset = -1L;
        var startColor = IntegerColor.Zero;
        for (var i = Offset; i < Offset + Bytes.Length; i++)
        {
            var color = document.StaticMarkerProvider.Lookup(i);

            if (color?.A is 255)
            {
                var c = color.Value;
                var foreground = DetermineTextColor(Color.FromArgb(c.A, c.R, c.G, c.B));

                if (foreground is not null && _colorToBrushCache[foreground.Value] is { } foregroundBrush)
                {
                    var relative = i - Offset;
                    _markerForegroundLookup[relative] = foregroundBrush;
                    _markerForegroundLookup[rightOffset + relative] = foregroundBrush;
                }
            }

            if (startOffset != -1 && (color is null || !color.Value.Equals(startColor)))
            {
                fakeMarker.Offset = startOffset;
                fakeMarker.Length = i - startOffset;
                fakeMarker.Background = Color.FromArgb(startColor.A, startColor.R, startColor.G, startColor.B);
                DrawLeftMarker(context, fakeMarker);
                DrawRightMarker(context, fakeMarker);
                startOffset = -1;
            }

            if (startOffset is -1 && color is not null)
            {
                startOffset = i;
                startColor = color.Value;
            }
        }
    }

    private bool IsMarkerVisible(long offset, long length) =>
        offset + length > Offset && offset < Offset + Bytes.Length;

    private bool IsMarkerVisible(IDocumentMarker marker) => IsMarkerVisible(marker.Offset, marker.Length);

    private void DrawMarkers(IRenderContext context, Func<IDocumentMarker, bool> condition)
    {
        if (ShouldAddMarkerDirtyRect())
        {
            AddDirtyRect(new SharedRectangle(0, 0, TotalWidth * CharacterWidth, Height), CharacterWidth);
        }

        _previousMarkers.Clear();

        for (var i = 0; i < GetMarkerCount(); i++)
        {
            var marker = GetMarkerAt(i);
            if (!IsMarkerVisible(marker))
            {
                continue;
            }

            _previousMarkers[marker] = new MarkerRange(marker.Offset, marker.Length);

            if (!condition(marker))
            {
                continue;
            }

            UpdateMarkerBrushOverrides(marker);

            DrawLeftMarker(context, marker);
            DrawRightMarker(context, marker);

            _previousMarkers[marker] = new MarkerRange(marker.Offset, marker.Length);
        }
    }

    private void DrawLeftMarker(IRenderContext context, IDocumentMarker marker)
    {
        if (_horizontalCharacterOffset >= Configuration.BytesPerRow ||
            marker.Column is not (ColumnSide.Left or ColumnSide.Both))
        {
            return;
        }

        DrawMarkerArea(context, ColumnSide.Left, marker);
    }

    private void DrawRightMarker(IRenderContext context, IDocumentMarker marker)
    {
        if (marker.Column is ColumnSide.Left)
        {
            return;
        }

        if (_horizontalCharacterOffset < Configuration.BytesPerRow)
        {
            var leftOffset = GetVisibleColumnWidth(ColumnSide.Left) + SPACING_BETWEEN_COLUMNS * CharacterWidth;
            context.PushTranslate(leftOffset, 0);
        }

        if (Configuration.ColumnsVisible is VisibleColumns.HexText)
        {
            DrawMarkerArea(context, ColumnSide.Right, marker);
        }

        if (_horizontalCharacterOffset < Configuration.BytesPerRow)
        {
            context.Pop();
        }
    }

    private CharacterSet GetCharacterSetForColumn(ColumnSide column)
    {
        if (column is ColumnSide.Right && Configuration.ColumnsVisible is not VisibleColumns.HexText)
        {
            throw new InvalidOperationException(
                "Cannot get character set of right column when right column is not enabled.");
        }

        return column switch
        {
            ColumnSide.Left => _leftCharacterSet,
            ColumnSide.Right => _rightCharacterSet!,
            ColumnSide.Both => throw new ArgumentException(
                "Only a character set for either left or right columns can be determined.", nameof(column)),
            _ => throw new NotSupportedException("This column type is not supported.")
        };
    }

    private int GetLeftRelativeToColumn(int offsetFromLeft, ColumnSide column, bool excludeLastGroup = false)
    {
        switch (column)
        {
            case ColumnSide.Left:
                return GetLeft(offsetFromLeft, column, excludeLastGroup) -
                       Math.Max(0, GetLeft(_horizontalCharacterOffset, column, excludeLastGroup));
            case ColumnSide.Right:
                var leftOffset = _horizontalCharacterOffset > Configuration.BytesPerRow
                    ? GetLeft(_horizontalCharacterOffset - Configuration.BytesPerRow, column, excludeLastGroup)
                    : 0;
                return GetLeft(offsetFromLeft, column, excludeLastGroup) - leftOffset;
            default:
                throw new ArgumentException("This column type is not supported.", nameof(column));
        }
    }

    private void DrawCarets(IRenderContext context, Document document)
    {
        if (document.Caret.Offset < Offset || document.Caret.Offset > Offset + Bytes.Length)
        {
            return;
        }

        DrawCaret(context, document, ColumnSide.Left);
        if (Configuration.ColumnsVisible is VisibleColumns.HexText)
        {
            DrawCaret(context, document, ColumnSide.Right);
        }

        if (_caretUpdated || _previousCaretOffset != document.Caret.Offset)
        {
            AddCaretDirtyRect(document.Caret);
        }


        _caretUpdated = false;
        _previousCaretOffset = document.Caret.Offset;
    }

    private void DrawCaret(IRenderContext context, Document document, ColumnSide column)
    {
        var caret = document.Caret;
        var characterSet = GetCharacterSetForColumn(column);

        if (_previousCaretOffset != caret.Offset)
        {
            var previousPosition = CalculateCaretPosition(_previousCaretOffset, 0, characterSet, column);
            if (previousPosition.X >= 0 && previousPosition.Y <= Height)
            {
                AddDirtyRect(new SharedRectangle(previousPosition.X, previousPosition.Y,
                    characterSet.Width * CharacterWidth, RowHeight), CharacterWidth);
            }
        }

        var drawCaret = _previousCaretOffset != caret.Offset &&
                        (caret.Column == column || document.Selection is null) ||
                        (caret.Column != column || _caretTick) &&
                        (caret.Column == column || document.Selection is null);

        var position = CalculateCaretPosition(caret.Offset, caret.Nibble, characterSet, column);
        if (position.X < 0 || position.Y > Height)
        {
            return;
        }

        if (column == document.Caret.Column)
        {
            var topOffset = 0;
            var leftOffset = characterSet.Groupable && IsEndOfGroup(caret.Offset) &&
                             document.Selection?.End == caret.Offset
                ? _parent.CharacterWidth
                : 1;

            // Move the caret up to the end of last row for visual reasons
            var moveCaretUp = document.Selection?.End == caret.Offset &&
                              caret.Offset % Configuration.BytesPerRow is 0;

            if (moveCaretUp)
            {
                topOffset = _parent.RowHeight;

                var groupAdjustment = characterSet.Groupable ? _parent.CharacterWidth : 1;
                leftOffset -= GetVisibleColumnWidth(column) + groupAdjustment;
            }

            var rect = new SharedRectangle(position.X - leftOffset, position.Y - topOffset, 2,
                _parent.RowHeight);

            if (drawCaret)
            {
                context.DrawRectangle(_parent.CaretBackground, null, rect);
            }
        }
        else
        {
            var pen = new SharedPen(_parent.CaretBackground, 1, PenStyle.Dotted);
            var aliasOffset = GetLineAntiAliasOffset(pen);
            var rect = new SharedRectangle(position.X + aliasOffset, position.Y + aliasOffset,
                _parent.CharacterWidth * characterSet.Width,
                _parent.RowHeight - aliasOffset * 2);

            if (drawCaret)
            {
                context.DrawRectangle(null, pen, rect);
            }
        }
    }


    public void AddCaretDirtyRect(Caret caret)
    {
        var position = CalculateCaretPosition(caret.Offset, 0, _leftCharacterSet, ColumnSide.Left);
        AddDirtyRect(new SharedRectangle(position.X, position.Y, _leftCharacterSet.Width * CharacterWidth, RowHeight),
            CharacterWidth);

        if (_rightCharacterSet is not null)
        {
            position = CalculateCaretPosition(caret.Offset, 0, _leftCharacterSet, ColumnSide.Right);
            AddDirtyRect(
                new SharedRectangle(position.X, position.Y, _leftCharacterSet.Width * CharacterWidth, RowHeight),
                CharacterWidth);
        }
    }

    private SharedPoint CalculateCaretPosition(long offset, long nibble, CharacterSet characterSet, ColumnSide column)
    {
        var relativeOffset = offset - Offset;
        var row = relativeOffset / Configuration.BytesPerRow;

        var x = GetLeftRelativeToColumn((int)(relativeOffset % Configuration.BytesPerRow), column) +
                Math.Min(characterSet.Width - 1, nibble) * _parent.CharacterWidth;
        var y = row * RowHeight + _parent.HeaderHeight;
        if (column is ColumnSide.Right && _horizontalCharacterOffset < Configuration.BytesPerRow)
        {
            x += GetVisibleColumnWidth(ColumnSide.Left) + SPACING_BETWEEN_COLUMNS * CharacterWidth;
        }

        return new SharedPoint(x, y);
    }

    private void DrawMarkerArea(IRenderContext context, ColumnSide column, IDocumentMarker marker)
    {
        if (marker.Background is null && marker.Border is null)
        {
            return;
        }

        var position = CalculateMarkerPosition(marker.Offset, marker.Length, column);

        var brush = marker.Background is { } background ? _colorToBrushCache[background] : null;
        var pen = marker.Border is { } border ? new SharedPen(_colorToBrushCache[border]!, 1) : null;

        if (position.StartRow == position.EndRow || marker.Length <= Configuration.BytesPerRow)
        {
            DrawAreaSimple(context, brush, pen, column, marker, position);
        }
        else
        {
            DrawAreaAdvanced(context, brush, pen, column, position);
        }
    }

    private bool IsEndOfGroup(long offset)
    {
        var column = offset % Configuration.BytesPerRow;
        return column % Configuration.GroupSize == 0;
    }

    private MarkerPosition CalculateMarkerPosition(long markerOffset, long markerLength, ColumnSide column)
    {
        var startOffset = Math.Max(0, markerOffset - Offset);
        var length = Math.Min(Bytes.Length, markerLength - (markerOffset < Offset ? Offset - markerOffset : 0) - 1);
        var endOffset = startOffset + length;
        var startRow = startOffset / Configuration.BytesPerRow;
        var endRow = endOffset / Configuration.BytesPerRow;

        var startX = Math.Max(0, GetLeftRelativeToColumn((int)(startOffset % Configuration.BytesPerRow), column));
        var startY = startOffset / Configuration.BytesPerRow * RowHeight + _parent.HeaderHeight;

        var charset = GetCharacterSetForColumn(column);

        var startColumn = startOffset % Configuration.BytesPerRow;
        var extendPastEdge = charset.Groupable && startColumn % Configuration.GroupSize == 0;
        if (extendPastEdge)
        {
            startX -= _parent.CharacterWidth / 2;
        }

        var endX = Math.Max(0, GetLeftRelativeToColumn((int)(endOffset % Configuration.BytesPerRow) + 1, column, true));
        var endY = endRow * RowHeight + _parent.HeaderHeight;

        extendPastEdge = charset.Groupable && IsEndOfGroup(endOffset + 1);
        if (extendPastEdge)
        {
            endX += _parent.CharacterWidth / 2;
        }

        return new MarkerPosition(startOffset, startRow, new SharedPoint(startX, startY), endOffset, endRow,
            new SharedPoint(endX, endY));
    }

    private void DrawAreaSimple(
        IRenderContext context,
        ISharedBrush? brush,
        ISharedPen? pen,
        ColumnSide column,
        IDocumentMarker marker,
        MarkerPosition position)
    {
        // TODO: fix funky rectangle borders, introduce a 'render flags' option in IRenderContext to determine how borders behave :)!
        var columnWidth = GetVisibleColumnWidth(column);
        var aliasOffset = GetLineAntiAliasOffset(pen);
        var characterSet = GetCharacterSetForColumn(column);

        if (position.StartRow == position.EndRow)
        {
            var rect = new SharedRectangle(
                position.Start.X - aliasOffset,
                position.Start.Y - aliasOffset,
                position.End.X - position.Start.X + aliasOffset * 2,
                RowHeight + aliasOffset * 2);
            context.DrawRectangle(brush, pen, rect);
        }
        else if (marker.Length <= Configuration.BytesPerRow)
        {
            var extendPastEdge = characterSet.Groupable ? _parent.CharacterWidth / 2 : 0;

            var firstRect = new SharedRectangle(
                position.Start.X - aliasOffset,
                position.Start.Y - aliasOffset,
                columnWidth - position.Start.X + extendPastEdge + aliasOffset * 2,
                RowHeight + aliasOffset * 2);
            context.DrawRectangle(brush, pen, firstRect);

            if (position.End.X > 0)
            {
                var secondRect = new SharedRectangle(
                    -extendPastEdge - aliasOffset,
                    position.End.Y - aliasOffset,
                    position.End.X + extendPastEdge + aliasOffset * 2,
                    RowHeight + aliasOffset * 2);
                context.DrawRectangle(brush, pen, secondRect);
            }
        }
    }

    // Utility for WPF, when not offsetting borders it will take up two half pixels (blurry)
    private static double GetLineAntiAliasOffset(ISharedPen? pen) =>
        pen is not null && pen.Thickness % 2 is not 0 ? .5 : 0;

    private void DrawAreaAdvanced(
        IRenderContext context,
        ISharedBrush? brush,
        ISharedPen? pen,
        ColumnSide column,
        MarkerPosition position)
    {
        var characterSet = GetCharacterSetForColumn(column);
        var columnWidth = GetVisibleColumnWidth(column);
        var aliasOffset = GetLineAntiAliasOffset(pen);

        var extendPastEdge = characterSet.Groupable ? _parent.CharacterWidth / 2 : 0;
        var points = ObjectPool<List<SharedPoint>>.Shared.Rent();
        if (position.StartOffset == 0 || position.End.Y == 0)
        {
            points.Add(new SharedPoint(-extendPastEdge + -aliasOffset, position.Start.Y - aliasOffset));
        }
        else
        {
            points.Add(new SharedPoint(position.Start.X - aliasOffset, position.Start.Y + RowHeight - aliasOffset));
            points.Add(new SharedPoint(position.Start.X - aliasOffset, position.Start.Y - aliasOffset));
        }

        points.Add(new SharedPoint(columnWidth + extendPastEdge + aliasOffset, position.Start.Y - aliasOffset));

        if (position.EndOffset == Configuration.BytesPerRow - 1)
        {
            points.Add(new SharedPoint(columnWidth + extendPastEdge - aliasOffset, position.End.Y + aliasOffset));
        }
        else
        {
            points.Add(new SharedPoint(columnWidth + extendPastEdge + aliasOffset, position.End.Y + aliasOffset));
            points.Add(new SharedPoint(position.End.X + aliasOffset, position.End.Y + aliasOffset));
            points.Add(new SharedPoint(position.End.X + aliasOffset, position.End.Y + RowHeight + aliasOffset));
        }

        points.Add(new SharedPoint(-extendPastEdge + -aliasOffset, position.End.Y + RowHeight + aliasOffset));
        points.Add(new SharedPoint(-extendPastEdge + -aliasOffset, position.Start.Y + RowHeight - aliasOffset));

        context.DrawPolygon(brush, pen, points);

        points.Clear();
        ObjectPool<List<SharedPoint>>.Shared.Return(points);
    }

    private int GetFirstVisibleColumn()
    {
        int GetVisibleCharacterCount(int horizontalOffset, CharacterSet characterSet)
        {
            var groups = characterSet.Groupable
                ? horizontalOffset / (Configuration.GroupSize * characterSet.Width + 1)
                : 0;
            return Math.Min((horizontalOffset - groups) / characterSet.Width, Configuration.BytesPerRow);
        }

        var leftCharacterCount = GetColumnCharacterCount(ColumnSide.Left);
        if (_rightCharacterSet is null)
        {
            return GetVisibleCharacterCount(HorizontalOffset, _leftCharacterSet);
        }

        return GetVisibleCharacterCount(HorizontalOffset, _leftCharacterSet) + Math.Max(0,
            GetVisibleCharacterCount(HorizontalOffset - leftCharacterCount, _rightCharacterSet));
    }

    private void WriteHexOffsetHeader(ITextBuilder builder)
    {
        const int padZeroCount = 2;

        var groupSize = Configuration.GroupSize;
        var invisibleGroups = (double)_horizontalCharacterOffset / groupSize;

        var visibleCharacters =
            (int)((1 - (invisibleGroups - (int)invisibleGroups)) * (_leftCharacterSet.Width * groupSize));
        var firstPartiallyVisible = visibleCharacters > 0;
        var incrementX = (_leftCharacterSet.Width * groupSize + 1) * CharacterWidth;
        var totalX = !firstPartiallyVisible ? 0 : (visibleCharacters + 1) * CharacterWidth;

        var startGroup = (int)invisibleGroups;
        var groupCount = Configuration.BytesPerRow / groupSize;
        for (var currentGroup = startGroup; currentGroup < groupCount; currentGroup++)
        {
            var firstGroup = currentGroup == startGroup;

            var length = BaseConverter.Convert(currentGroup, Configuration.OffsetBase, true, _characterBuffer);
            var padZeros = Math.Max(0, padZeroCount - length);

            var x = firstGroup ? 0 : totalX;
            var y = -(_parent.CharacterHeight + 2);

            for (var i = 0; i < Math.Max(length, length + padZeros); i++)
            {
                // Overflow if group size is 1 or first visible area is smaller than 2 characters
                if ((i - 1) % 2 == 1 && (groupSize == 1 || visibleCharacters == 2 && firstGroup) || i == 0)
                {
                    y += _parent.CharacterHeight + 2;
                    builder.Next(new SharedPoint(x, y));
                }

                var charIndex = length - (i - padZeros) - 1;
                var @char = charIndex < 0 || charIndex >= length ? '0' : _characterBuffer[charIndex];

                builder.Add(_parent.HeaderForeground, @char);
            }

            totalX += firstGroup ? 0 : incrementX;
        }
    }

    private void WriteTextHeader(ITextBuilder builder)
    {
        var left = Configuration.ColumnsVisible is VisibleColumns.HexText &&
                   _horizontalCharacterOffset < Configuration.BytesPerRow
            ? GetVisibleColumnWidth(ColumnSide.Left) + SPACING_BETWEEN_COLUMNS * CharacterWidth
            : 0;
        if (left > Width)
        {
            return;
        }

        builder.Next(new SharedPoint(Math.Max(0, left), 0));
        builder.Add(_parent.HeaderForeground, "Decoded text");
    }

    private void WriteContentBytes(ITextBuilder builder)
    {
        var leftWidth = GetColumnCharacterCount(ColumnSide.Left);
        var horizontalSpace = Math.Ceiling(Width / CharacterWidth);
        var verticalSpace = Math.Ceiling(Height / RowHeight) + RowHeight;
        var leftColumnVisibleCharacters = GetVisibleColumnWidth(ColumnSide.Left) / _parent.CharacterWidth;
        var textMiddleOffset = (int)Math.Round(RowHeight / 2d - _parent.CharacterHeight / 2d);

        var typeface = builder.Typeface;
        var bytesPerRow = Configuration.BytesPerRow;
        // Round content length to full row lengths for padding of final row
        var contentLength = (int)(Math.Ceiling(Bytes.Length / (float)bytesPerRow) * bytesPerRow);
        var maxBytesWritten = Configuration.ColumnsVisible is not VisibleColumns.HexText
            ? bytesPerRow
            : bytesPerRow * 2;

        for (var row = 0; row < verticalSpace; row++)
        {
            var y = row * RowHeight + _parent.HeaderHeight + textMiddleOffset;
            var byteColumn = 0;
            var visualCol = 0;
            var bytesWritten = 0;

            var horizontalCharacterOffset = _horizontalCharacterOffset;
            builder.Next(new SharedPoint(0, y));

            while (visualCol < horizontalSpace && ++bytesWritten + _horizontalCharacterOffset <= maxBytesWritten)
            {
                var column = visualCol < leftWidth - HorizontalOffset ? ColumnSide.Left : ColumnSide.Right;
                var characterSet = column is ColumnSide.Left
                    ? _leftCharacterSet
                    : _rightCharacterSet;

                var columnIndex = (byteColumn + horizontalCharacterOffset) % bytesPerRow;
                var byteIndex = row * bytesPerRow + columnIndex;

                if (byteIndex > contentLength - 1)
                {
                    if (column is ColumnSide.Right)
                    {
                        break;
                    }

                    horizontalCharacterOffset = 0;
                    visualCol = leftColumnVisibleCharacters + SPACING_BETWEEN_COLUMNS;
                    byteColumn = 0;
                    continue;
                }

                if (characterSet is not null)
                {
                    visualCol +=
                        WriteSingleContentByte(builder, typeface, characterSet, column, byteIndex, columnIndex);
                }

                byteColumn++;
            }
        }
    }

    private ISharedBrush LookupBrushForByte(CharacterSet characterSet, ColumnSide column, int byteIndex,
        int columnIndex)
    {
        // Uses an array twice the size of the bytes buffer instead of dictionary, since looking up an array item by index is faster.
        var offset = column is ColumnSide.Left ? 0 : _markerForegroundLookup.Length / 2;
        var brush = _markerForegroundLookup[offset + byteIndex];
        if (brush is not null)
        {
            return brush;
        }

        return characterSet.Groupable && columnIndex / Configuration.GroupSize % 2 == 1
            ? _parent.EvenForeground
            : _parent.Foreground;
    }

    private int WriteSingleContentByte(ITextBuilder builder, IGlyphTypeface typeface, CharacterSet characterSet,
        ColumnSide column,
        int byteIndex, int columnIndex)
    {
        int writtenCharacters;
        if (byteIndex > Bytes.Length - 1) // Pad excess bytes with whitespaces
        {
            writtenCharacters = characterSet.Width;
            builder.Whitespace(characterSet.Width);
        }
        else
        {
            var brush = LookupBrushForByte(characterSet, column, byteIndex, columnIndex);
            writtenCharacters = WriteByteToTextBuilder(builder, typeface, brush, characterSet, Bytes[byteIndex]);
        }

        // Add whitespace between characters
        if (column is ColumnSide.Left && columnIndex == Configuration.BytesPerRow - 1)
        {
            builder.Whitespace(SPACING_BETWEEN_COLUMNS);
            writtenCharacters += SPACING_BETWEEN_COLUMNS;
        }
        else if (characterSet.Groupable && columnIndex % Configuration.GroupSize == Configuration.GroupSize - 1)
        {
            builder.Whitespace();
            writtenCharacters++;
        }

        return writtenCharacters;
    }

    private int WriteByteToTextBuilder(
        ITextBuilder builder,
        IGlyphTypeface typeface,
        ISharedBrush brush,
        CharacterSet characterSet,
        byte @byte)
    {
        var characterCount = characterSet.GetCharacters(@byte, _characterBuffer);
        for (var i = 0; i < characterCount; i++)
        {
            var @char = _characterBuffer[i];
            if (char.IsControl(@char) || !char.IsLetterOrDigit(@char) && !typeface.TryGetGlyphIndex(@char, out _))
            {
                @char = '.';
            }

            builder.Add(brush, @char);
        }

        return characterSet.Width;
    }

    private (ColumnSide column, SharedPoint) GetPointRelativeToColumn(SharedPoint point)
    {
        var leftWidth = GetColumnCharacterCount(ColumnSide.Left) * CharacterWidth;
        var leftOffset = GetLeft(_horizontalCharacterOffset, ColumnSide.Left);

        var column = ColumnSide.Left;
        var x = leftOffset + Math.Max(0, point.X);
        if (x > leftWidth && Configuration.ColumnsVisible is VisibleColumns.HexText)
        {
            x = Math.Min(GetColumnCharacterCount(ColumnSide.Right) * CharacterWidth,
                x - (leftWidth + SPACING_BETWEEN_COLUMNS * CharacterWidth));
            column = ColumnSide.Right;
        }

        return (column, new SharedPoint(x, point.Y - _parent.HeaderHeight));
    }

    private (ColumnSide side, long offset, int nibble) GetOffsetFromPoint(SharedPoint point)
    {
        var (column, relativePoint) = GetPointRelativeToColumn(point);
        var characterSet = GetCharacterSetForColumn(column);
        var leftInCharacters = (int)(relativePoint.X / CharacterWidth);

        var groupCount = characterSet.Groupable
            ? leftInCharacters / (Configuration.GroupSize * characterSet.Width + 1)
            : 0;

        var byteColumn = (leftInCharacters - groupCount) / characterSet.Width;
        var nibble = Math.Max(0, ((int)relativePoint.X - GetLeft(byteColumn, column)) / _parent.CharacterWidth);

        var isFinalRow = Offset == Document?.Offset && Document?.Length % Configuration.BytesPerRow is 0;

        // TODO: will break at start and end of document
        var _ = Bytes.Length / Configuration.BytesPerRow -
                (Bytes.Length % Configuration.BytesPerRow == 0 && !isFinalRow ? 1 : 0);

        var byteRow = (int)(relativePoint.Y / RowHeight);
        var clampedRow = byteRow; //Math.Max(0, Math.Min(maxRow, byteRow));
        var offset = Offset + (clampedRow * Configuration.BytesPerRow + byteColumn);

        return (column, ClampOffset(offset), offset >= Document?.Length ? 0 : nibble);
    }

    private void OnMouseDown(object? sender, HostMouseButtonEventArgs e)
    {
        if (e.Button is not HostMouseButton.Left || Document is null)
        {
            return;
        }

        var position = e.PointRelativeTo(this);
        if (!IsPointInEditableArea(position))
        {
            return; // Use clicked outside editable area, don't count this as valid click to prevent weird behavior
        }

        var (column, offset, _) = GetOffsetFromPoint(position);

        // Track the mouse down position for determining initial drag direction and whether it is a click or selection.
        _mouseDownPosition = position;
        _activeColumn = column;
        _startSelectionOffset = offset;

        CaptureMouse();
    }

    private long ClampOffset(long offset) => Math.Max(0, Math.Min(offset, Document?.Length ?? 0));

    private void OnMouseUp(object? sender, HostMouseButtonEventArgs e)
    {
        ReleaseMouse();

        if (Document is null)
        {
            return;
        }

        var (column, offset, nibble) = GetOffsetFromPoint(e.PointRelativeTo(this));
        if (_mouseDownPosition is not null)
        {
            SetCaretOffset(column, offset, nibble);
            Deselect();
        }

        _mouseDownPosition = null;
        _startSelectionOffset = null;
        _mouseSelectMode = false;
    }

    private void SetCaretOffset(long offset, int nibble = 0, bool scrollToCaret = false)
    {
        if (Document is null)
        {
            return;
        }

        SetCaretOffset(Document.Caret.Column, offset, nibble, scrollToCaret);
    }

    private void SetCaretOffset(ColumnSide column, long offset, int nibble = 0, bool scrollToCaret = false)
    {
        if (Document is null)
        {
            return;
        }

        ResetCaretTick();
        Document.ChangeCaret(column, offset, nibble, scrollToCaret);
    }

    private void ResetCaretTick()
    {
        if (_caretTimer is null)
        {
            return;
        }

        _caretTick = true;
        _caretTimer.Stop();
        _caretTimer.Start();
    }

    private bool IsPointInEditableArea(SharedPoint point)
    {
        var leftWidth = GetVisibleColumnWidth(ColumnSide.Left);
        var rightWidth = GetVisibleColumnWidth(ColumnSide.Right);


        var inLeftColumn = point.X < leftWidth;
        var pastHeader = point.Y > _parent.HeaderHeight;
        var rowCount = Math.Ceiling(Bytes.Length / (float)Configuration.BytesPerRow);
        var beforeEnd = point.Y < rowCount * _parent.RowHeight + _parent.HeaderHeight;

        // Only left column is visible, don't check right column
        if (Configuration.ColumnsVisible is not VisibleColumns.HexText)
        {
            return inLeftColumn && pastHeader && beforeEnd;
        }

        var rightColumnX = leftWidth + SPACING_BETWEEN_COLUMNS * _parent.CharacterWidth;
        var inRightColumn = leftWidth >= 0 && point.X >= rightColumnX && point.X < rightColumnX + rightWidth;
        return (inLeftColumn || inRightColumn) && pastHeader && beforeEnd;
    }

    private void OnMouseMove(object? sender, HostMouseEventArgs e)
    {
        var position = e.PointRelativeTo(this);
        Cursor = IsPointInEditableArea(position) ? HostCursor.Text : null;

        if (_startSelectionOffset is null || Document is null || _keyboardSelectMode && !_mouseSelectMode)
        {
            return;
        }

        // Allow for some mouse movement tolerance before not considering it a mouse click
        if (_mouseDownPosition is not null && Math.Abs(position.X - _mouseDownPosition.Value.X) +
            Math.Abs(position.Y - _mouseDownPosition.Value.Y) < 2)
        {
            return;
        }

        var (column, offset, nibble) = GetOffsetFromPoint(position);

        // Allow selecting from middle of character rather than entire character
        var characterSet = GetCharacterSetForColumn(column);
        if (nibble >= characterSet.Width / 2)
        {
            offset += characterSet.Width / 2;
        }

        // Check if user is initially dragging backwards (left or up)
        if (_mouseDownPosition is not null &&
            (position.X < _mouseDownPosition.Value.X || position.Y < _mouseDownPosition.Value.Y))
        {
            _startSelectionOffset += 1;
        }

        _mouseDownPosition = null;
        _mouseSelectMode = true;
        Select(offset, _activeColumn);
    }

    private void OnMouseLeave(object? sender, HandledEventArgs e)
    {
        Cursor = null;
    }

    private void Select(long newOffset, ColumnSide column)
    {
        if (_startSelectionOffset is null || Document is null)
        {
            return;
        }

        var startOffset = newOffset >= _startSelectionOffset ? _startSelectionOffset.Value : newOffset;
        var endOffset = newOffset >= _startSelectionOffset ? newOffset : _startSelectionOffset.Value;

        if (startOffset == endOffset)
        {
            Document.Deselect();
            SetCaretOffset(startOffset, scrollToCaret: true);
        }
        else
        {
            var newCaretLocation = newOffset >= _startSelectionOffset.Value
                ? NewCaretLocation.SelectionEnd
                : NewCaretLocation.SelectionStart;
            Document.Select(startOffset, endOffset, column, newCaretLocation, true);
        }
    }

    private void TextBoxOnKeyDown(object? sender, HostKeyEventArgs e)
    {
        if (Document is null)
        {
            return;
        }

        var caret = Document.Caret;
        var selection = Document.Selection;
        var ctrlPressed = (e.Modifiers & HostKeyModifier.Control) is not 0;
        if (e.Key is HostKey.Left or HostKey.Up or HostKey.Down or HostKey.Right)
        {
            if (!_keyboardSelectMode)
            {
                Deselect();
            }

            HandleArrowKeys(Document, e.Key, !ctrlPressed);
        }
        else if (ctrlPressed && e.Key is HostKey.A)
        {
            _startSelectionOffset = 0;
            Select(Document.Length, _activeColumn);
            _startSelectionOffset = null;
        }
        else if (ctrlPressed && e.Key is HostKey.Z && !IsReadOnly)
        {
            Document.Buffer.Undo();
        }
        else if (ctrlPressed && e.Key is HostKey.Y && !IsReadOnly)
        {
            Document.Buffer.Redo();
        }
        else if (e.Key is HostKey.Shift)
        {
            // Respect user dragging up for continuation with keyboard controls
            _startSelectionOffset =
                (caret.Offset == selection?.Start ? selection.End - 1 : selection?.Start) ??
                caret.Offset;
            _keyboardSelectMode = true;
        }
        else if (selection is not null && e.Key is HostKey.Back or HostKey.Delete && !IsReadOnly)
        {
            Document.Buffer.Delete(selection.Start, selection.End - selection.Start);
            SetCaretOffset(selection.Start);
            Deselect();
        }
        else if (caret.Nibble is 0 && !IsReadOnly)
        {
            switch (e.Key)
            {
                case HostKey.Back:
                    Document.Buffer.Delete(caret.Offset - 1, 1);
                    SetCaretOffset(caret.Offset - 1);
                    break;
                case HostKey.Delete:
                    Document.Buffer.Delete(caret.Offset, 1);
                    break;
            }
        }
    }

    private void TextBoxOnKeyUp(object? sender, HostKeyEventArgs e)
    {
        if (e.Key is not HostKey.Shift)
        {
            return;
        }

        if (!_mouseSelectMode)
        {
            _startSelectionOffset = null;
        }

        _keyboardSelectMode = false;
    }

    private async void TextBoxOnTextChanged(object? sender, HostTextChangedEventArgs e)
    {
        if (e.NewText.Length > 0)
        {
            await HandleWriteKey(e.NewText[^1]);
        }

        if (sender is IHostTextBox textBox)
        {
            textBox.Clear();
        }
    }

    private async Task HandleWriteKey(char @char)
    {
        if (Document is null || IsReadOnly)
        {
            return;
        }

        Deselect();

        var caret = Document.Caret;
        var appendToDocument = caret.Offset >= Document.Length;

        var characterSet = GetCharacterSetForColumn(caret.Column);
        var oldByte = (byte)0;

        if (!appendToDocument)
        {
            var readByte = await ReadCaretByte(caret);
            if (readByte is null)
            {
                return;
            }

            oldByte = readByte.Value;
        }

        // Write to byte and validate if it is possible to write this character
        if (!characterSet.TryWrite(oldByte, @char, caret.Nibble, out var newByte))
        {
            return;
        }

        if (appendToDocument)
        {
            Document.Buffer.Insert(caret.Offset, newByte);
        }
        else
        {
            Document.Buffer.Write(caret.Offset, newByte);
        }

        HandleArrowKeys(Document, HostKey.Right);
    }

    private async Task<byte?> ReadCaretByte(Caret caret)
    {
        if (Document is null)
        {
            return null;
        }

        var relativeOffset = caret.Offset - Offset;
        if (relativeOffset < Bytes.Length)
        {
            return Bytes[relativeOffset];
        }

        // Allow for writing outside of current visible buffer
        var readLength = await Document.Buffer.ReadAsync(caret.Offset, _readBuffer);
        if (readLength <= 0)
        {
            return null;
        }

        return _readBuffer[0];
    }

    private void HandleArrowKeys(Document document, HostKey key, bool jumpByte = false)
    {
        var offset = document.Caret.Offset;
        var nibble = document.Caret.Nibble;

        // Allow for nibble level control when not selecting and byte level when selecting.
        var charset = GetCharacterSetForColumn(document.Caret.Column);
        switch (key)
        {
            case HostKey.Right when nibble == charset.Width - 1 || _keyboardSelectMode || jumpByte:
                offset++;
                nibble = 0;
                break;

            case HostKey.Left when nibble == 1 && jumpByte:
                nibble--;
                break;
            case HostKey.Left when _keyboardSelectMode || jumpByte:
                offset--;
                nibble = 0;
                break;
            case HostKey.Left when nibble == 0:
                offset--;
                nibble = charset.Width - 1;
                break;
            case HostKey.Left or HostKey.Right:
                nibble += key is HostKey.Left ? -1 : 1;
                break;
            case HostKey.Up or HostKey.Down:
                offset += Configuration.BytesPerRow *
                          (key is HostKey.Up ? -1 : 1);
                break;
        }

        offset = Math.Max(0, Math.Min(document.Length, offset));
        nibble = Math.Max(0, nibble);

        if (document.Selection is null)
        {
            SetCaretOffset(offset, nibble, true);
        }

        Select(offset, _activeColumn);
    }

    private void Deselect()
    {
        _startSelectionOffset = null;
        _keyboardSelectMode = false;

        Document?.Deselect();
    }

    private record struct MarkerRange(long Offset, long Length);

    private record struct MarkerPosition(long StartOffset, long StartRow, SharedPoint Start, long EndOffset,
        long EndRow, SharedPoint End);
}