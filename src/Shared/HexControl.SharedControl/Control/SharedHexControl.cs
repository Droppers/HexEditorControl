using System.Drawing;
using HexControl.Buffers;
using HexControl.Buffers.Events;
using HexControl.Buffers.Modifications;
using HexControl.SharedControl.Control.Elements;
using HexControl.SharedControl.Documents.Events;
using HexControl.Framework;
using HexControl.Framework.Drawing;
using HexControl.Framework.Drawing.Text;
using HexControl.Framework.Host.Controls;
using HexControl.Framework.Host.Events;
using HexControl.Framework.Host;
using HexControl.Framework.Observable;
using HexControl.Framework.Visual;
using HexControl.SharedControl.Documents;
using JetBrains.Annotations;

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
[PublicAPI]
internal class SharedHexControl : VisualElement
{
    public const string VerticalScrollBarName = "VerticalScrollBar";
    public const string HorizontalScrollBarName = "HorizontalScrollBar";
    public const string FakeTextBoxName = "FakeTextBox";

    private readonly EditorElement _editorElement;
    private readonly OffsetElement _offsetElement;
    private long _lastRefreshLength;

    private byte[] _displayBuffer = Array.Empty<byte>();
    private byte[] _readBuffer = Array.Empty<byte>();
    private List<ModifiedRange> _displayModifications = new(25);
    private List<ModifiedRange> _readModifications = new(25);

    private IRenderContext? _renderContext;
    private bool _requireTypefaceUpdate = true;
    private bool _scrollToCaret;

    private long _bytesLength;
    
    public SharedHexControl() : base(true)
    {
        // Register events
        SizeChanged += OnSizeChanged;
        MouseWheel += OnMouseWheel;
        MouseUp += OnMouseUp;

        // Create child elements
        _offsetElement = new OffsetElement(this);
        AddChild(_offsetElement);

        _editorElement = new EditorElement(this);
        AddChild(_editorElement);
    }

#region Mapped properties
    private ISharedBrush _background = new ColorBrush(Color.FromArgb(255, 255, 255, 255));
    public ISharedBrush Background
    {
        get => Get(ref _background);
        set => Set(ref _background, value);
    }

    private ISharedBrush _headerForeground = new ColorBrush(Color.FromArgb(0, 0, 190));
    public ISharedBrush HeaderForeground
    {
        get => Get(ref _headerForeground);
        set => Set(ref _headerForeground, value);
    }

    private ISharedBrush _offsetForeground = new ColorBrush(Color.FromArgb(0, 0, 190));
    public ISharedBrush OffsetForeground
    {
        get => Get(ref _offsetForeground);
        set => Set(ref _offsetForeground, value);
    }

    private ISharedBrush _foreground = new ColorBrush(Color.FromArgb(0, 0, 0));
    public ISharedBrush Foreground
    {
        get => Get(ref _foreground);
        set => Set(ref _foreground, value);
    }

    private ISharedBrush _evenForeground = new ColorBrush(Color.FromArgb(180, 0, 0, 0));
    public ISharedBrush EvenForeground
    {
        get => Get(ref _evenForeground);
        set => Set(ref _evenForeground, value);
    }

    private ISharedBrush _modifiedForeground = new ColorBrush(Color.FromArgb(255, 240, 111, 143));
    public ISharedBrush ModifiedForeground
    {
        get => Get(ref _modifiedForeground);
        set => Set(ref _modifiedForeground, value);
    }

    private ISharedBrush _caretBackground = new ColorBrush(Color.FromArgb(0, 0, 0));
    public ISharedBrush CaretBackground
    {
        get => Get(ref _caretBackground);
        set => Set(ref _caretBackground, value);
    }

    private int _scrollWheelSkipRows = 3;
    public int ScrollWheelSkipRows
    {
        get => Get(ref _scrollWheelSkipRows);
        set => Set(ref _scrollWheelSkipRows, value);
    }

    private int _margin = 10;
    public int Margin
    {
        get => Get(ref _margin);
        set => Set(ref _margin, value);
    }

    private string _fontFamily = "Default";
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

    private int _fontSize = 13;
    public int FontSize
    {
        get => Get(ref _fontSize);
        set
        {
            if (_fontSize == value)
            {
                return;
            }
            
            Set(ref _fontSize, value);
        }
    }

    private string _offsetHeader = "Offset";
    public string OffsetHeader
    {
        get => Get(ref _offsetHeader);
        set => Set(ref _offsetHeader, value);
    }

