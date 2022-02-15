#if SKIA_RENDER
using System.Windows.Controls;
using HexControl.Renderer.Skia;
using HexControl.Wpf.Host.Controls;
using SkiaSharp;

namespace HexControl.Wpf.Host;

internal class WpfSkiaHost : WpfControl
{
    private readonly SKElement _skElement;
    private SkiaRenderContext? _context;

    public WpfSkiaHost(Grid container) : base(container)
    {
        _skElement = new SKElement();
        _skElement.PaintSurface += OnPaintSurface;
        container.Children.Add(_skElement);
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DoRender(e.Surface.Canvas);
    }

    private void DoRender(SKCanvas context)
    {
        _context ??= new SkiaRenderContext(context, new WpfSkiaRenderFactory());
        _context.CanRender = true;
        _context.Context = context;

        RaiseRender(_context, false);
    }

    public override void Invalidate()
    {
        _skElement.InvalidateVisual();
        return;
        
        if (_context is null)
        {
            _skElement.InvalidateVisual();
            return;
        }

        RaiseRender(_context, false);
    }
}
#endif