using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using HexControl.Core;
using HexControl.SharedControl.Control;
using HexControl.Wpf.Host;
using HexControl.Wpf.Host.Controls;
using Microsoft.Win32;

namespace HexControl.Wpf;

public partial class HexEditorControl : UserControl
{
    public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register(nameof(Document),
        typeof(Document), typeof(HexEditorControl), new PropertyMetadata(null, OnPropertyChanged));

    public static readonly DependencyProperty EvenForegroundProperty = DependencyProperty.Register(
        nameof(EvenForeground),
        typeof(Brush), typeof(HexEditorControl), new PropertyMetadata(null, OnPropertyChanged));

    public static readonly DependencyProperty RowHeightProperty = DependencyProperty.Register(nameof(RowHeight),
        typeof(int), typeof(HexEditorControl), new PropertyMetadata(15, OnPropertyChanged));


    public static readonly DependencyProperty RenderApiProperty = DependencyProperty.Register(nameof(RenderApi),
        typeof(HexRenderApi), typeof(HexEditorControl), new PropertyMetadata(null, OnPropertyChanged));

#if D2D_RENDER
    private WpfD2DInteropHost _host;
    //private readonly Image _image;
    //private readonly D3D11Image _d3dImage;
#elif SKIA_RENDER
    private readonly WpfSkiaHost _host;
#else
    private readonly WpfHost _host;
#endif

    public HexEditorControl()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;

//#if D2D_RENDER
        //_image = new Image();
        //_d3dImage = new D3D11Image();
        //_image.Source = _d3dImage;
        //Container.Children.Insert(0, _image);

        //var d2dControl = new D2DControl();

        //Container.Children.Insert(0, d2dControl);

        //_host = new WpfD2DHost(d2dControl)
//        var owner = Process.GetCurrentProcess().MainWindowHandle;
//        _host = new WpfD2InteropDHost(owner, image, d3dImage)
//#elif SKIA_RENDER
//        var skiaCanvas = new SKElement();
//        Container.Children.Insert(0, skiaCanvas);
//        skiaCanvas.PaintSurface += SkiaCanvasOnPaintSurface;

//        _host = new WpfSkiaHost(skiaCanvas, new WpfSkiaRenderFactory())
//#else
//        _host = new WpfHost(this)
//#endif
//        {
//            {"VerticalScrollBar", new WpfScrollBar(VerticalScrollBar)},
//            {"HorizontalScrollBar", new WpfScrollBar(HorizontalScrollBar)},
//            {"FakeTextBox", new WpfTextBox(FakeTextBox)}
//        };

        Control = new SharedHexControl();
        //Control.AttachHost(_host);

        var factory = new WpfNativeFactory();
        Mapper = new HexControlPropertyMapper(Control, factory);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SystemEvents.PowerModeChanged += OnPowerModeChanged;

        var window = Window.GetWindow(this);
        if (window is null)
        {
            throw new Exception("Could not obtain window.");
        }

        window.DpiChanged += WindowOnDpiChanged;
        var owner = new WindowInteropHelper(window).Handle;
        InteropImage.WindowOwner = owner;
        _host = new WpfD2DInteropHost(ImageContainer, MainImage, InteropImage)
        {
            {"VerticalScrollBar", new WpfScrollBar(VerticalScrollBar)},
            {"HorizontalScrollBar", new WpfScrollBar(HorizontalScrollBar)},
            {"FakeTextBox", new WpfTextBox(FakeTextBox)}
        };
        Control.AttachHost(_host);

        _host.Resize();
    }

    private void WindowOnDpiChanged(object sender, DpiChangedEventArgs e)
    {
        _host.Invalidate();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;

        var window = Window.GetWindow(this);
        if (window is null)
        {
            throw new Exception("Could not obtain window.");
        }

        window.DpiChanged -= WindowOnDpiChanged;
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode is PowerModes.Resume)
        {
            _host.Invalidate();
        }
    }

#if D2D_RENDER
#elif SKIA_RENDER
    private float _scale = 1;
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);

        _scale = DetermineSkiaScale();
    }

    private void SkiaCanvasOnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        e.Surface.Canvas.SetMatrix(SKMatrix.CreateScale(_scale, _scale));

        var canvas = e.Surface.Canvas;
        _host.DoRender(canvas);
    }

    private float DetermineSkiaScale()
    {
        var presentationSource = PresentationSource.FromVisual(this);
        if (presentationSource == null) throw new Exception("PresentationSource is null");
        var compositionTarget = presentationSource.CompositionTarget;
        if (compositionTarget == null) throw new Exception("CompositionTarget is null");

        var matrix = compositionTarget.TransformToDevice;

        var dpiX = matrix.M11;
        var dpiY = matrix.M22;

        if (dpiX != dpiY) throw new ArgumentException();

        return (float)dpiX;
    }
#else
    protected override void OnRender(DrawingContext context)
    {
        base.OnRender(context);
        _host.DoRender(context);
    }
#endif

    private HexControlPropertyMapper Mapper { get; }

    private SharedHexControl Control { get; }

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

    private static async void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not HexEditorControl editor)
        {
            return;
        }

        var value = e.NewValue;
        await editor.Mapper.SetValue(e.Property.Name, value);
    }
}