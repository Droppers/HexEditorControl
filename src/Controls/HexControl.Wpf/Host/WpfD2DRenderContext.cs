using HexControl.Renderer.Direct2D;
using HexControl.SharedControl.Framework.Drawing;
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

    public override void End(SharedRectangle? dirtyRect)
    {
        base.End(dirtyRect);

        if (dirtyRect is { } rect)
        {
            dirtyRect = new SharedRectangle(rect.X * Dpi, rect.Y * Dpi, rect.Width * Dpi, rect.Height * Dpi);
        }

        _control.InvalidateImage(dirtyRect);
    }
}