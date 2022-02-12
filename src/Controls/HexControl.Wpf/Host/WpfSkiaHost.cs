using System.Windows;
using HexControl.Renderer.Skia;
using HexControl.Wpf.Host.Controls;
using SkiaSharp;

namespace HexControl.Wpf.Host;

internal class WpfSkiaHost : WpfControl
{
    private readonly SkiaRenderFactory _renderFactory;
    private SkiaRenderContext? _context;

    public WpfSkiaHost(FrameworkElement element, SkiaRenderFactory renderFactory) : base(element)
    {
        _renderFactory = renderFactory;
    }

    public void DoRender(SKCanvas context)
    {
        _context ??= new SkiaRenderContext(context, _renderFactory);
        _context.Context = context;

        RaiseRender(_context, false);
    }
}