    private string _textHeader = "Decoded text";
    public string TextHeader
    {
        get => Get(ref _textHeader);
        set => Set(ref _textHeader, value);
    }
    #endregion

    public IGlyphTypeface? Typeface { get; private set; }

    private int BytesToRead => Configuration.BytesPerRow * (int)Math.Round(Height / RowHeight);

    private IHostScrollBar? VerticalScrollBar => Host?.GetChild<IHostScrollBar>(VerticalScrollBarName);
    private IHostScrollBar? HorizontalScrollBar => Host?.GetChild<IHostScrollBar>(HorizontalScrollBarName);

    public Document? Document { get; private set; }

    private DocumentConfiguration Configuration => Document?.Configuration ?? DocumentConfiguration.Default;

    internal int HeaderHeight { get; private set; }
    internal int RowHeight { get; private set; } = 16;

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

        if (Math.Abs(e.Delta.Y) > double.Epsilon)
        {
            var direction = e.Delta.Y >= 0 ? -1 : 1;
            var modifier = Math.Clamp(Math.Abs(e.Delta.Y), 0.35, 1.70);
            var increment = (long)(((Configuration.BytesPerRow * _scrollWheelSkipRows) * modifier) * direction);
            Document.Offset += increment;
        }
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
            newMaximum = Math.Max(1, Math.Min(1000, documentLength / Configuration.BytesPerRow));
            visible = HeaderHeight + documentLength / Configuration.BytesPerRow * RowHeight > _editorElement.Height;
        }
        else
        {
            var visibleWidth = (int)(_editorElement.Width / CharacterWidth) - 1;
            newViewport = Math.Max(1, visibleWidth);
            newMaximum = _editorElement.TotalWidth - Math.Min(visibleWidth, _editorElement.TotalWidth);
            visible = _editorElement.TotalWidth * CharacterWidth > _editorElement.Width;
        }

        hostScrollBar.Minimum = 0;
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


    public async ValueTask SetDocumentAsync(Document? newDocument)
    {
        if (Document is not null)
        {
            Document.OffsetChanged -= DocumentOnOffsetChanged;
            Document.SelectionChanged -= DocumentOnSelectionChanged;
            Document.CaretChanged -= DocumentOnCaretChanged;
            Document.ConfigurationChanged -= DocumentOnConfigurationChanged;
            
            Document.Modified -= DocumentOnModified;
            Document.Saved += DocumentOnSaved;
            Document.LengthChanged -= DocumentOnLengthChanged;
        }

        if (newDocument is not null)
        {
            newDocument.OffsetChanged += DocumentOnOffsetChanged;
            newDocument.SelectionChanged += DocumentOnSelectionChanged;
            newDocument.CaretChanged += DocumentOnCaretChanged;
            newDocument.ConfigurationChanged += DocumentOnConfigurationChanged;

            newDocument.Modified += DocumentOnModified;
            newDocument.Saved += DocumentOnSaved;
            newDocument.LengthChanged += DocumentOnLengthChanged;

            Document = newDocument;
            _offsetElement.Configuration = newDocument.Configuration;
            _offsetElement.Document = newDocument;
            _editorElement.Configuration = newDocument.Configuration;
            _editorElement.Document = newDocument;

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
        
        _editorElement.AddDirtyRect(new SharedRectangle(0, 0, Width, _offsetElement.Width + _editorElement.Width + Margin));
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

        if (Document.Caret.Offset <= Document.Offset)
        {
            Document.Offset = Document.Caret.Offset;
        }
        else if (Document.Caret.Offset > _editorElement.MaxVisibleOffset
            && (_bytesLength / Configuration.BytesPerRow) >= _editorElement.VisibleRows
                || (Document.Caret.Offset - Document.Offset) / Configuration.BytesPerRow >= _editorElement.VisibleRows)
        {
            Document.Offset = Document.Caret.Offset - (_editorElement.MaxVisibleOffset - Document.Offset) +
                              Configuration.BytesPerRow;
        }
    }

    private async void DocumentOnModified(object? sender, ModifiedEventArgs e)
    {
        if (Document is null)
        {
            return;
        }
        
        ScrollToCaret();

        await RefreshDocument();
    }

    private async void DocumentOnSaved(object? sender, EventArgs e)
    {
        await RefreshDocument();
    }

    private void DocumentOnLengthChanged(object? sender, LengthChangedEventArgs e)
    {
        _offsetElement.Length = Document?.Length ?? 0;

        UpdateChildDimensions();
        UpdateScrollBars();
    }

    private void ApplyConfiguration()
    {
        _offsetElement.Visible = Configuration.OffsetsVisible;
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

        _offsetElement.TextBuilder = context.PreferTextLayout
            ? new TextLayoutBuilder(Typeface, FontSize)
            : new GlyphRunBuilder(Typeface, FontSize, CharacterWidth);
        _editorElement.TextBuilder = context.PreferTextLayout
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

    private async void DocumentOnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        foreach (var property in e.Changes)
        {
            switch (property)
            {
                case nameof(DocumentConfiguration.OffsetsVisible):
                    _offsetElement.Visible = Configuration.OffsetsVisible;
                    break;
                case nameof(DocumentConfiguration.BytesPerRow):
                    await RefreshDocument();
                    break;
            }
        }

        _editorElement.Configuration = e.NewConfiguration;

        UpdateScrollBars();

        AddDirtyRect(new SharedRectangle(0, 0, Width, Height));
        Invalidate();
    }

    private async ValueTask InitDocument()
    {
        _editorElement.HorizontalCharacterOffset = Document?.HorizontalCharacterOffset ?? 0;
        _offsetElement.Length = Document?.Length ?? 0;

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
        if (e.RequestCenter)
        {
            RequestScrollToCaret();
        }

        Invalidate();
    }

    private async ValueTask RefreshDocument()
    {
        if (Document is null || BytesToRead <= 0)
        {
            _editorElement.Bytes = Array.Empty<byte>();
            return;
        }

        var canDoIO = await queue.StartIOTaskAsync();
        if (!canDoIO)
        {
            return;
        }

        // Minimum required buffer size has increased
        if (BytesToRead > _readBuffer.Length || BytesToRead > _displayBuffer.Length)
        {
            _readBuffer = new byte[BytesToRead * 2];
            _displayBuffer = new byte[BytesToRead * 2];
        }

        _readModifications.Clear();
        _bytesLength = await Document.Buffer.ReadAsync(_readBuffer.AsMemory(0, BytesToRead), Document.Offset, _readModifications);

        // Swap read and display buffers
        (_readBuffer, _displayBuffer) = (_displayBuffer, _readBuffer);
        (_readModifications, _displayModifications) = (_displayModifications, _readModifications);

        _offsetElement.Offset = Document.Offset;
        _editorElement.Offset = Document.Offset;
        _editorElement.BytesLength = _bytesLength;
        _editorElement.Bytes = _displayBuffer;
        _editorElement.Modifications = _displayModifications;

        queue.StopIOTask();

        _lastRefreshLength = Document.Length;
        if (_scrollToCaret)
        {
            ScrollToCaret();
        }

        var editorWidth = _editorElement.TotalWidth * CharacterWidth;
        var width = _editorElement.Left + editorWidth - _offsetElement.Left;
        AddDirtyRect(
            new SharedRectangle(_offsetElement.Left, _editorElement.Top + HeaderHeight, width, _editorElement.Height),
            CharacterWidth);
        Invalidate();
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

        var readOffset = (long)(scrollValue / VerticalScrollBar.Maximum * Document.Length);
        Document.Offset = readOffset;
    }

    public void SetHorizontalScrollValue(double scrollValue)
    {
        if (Document is null)
        {
            return;
        }

        _editorElement.HorizontalCharacterOffset = (int)scrollValue;
        Document.HorizontalCharacterOffset = (int)scrollValue;

        AddDirtyRect(
            new SharedRectangle(_editorElement.Left, _editorElement.Top, _editorElement.Width, _editorElement.Height),
            CharacterWidth);
        Invalidate();
    }

    protected override void Render(IRenderContext context)
    {
        Document?.CaptureState();

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
        _offsetElement.Top = Margin;
        _editorElement.Top = Margin;
        _offsetElement.Left = Margin;

        var editorWidth = _editorElement.TotalWidth * CharacterWidth;

        if (_offsetElement.Visible)
        {
            _editorElement.Width = Width - _offsetElement.Width - Margin;
            _editorElement.Left = Math.Min(editorWidth, (int)_offsetElement.Width + _offsetElement.Left);
        }
        else
        {
            _editorElement.Left = Margin;
            _editorElement.Width = Math.Min(editorWidth, Width - Margin);
        }

        _offsetElement.Height = Height - Margin;
        _editorElement.Height = Height - Margin;
    }
}