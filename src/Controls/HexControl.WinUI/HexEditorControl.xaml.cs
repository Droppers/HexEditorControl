using HexControl.Core;
using HexControl.Renderer.Direct2D;
using HexControl.SharedControl.Control;
using HexControl.SharedControl.Framework.Drawing;
using HexControl.WinUI.Host;
using HexControl.WinUI.Host.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SharpDX;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HexControl.WinUI;

public sealed partial class HexEditorControl : UserControl
{
    private static readonly DependencyProperty DocumentProperty =
        DependencyProperty.Register(nameof(Document),
            typeof(Document),
            typeof(HexEditorControl), new PropertyMetadata(null, PropertyChanged));

    private readonly HexControlPropertyMapper _mapper;
    private readonly SwapChainRenderer _renderer;

    public HexEditorControl()
    {
        InitializeComponent();

        _renderer = new SwapChainRenderer();

        RenderPanel.Loaded += RenderPanelOnLoaded;
        RenderPanel.Unloaded += RenderPanelOnUnloaded;
        RenderPanel.SizeChanged += RenderPanelOnSizeChanged;

        var host = new WinUIHost(this, _renderer)
        {
            {SharedHexControl.VerticalScrollBarName, new WinUIScrollBar(VerticalScrollBar)},
            {SharedHexControl.HorizontalScrollBarName, new WinUIScrollBar(HorizontalScrollBar)}
        };
        var control = new SharedHexControl();
        control.AttachHost(host);

        var factory = new WinUINativeFactory();
        _mapper = new HexControlPropertyMapper(control, factory);
    }

    public Document? Document
    {
        get => _mapper.GetValue<Document>(GetValue(DocumentProperty));
        set => SetValue(DocumentProperty, value);
    }

    private static async void PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not HexEditorControl control)
        {
            return;
        }

        var property = e.Property;
        var propertyName = true switch
        {
            true when property == DocumentProperty => nameof(Document),
            _ => null
        };

        if (propertyName is not null)
        {
            await control._mapper.SetValue(propertyName, e.NewValue);
        }
    }

    private SharedSize GetRenderSize() => new(RenderPanel.ActualWidth, RenderPanel.ActualHeight);

    private void RenderPanelOnLoaded(object sender, RoutedEventArgs e)
    {
        using var nativePanel = ComObject.As<ISwapChainPanelNative>(RenderPanel);
        _renderer.Initialize(nativePanel, GetRenderSize());
    }

    private void RenderPanelOnUnloaded(object sender, RoutedEventArgs e)
    {
        _renderer.Dispose();
    }

    private void RenderPanelOnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _renderer.Resize(GetRenderSize());
    }
}