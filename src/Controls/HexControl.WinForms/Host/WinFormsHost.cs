using HexControl.Framework;
using HexControl.Renderer.Direct2D;
using HexControl.WinForms.Host.Controls;
using SharpDX.Direct2D1;

namespace HexControl.WinForms.Host;

internal class WinFormsHost : WinFormsControl
{
    private readonly D2DControl _control;

    private D2DRenderFactory? _factory;
    private D2DRenderContext? _renderContext;

    public WinFormsHost(D2DControl control) : base(control)
    {
        _control = control;
        _control.Render += OnRender;
    }

    private void OnRender(Factory factory, RenderTarget renderTarget, bool newSurface)
    {
        if (newSurface || _factory is null || _renderContext is null)
        {
            Disposer.SafeDispose(ref _renderContext);

            _factory = new WinFormsRenderFactory(renderTarget);
            _renderContext = new D2DRenderContext(_factory, factory, renderTarget);
            _renderContext.AttachStateProvider(_control);
        }

        RaiseRender(_renderContext, newSurface);
    }

    public override void Invalidate()
    {
        _control.Draw();
    }

    public override void Dispose()
    {
        Disposer.SafeDispose(ref _renderContext);
        _control.Render -= OnRender;

        base.Dispose();
    }
}