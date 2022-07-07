using System.Drawing;
using HexControl.Core;
using HexControl.Core.Buffers.Modifications;
using HexControl.Core.Events;
using HexControl.Core.Observable;
using HexControl.SharedControl.Control.Elements;
using HexControl.SharedControl.Framework;
using HexControl.SharedControl.Framework.Drawing;
using HexControl.SharedControl.Framework.Drawing.Text;
using HexControl.SharedControl.Framework.Host.Controls;
using HexControl.SharedControl.Framework.Host.EventArgs;
using HexControl.SharedControl.Framework.Host.Typeface;
using HexControl.SharedControl.Framework.Visual;

// ReSharper disable MemberCanBePrivate.Global

namespace HexControl.SharedControl.Control;

internal enum SharedScrollBar
{
    Vertical,
    Horizontal
}

internal class ScrollBarVisibilityChangedEventArgs : EventArgs
{
    public ScrollBarVisibilityChangedEventArgs(SharedScrollBar scrollBar, bool visible)
    {
        ScrollBar = scrollBar;
        Visible = visible;
    }

    public SharedScrollBar ScrollBar { get; }
    public bool Visible { get; }
}

// TODO: horizontal character offset in the editor column will be messed up if someone decides to change character sets
// TODO:  -> recalculate this offset when this happens
// TODO:  -> also store this offset in the document, will be necessary when switching between documents and remaining state.
internal class SharedHexControl : VisualElement
{
    public const string VerticalScrollBarName = "VerticalScrollBar";
    public const string HorizontalScrollBarName = "HorizontalScrollBar";
    public const string FakeTextBoxName = "FakeTextBox";
    private readonly EditorColumn _editorColumn;
    private readonly OffsetColumn _offsetColumn;

    private ISharedBrush _background = new ColorBrush(Color.FromArgb(255, 24, 27, 32));
    private ISharedBrush _caretBackground = new ColorBrush(Color.FromArgb(255, 255, 255));
    private ISharedBrush _evenForeground = new ColorBrush(Color.FromArgb(180, 255, 255, 255));

    private string _fontFamily = "Default";
    private int _fontSize = 13;
    private ISharedBrush _foreground = new ColorBrush(Color.FromArgb(255, 255, 255));
    private ISharedBrush _headerForeground = new ColorBrush(Color.FromArgb(0, 174, 255));

    private long _lastRefreshLength;

    private int _margin = 10;
    private ISharedBrush _modifiedForeground = new ColorBrush(Color.FromArgb(255, 240, 111, 143));
    private ISharedBrush _offsetForeground = new ColorBrush(Color.FromArgb(0, 174, 255));

    //private ISharedBrush _background = new ColorBrush(Color.FromArgb(255, 255, 255, 255));
    //private ISharedBrush _headerForeground = new ColorBrush(Color.FromArgb(0, 0, 190));
    //private ISharedBrush _offsetForeground = new ColorBrush(Color.FromArgb(0, 0, 190));
    //private ISharedBrush _foreground = new ColorBrush(Color.FromArgb(0, 0, 0));
    //private ISharedBrush _evenForeground = new ColorBrush(Color.FromArgb(180, 0, 0, 0));
    //private ISharedBrush _caretBackground = new ColorBrush(Color.FromArgb(0, 0, 0));
    //private ISharedBrush _modifiedForeground = new ColorBrush(Color.FromArgb(255, 240, 111, 143));

    private byte[] _readBuffer = Array.Empty<byte>();
    private IRenderContext? _renderContext;

    private bool _requireTypefaceUpdate = true;
    private bool _scrollToCaret;

    private int _scrollWheelSkipRows = 3;

    public SharedHexControl() : base(true)
    {
        // Register events
        SizeChanged += OnSizeChanged;
        MouseWheel += OnMouseWheel;
        MouseUp += OnMouseUp;

        // Create child elements
        _offsetColumn = new OffsetColumn(this);
        AddChild(_offsetColumn);

        _editorColumn = new EditorColumn(this);
        AddChild(_editorColumn);
    }

    public ISharedBrush Background
    {
        get => Get(ref _background);
        set => Set(ref _background, value);
    }

    public ISharedBrush HeaderForeground
    {
        get => Get(ref _headerForeground);
        set => Set(ref _headerForeground, value);
    }

