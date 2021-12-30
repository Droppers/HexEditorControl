using HexControl.Renderer.Direct2D;
using HexControl.WinForms.Host.Controls;
using SharpDX.Direct2D1;

namespace HexControl.WinForms.Host;

internal class WinFormsHost : WinFormsControl
{
    private readonly D2DControl _control;

    private D2DRenderFactory? _factory;
    private RenderTarget? _previousTarget;
    private D2DRenderContext? _renderContext;

    public WinFormsHost(D2DControl control) : base(control)
    {
        _control = control;
        _control.Render += OnRender;
    }

    private void OnRender(object? sender, RenderEventArgs e)
    {
        if (!ReferenceEquals(e.RenderTarget, _previousTarget) || _factory is null || _renderContext is null)
        {
            _renderContext?.Dispose();

            _factory = new WinFormsRenderFactory(e.RenderTarget);
            _renderContext = new D2DRenderContext(_factory, e.Factory, e.RenderTarget);
            _renderContext.AttachStateProvider(_control);
        }

        RaiseRender(_renderContext);

        _previousTarget = e.RenderTarget;
    }

    public override void Invalidate()
    {
        _control.Draw();
    }

    public override void Dispose()
    {
        _renderContext?.Dispose();
        _control.Render -= OnRender;

        base.Dispose();
    }
}