#if !SKIA_RENDER
using HexControl.Renderer.Direct2D;
using HexControl.Framework.Drawing;
using HexControl.Wpf.D2D;
using SharpDX.Direct2D1;

namespace HexControl.Wpf.Host;

internal class WpfD2DRenderContext : D2DRenderContext
{
    private readonly Direct2DImageSurface _surface;
    //private readonly D2DControl _control;

    public override bool DirtyRect => true;

    public WpfD2DRenderContext(D2DRenderFactory factory, Direct2DImageSurface surface, RenderTarget context) :
        base(factory, surface.D2DFactory, context)
    {
        _surface = surface;
        CanRender = true;
    }

    public override void Begin()
    {
        _surface.BeforeDrawing();

        base.Begin();
    }

    public override void End(SharedRectangle? dirtyRect)
    {
        base.End(dirtyRect);

        if (dirtyRect is { } rect)
        {
            dirtyRect = new SharedRectangle(rect.X * Dpi, rect.Y * Dpi, rect.Width * Dpi, rect.Height * Dpi);
        }

        _surface.AfterDrawing(dirtyRect);
    }
}
#endif