    public ISharedBrush OffsetForeground
    {
        get => Get(ref _offsetForeground);
        set => Set(ref _offsetForeground, value);
    }

    public ISharedBrush Foreground
    {
        get => Get(ref _foreground);
        set => Set(ref _foreground, value);
    }

    public ISharedBrush EvenForeground
    {
        get => Get(ref _evenForeground);
        set => Set(ref _evenForeground, value);
    }

    public ISharedBrush ModifiedForeground
    {
        get => Get(ref _modifiedForeground);
        set => Set(ref _modifiedForeground, value);
    }

    public ISharedBrush CaretBackground
    {
        get => Get(ref _caretBackground);
        set => Set(ref _caretBackground, value);
    }

    public int ScrollWheelSkipRows
    {
        get => Get(ref _scrollWheelSkipRows);
        set => Set(ref _scrollWheelSkipRows, value);
    }

    public int Margin
    {
        get => Get(ref _margin);
        set => Set(ref _margin, value);
    }

    public string FontFamily
    {
        get => Get(ref _fontFamily);
        set
        {
            if (_fontFamily == value)
            {
                return;
            }

            Set(ref _fontFamily, value);
            _requireTypefaceUpdate = true;
        }
    }

    public int FontSize
    {
        get => Get(ref _fontSize);
        set
        {
            if (_fontSize == value)
            {
                return;
            }

            _fontSize = value;
            Set(ref _fontSize, value);
        }
    }

    public IGlyphTypeface? Typeface { get; private set; }

    private int BytesToRead => Configuration.BytesPerRow * (int)Math.Round(Height / RowHeight);

    private IHostScrollBar? VerticalScrollBar => Host?.GetChild<IHostScrollBar>(VerticalScrollBarName);
    private IHostScrollBar? HorizontalScrollBar => Host?.GetChild<IHostScrollBar>(HorizontalScrollBarName);

    public Document? Document { get; private set; }

    private DocumentConfiguration Configuration => Document?.Configuration ?? DocumentConfiguration.Default;

    public int HeaderHeight { get; internal set; }
    public int RowHeight { get; internal set; } = 16;

    internal int CharacterWidth { get; private set; } = 8;
    internal int CharacterHeight { get; private set; } = 8;

    public event EventHandler<ScrollBarVisibilityChangedEventArgs>? ScrollBarVisibilityChanged;

    protected override void OnHostAttached(IHostControl attachHost)
    {
        _requireTypefaceUpdate = true;
        UpdateFontSize();
        UpdateDimensions();

        InitScrollBars();
    }

    protected void OnMouseWheel(object? sender, HostMouseWheelEventArgs e)
    {
        if (Document is null)
        {
            return;
        }

        var direction = e.Delta >= 0 ? -1 : 1;
        var increment = Configuration.BytesPerRow * _scrollWheelSkipRows * direction;
        Document.Offset += increment;
    }

    protected async void OnMouseUp(object? sender, HostMouseButtonEventArgs e)
    {
        // Otherwise WinUI doesn't let me steal its focus after pointer click :(
        await Task.Delay(10);

        var textBox = Host?.GetChild<IHostTextBox>(FakeTextBoxName);
        textBox?.Focus();
    }

    private void InitScrollBars()
    {
        if (VerticalScrollBar is not null)
        {
            VerticalScrollBar.Scroll += VerticalScrollBar_OnScroll;
        }

        if (HorizontalScrollBar is not null)
        {
            HorizontalScrollBar.Scroll += HorizontalScrollBar_OnScroll;
        }

        UpdateScrollBars();
    }

    private void VerticalScrollBar_OnScroll(object? sender, HostScrollEventArgs e)
    {
        switch (e.ScrollType)
        {
            case HostScrollEventType.SmallIncrement:
                VerticalSmallScroll(true);
                break;
            case HostScrollEventType.SmallDecrement:
                VerticalSmallScroll(false);
                break;
            default:
                SetVerticalScrollValue(e.NewValue);
                break;
        }
    }

    private void HorizontalScrollBar_OnScroll(object? sender, HostScrollEventArgs e)
    {
        SetHorizontalScrollValue(e.NewValue);
    }

