using HexControl.Renderer.Direct2D;
using HexControl.Wpf.D2D;
using SharpDX.Direct2D1;

namespace HexControl.Wpf.Host;

internal class WpfD2DRenderContext : D2DRenderContext
{
    private readonly D2DControl _control;

    public WpfD2DRenderContext(D2DRenderFactory factory, Factory d2dFactory, RenderTarget context, D2DControl control) :
        base(factory, d2dFactory, context)
    {
        _control = control;
    }

    public override void End()
    {
        base.End();

        _control.InvalidateImage();
    }
}