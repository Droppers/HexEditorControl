using HexControl.Renderer.Direct2D;
using HexControl.SharedControl.Framework.Drawing;
using HexControl.WinUI.Host.Controls;
using Microsoft.UI.Xaml.Controls;

namespace HexControl.WinUI.Host;

internal class WinUIHost : WinUIControl
{
    private readonly SwapChainRenderer _renderer;
    private WinUIRenderContextX? _context;

    public WinUIHost(Control control, SwapChainRenderer renderer) : base(control)
    {
        _renderer = renderer;
        _renderer.RenderStateChanged += RenderStateChanged;
    }

    private void RenderStateChanged(object? sender, RenderStateChangedEventArgs e)
    {
        if (_context is not null)
        {
            _context.CanRender = e.CanRender;
        }

        if (e.CanRender)
        {
            DoRender();
        }
    }

    public void DoRender()
    {
        if (_renderer.SwapChain is null || _renderer.RenderTarget is null || _renderer.Factory is null)
        {
            return;
        }

        if (_context is null)
        {
            _context = new WinUIRenderContextX(new WinUIRenderFactory(_renderer.RenderTarget), _renderer.Factory,
                _renderer.SwapChain, _renderer.RenderTarget);
            _context.CanRender = _renderer.CanRender;
        }

        RaiseRender(_context);
    }

    public override void Invalidate()
    {
        DoRender();
    }

    public override void Dispose()
    {
        _renderer.RenderStateChanged -= RenderStateChanged;

        base.Dispose();
    }
}