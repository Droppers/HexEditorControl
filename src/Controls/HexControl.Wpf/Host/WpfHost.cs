using System.Windows;
using System.Windows.Media;
using HexControl.Wpf.Host.Controls;

namespace HexControl.Wpf.Host;

internal class WpfHost : WpfControl
{
    private readonly DrawingGroup _backingStore;
    private WpfRenderContext? _context;

    public WpfHost(FrameworkElement element) : base(element)
    {
        _backingStore = new DrawingGroup();
    }

    public void DoRender(DrawingContext context)
    {
        Invalidate();
        context.DrawDrawing(_backingStore);
    }

    public override void Invalidate()
    {
        using var backingContext = _backingStore.Open();
        _context ??= new WpfRenderContext(backingContext);
        _context.Context = backingContext;
        RaiseRender(_context);
        backingContext.Close();
    }
}