    private async void OnSizeChanged(object? sender, HostSizeChangedEventArgs e)
    {
        UpdateDimensions();
        UpdateScrollBars();

        AddDirtyRect(new SharedRectangle(0, 0, Width, Height));
        await RefreshDocument();
    }

    private void UpdateDimensions()
    {
        Width = Math.Max(0, Host?.Width ?? 0);
        Height = Math.Max(0, Host?.Height ?? 0);

        UpdateChildDimensions();
    }

    public void UpdateScrollBars()
    {
        UpdateScrollBar(SharedScrollBar.Vertical);
        UpdateScrollBar(SharedScrollBar.Horizontal);
    }

    public void UpdateScrollBar(SharedScrollBar scrollBar)
    {
        var hostScrollBar = GetScrollBar(scrollBar);
        if (hostScrollBar is null)
        {
            return;
        }

        double newViewport;
        double newMaximum;
        bool visible;

        if (scrollBar is SharedScrollBar.Vertical)
        {
            var documentLength = Document?.Length ?? 1;

            newViewport = Height / RowHeight;
            newMaximum = Math.Min(1000, documentLength / Configuration.BytesPerRow);
            visible = HeaderHeight + documentLength / Configuration.BytesPerRow * RowHeight > _editorColumn.Height;
        }
        else
        {
            var visibleWidth = (int)(_editorColumn.Width / CharacterWidth) - 1;
            newViewport = Math.Max(1, visibleWidth);
            newMaximum = _editorColumn.TotalWidth - visibleWidth;
            visible = _editorColumn.TotalWidth * CharacterWidth > _editorColumn.Width;
        }

        hostScrollBar.Maximum = newMaximum;
        hostScrollBar.Viewport = newViewport;
        ScrollBarVisibilityChanged?.Invoke(this, new ScrollBarVisibilityChangedEventArgs(scrollBar, visible));
    }

    private IHostScrollBar? GetScrollBar(SharedScrollBar scrollBar)
    {
        return scrollBar switch
        {
            SharedScrollBar.Vertical => VerticalScrollBar,
            SharedScrollBar.Horizontal => HorizontalScrollBar,
            _ => null
        };
    }


    public async Task SetDocumentAsync(Document? newDocument)
    {
        if (Document is not null)
        {
            Document.OffsetChanged -= DocumentOnOffsetChanged;
            Document.SelectionChanged -= DocumentOnSelectionChanged;
            Document.CaretChanged -= DocumentOnCaretChanged;
            Document.ConfigurationChanged -= DocumentOnConfigurationChanged;

            Document.Buffer.Modified -= BufferOnModified;
            Document.Saved += DocumentOnSaved;
            Document.LengthChanged -= DocumentOnLengthChanged;
        }

        if (newDocument is not null)
        {
            newDocument.OffsetChanged += DocumentOnOffsetChanged;
            newDocument.SelectionChanged += DocumentOnSelectionChanged;
            newDocument.CaretChanged += DocumentOnCaretChanged;
            newDocument.ConfigurationChanged += DocumentOnConfigurationChanged;

            newDocument.Buffer.Modified += BufferOnModified;
            newDocument.Saved += DocumentOnSaved;
            newDocument.LengthChanged += DocumentOnLengthChanged;

            Document = newDocument;
            _offsetColumn.Configuration = newDocument.Configuration;
            _editorColumn.Configuration = newDocument.Configuration;
            _editorColumn.Document = newDocument;

            ApplyConfiguration();
            await InitDocument();
        }
        else
        {
            Document = null;
            Invalidate();
        }
    }

    private void DocumentOnCaretChanged(object? sender, CaretChangedEventArgs e)
    {
        if (Document is null)
        {
            return;
        }

        if (e.ScrollToCaret)
        {
            RequestScrollToCaret();
        }

        _editorColumn.AddCaretDirtyRect(e.NewCaret);
        Invalidate();
    }

    private void RequestScrollToCaret()
    {
        // We have not received this information about increased length yet, 'enqueue' scroll to caret instead
        if (Document?.Caret.Offset > _lastRefreshLength)
        {
            _scrollToCaret = true;
        }
        else
        {
            ScrollToCaret();
        }
    }

