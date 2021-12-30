using HexControl.Renderer.Direct2D;
using HexControl.Wpf.D2D;
using HexControl.Wpf.Host.Controls;
using SharpDX.Direct2D1;

namespace HexControl.Wpf.Host;

internal class WpfD2DHost : WpfControl
{
    private readonly D2DControl _element;
    private D2DRenderFactory? _factory;
    private RenderTarget? _previousTarget;
    private WpfD2DRenderContext? _renderContext;

    public WpfD2DHost(D2DControl element) : base(element)
    {
        _element = element;
        element.Render += OnRender;
    }

    private void OnRender(object? sender, RenderEventArgs e)
    {
        if (!ReferenceEquals(e.RenderTarget, _previousTarget) || _factory is null || _renderContext is null)
        {
            _renderContext?.Dispose();

            _factory = new WpfD2DFactory(e.RenderTarget);
            _renderContext = new WpfD2DRenderContext(_factory, e.Factory, e.RenderTarget, _element);
            _renderContext.AttachStateProvider(_element);
        }

        RaiseRender(_renderContext);
        _previousTarget = e.RenderTarget;
    }

    public override void Invalidate()
    {
        _element.Invalidate();
    }
}