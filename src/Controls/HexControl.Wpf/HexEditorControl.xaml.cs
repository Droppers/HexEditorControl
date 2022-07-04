using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HexControl.Core;
using HexControl.SharedControl.Control;
#if !SKIA_RENDER
using HexControl.Wpf.D2D;
#endif
using HexControl.Wpf.Host;
using HexControl.Wpf.Host.Controls;

namespace HexControl.Wpf;

public partial class HexEditorControl : UserControl
{
    public static readonly DependencyProperty ResizeModeProperty = DependencyProperty.Register(nameof(ResizeMode),
        typeof(ResizeMode), typeof(HexEditorControl), new PropertyMetadata(ResizeMode.Debounce, OnPropertyChanged));

    public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register(nameof(Document),
        typeof(Document), typeof(HexEditorControl), new PropertyMetadata(null, OnPropertyChanged));

    public static readonly DependencyProperty EvenForegroundProperty = DependencyProperty.Register(
        nameof(EvenForeground),
        typeof(Brush), typeof(HexEditorControl), new PropertyMetadata(null, OnPropertyChanged));

    public static readonly DependencyProperty RowHeightProperty = DependencyProperty.Register(nameof(RowHeight),
        typeof(int), typeof(HexEditorControl), new PropertyMetadata(15, OnPropertyChanged));

    public static readonly DependencyProperty RenderApiProperty = DependencyProperty.Register(nameof(RenderApi),
        typeof(HexRenderApi), typeof(HexEditorControl), new PropertyMetadata(null, OnPropertyChanged));

#if SKIA_RENDER
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly WpfSkiaHost _host;
#else
    private readonly WpfD2DHost _host;
#endif

    public HexEditorControl()
    {
        InitializeComponent();

#if SKIA_RENDER
        _host = new WpfSkiaHost(HostContainer)
        {
            { "VerticalScrollBar", new WpfScrollBar(VerticalScrollBar)},
            { "HorizontalScrollBar", new WpfScrollBar(HorizontalScrollBar)},
            { "FakeTextBox", new WpfTextBox(FakeTextBox)}
        };
#else
        _host = new WpfD2DHost(HostContainer, new D2DControl(HostContainer))
        {
            {"VerticalScrollBar", new WpfScrollBar(VerticalScrollBar)},
            {"HorizontalScrollBar", new WpfScrollBar(HorizontalScrollBar)},
            {"FakeTextBox", new WpfTextBox(FakeTextBox)}
        };
#endif

        Control = new SharedHexControl();
        Control.ScrollBarVisibilityChanged += OnScrollBarVisibilityChanged;
        Control.AttachHost(_host);

        var factory = new WpfNativeFactory();
        Mapper = new HexControlPropertyMapper(Control, factory);
    }

    private HexControlPropertyMapper Mapper { get; }

    private SharedHexControl Control { get; }

    public ResizeMode ResizeMode
    {
        get => (ResizeMode)GetValue(ResizeModeProperty);
        set => SetValue(ResizeModeProperty, value);
    }

    public Document? Document
    {
        get => (Document)GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    public Brush? EvenForeground
    {
        get => Mapper.GetValue<Brush>(GetValue(EvenForegroundProperty));
        set => SetValue(EvenForegroundProperty, value);
    }

    public int RowHeight
    {
        get => Mapper.GetValue<int>(GetValue(RowHeightProperty));
        set => SetValue(RowHeightProperty, value);
    }

    public HexRenderApi? RenderApi
    {
        get => Mapper.GetValueNullable<HexRenderApi>(GetValue(RenderApiProperty));
        set => SetValue(RenderApiProperty, value);
    }

    private void OnScrollBarVisibilityChanged(object? sender, ScrollBarVisibilityChangedEventArgs e)
    {
        if (e.ScrollBar is SharedScrollBar.Horizontal)
        {
            GridRow.Height = e.Visible ? GridLength.Auto : new GridLength(0);
        }
        else
        {
            GridColumn.Width = e.Visible ? GridLength.Auto : new GridLength(0);
        }
    }

    private static async void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not HexEditorControl editor)
        {
            return;
        }

        var value = e.NewValue;

#if !SKIA_RENDER
        if (e.Property.Name is nameof(ResizeMode))
        {
            editor._host.ResizeMode = (ResizeMode)value;
            return;
        }
#endif

        await editor.Mapper.SetValueAsync(e.Property.Name, value);
    }
}