    private void ScrollToCaret()
    {
        if (Document is null)
        {
            return;
        }

        _scrollToCaret = false;

        if (Document.Caret.Offset < Document.Offset)
        {
            Document.Offset = Document.Caret.Offset;
        }
        else if (Document.Caret.Offset > _editorColumn.MaxVisibleOffset)
        {
            Document.Offset = Document.Caret.Offset - (_editorColumn.MaxVisibleOffset - Document.Offset) +
                              Configuration.BytesPerRow;
        }
    }

    private async void BufferOnModified(object? sender, ModifiedEventArgs e)
    {
        if (Document is null)
        {
            return;
        }

        if (e.Modification.Offset + e.Modification.Length < Document.Offset ||
            e.Modification.Offset > Document.Offset + _editorColumn.MaxVisibleOffset)
        {
            return;
        }

        await RefreshDocument();
    }

    private async void DocumentOnSaved(object? sender, EventArgs e)
    {
        await RefreshDocument();
    }

    private void DocumentOnLengthChanged(object? sender, LengthChangedEventArgs e)
    {
        _offsetColumn.Length = Document?.Length ?? 0;

        UpdateChildDimensions();
        UpdateScrollBars();
    }

    private void ApplyConfiguration()
    {
        _offsetColumn.Visible = Configuration.OffsetsVisible;
    }

    private void UpdateTypeface()
    {
        if (_renderContext is null)
        {
            return;
        }

        Typeface?.Dispose();

        _requireTypefaceUpdate = false;

        var factory = _renderContext.Factory;

        if (FontFamily.Equals("Default", StringComparison.CurrentCultureIgnoreCase))
        {
            // On Windows we default to Courier New, on other platforms we use an embedded version of DejaVu Sans Mono

            Typeface = OperatingSystem.IsWindows()
                ? factory.CreateGlyphTypeface("Courier New")
                : factory.CreateGlyphTypeface(new EmbeddedAsset("HexControl.SharedControl",
                    "Control.Fonts.DejaVuSansMono.ttf", "DejaVu Sans Mono"));
        }
        else
        {
            Typeface = factory.CreateGlyphTypeface(FontFamily);
        }

        UpdateFontSize();

        if (_renderContext is not null)
        {
            UpdateTextBuilder(_renderContext);
        }
    }

    private void UpdateFontSize()
    {
        if (Typeface is null)
        {
            return;
        }

        var characterWidth = Typeface.GetWidth(_fontSize);
        var characterHeight = Typeface.GetCapHeight(_fontSize);

        CharacterHeight = (int)Math.Ceiling(characterHeight);
        RowHeight = CharacterHeight + 8;
        CharacterWidth = (int)Math.Round(characterWidth);
        HeaderHeight = (int)Math.Round(CharacterHeight * 2.5);

        if (_renderContext is not null)
        {
            UpdateTextBuilder(_renderContext);
        }
    }

    private void UpdateTextBuilder(IRenderContext context)
    {
        if (Typeface is null)
        {
            return;
        }

        _offsetColumn.TextBuilder = context.PreferTextLayout
            ? new TextLayoutBuilder(Typeface, FontSize)
            : new GlyphRunBuilder(Typeface, FontSize, CharacterWidth);
        _editorColumn.TextBuilder = context.PreferTextLayout
            ? new TextLayoutBuilder(Typeface, FontSize)
            : new GlyphRunBuilder(Typeface, FontSize, CharacterWidth);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        switch (e.Property)
        {
            case nameof(FontFamily):
                UpdateFontSize();
                break;
        }
    }

    private async void DocumentOnConfigurationChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.Property)
        {
            case nameof(DocumentConfiguration.OffsetsVisible):
                _offsetColumn.Visible = Configuration.OffsetsVisible;
                break;
            case nameof(DocumentConfiguration.BytesPerRow):
                await RefreshDocument();
                break;
        }

        UpdateScrollBars();

        if (e.Property is not nameof(DocumentConfiguration.BytesPerRow))
        {
            Invalidate();
        }
    }

    private async Task InitDocument()
    {
        _editorColumn.HorizontalOffset = Document?.HorizontalOffset ?? 0;
        _offsetColumn.Length = Document?.Length ?? 0;

        await RefreshDocument();

        UpdateChildDimensions();
        UpdateScrollBars();
    }

    private async void DocumentOnOffsetChanged(object? sender, OffsetChangedEventArgs e)
    {
        await RefreshDocument();
    }

    private void DocumentOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // TODO: how does request center differ from scrolling to caret?
        //if (!e.RequestCenter)
        //{
        //    // TODO: should be implemented in OnCaretChanged instead.
        //}
        //else
        //{
        RequestScrollToCaret();
        //}

        Invalidate();
    }

    private async Task RefreshDocument()
    {
        if (Document is null || BytesToRead <= 0)
        {
            _editorColumn.Bytes = Array.Empty<byte>();
            return;
        }

        var canDoIO = await queue.StartIOTaskAsync();
        if (!canDoIO)
        {
            return;
        }

        // Required buffer size has increased
        if (BytesToRead > _readBuffer.Length)
        {
            _readBuffer = new byte[BytesToRead];
        }

        _editorColumn.Modifications.Clear();
        var actualRead =
            await Document.Buffer.ReadAsync(Document.Offset, _readBuffer, _editorColumn.Modifications);
        var displayBuffer = actualRead < BytesToRead
            ? CopyIntoBufferWithLength(_readBuffer, (int)actualRead)
            : _readBuffer;

        _offsetColumn.Offset = Document.Offset;
        _editorColumn.Bytes = displayBuffer;
        _editorColumn.Offset = Document.Offset;

        queue.StopIOTask();

        _lastRefreshLength = Document.Length;
        if (_scrollToCaret)
        {
            ScrollToCaret();
        }

        var editorWidth = _editorColumn.TotalWidth * CharacterWidth;
        var width = _editorColumn.Left + editorWidth - _offsetColumn.Left;
        AddDirtyRect(
            new SharedRectangle(_offsetColumn.Left, _editorColumn.Top + HeaderHeight, width, _editorColumn.Height),
            CharacterWidth);
        Invalidate();
    }

    private static byte[] CopyIntoBufferWithLength(byte[] sourceBuffer, int targetLength)
    {
        if (targetLength == 0)
        {
            return Array.Empty<byte>();
        }

        var targetBuffer = new byte[targetLength];
        Buffer.BlockCopy(sourceBuffer, 0, targetBuffer, 0, targetBuffer.Length);
        return targetBuffer;
    }

    // Scrolling
    public void VerticalSmallScroll(bool increment)
    {
        if (Document is null)
        {
            return;
        }

        Document.Offset += increment ? Configuration.BytesPerRow : -Configuration.BytesPerRow;
    }

    public void SetVerticalScrollValue(double scrollValue)
    {
        if (Document is null || VerticalScrollBar is null)
        {
            return;
        }

        var readOffset = (long)(scrollValue / VerticalScrollBar.Maximum * Document.Length) /
                         Configuration.BytesPerRow *
                         Configuration.BytesPerRow;
        Document.Offset = readOffset;
    }

    public void SetHorizontalScrollValue(double scrollValue)
    {
        if (Document is null)
        {
            return;
        }

        _editorColumn.HorizontalOffset = (int)scrollValue;
        Document.HorizontalOffset = (int)scrollValue;

        AddDirtyRect(
            new SharedRectangle(_editorColumn.Left, _editorColumn.Top, _editorColumn.Width, _editorColumn.Height),
            CharacterWidth);
        Invalidate();
    }

    protected override void Render(IRenderContext context)
    {
        if (!ReferenceEquals(context, _renderContext))
        {
            UpdateTextBuilder(context);
            _renderContext = context;
        }

        if (_requireTypefaceUpdate)
        {
            UpdateTypeface();
        }

        if (context.RequiresClear)
        {
            context.Clear(Background);
        }

        // Display a blank screen when the document is null
        if (Document is null)
        {
            return;
        }

        base.Render(context);
    }

    private void UpdateChildDimensions()
    {
        _offsetColumn.Top = Margin;
        _editorColumn.Top = Margin;
        _offsetColumn.Left = Margin;

        if (_offsetColumn.Visible)
        {
            _editorColumn.Width = Width - _offsetColumn.Width - Margin;
            _editorColumn.Left = (int)_offsetColumn.Width + _offsetColumn.Left;
        }
        else
        {
            _editorColumn.Left = Margin;
            _editorColumn.Width = Width - Margin;
        }

        _offsetColumn.Height = Height - Margin;
        _editorColumn.Height = Height - Margin;
    }
}