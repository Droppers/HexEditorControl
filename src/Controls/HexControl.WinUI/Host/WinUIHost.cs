using HexControl.Framework;
using HexControl.Renderer.Direct2D;
using HexControl.Framework.Drawing;
using HexControl.WinUI.Host.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SharpDX;

namespace HexControl.WinUI.Host;

internal class WinUIHost : WinUIControl
{
    private readonly SwapChainRenderer _renderer;
    private readonly SwapChainPanel _renderPanel;
    private WinUIRenderContext? _context;

    public WinUIHost(FrameworkElement element, SwapChainPanel renderPanel) : base(element)
    {
        _renderPanel = renderPanel;
        _renderer = new SwapChainRenderer();
        _renderer.RenderStateChanged += RenderStateChanged;

        renderPanel.Loaded += OnLoaded;
        renderPanel.Unloaded += OnUnloaded;
        renderPanel.SizeChanged += OnSizeChanged;
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

    private SharedSize GetRenderSize() => new(_renderPanel.ActualWidth, _renderPanel.ActualHeight);

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        using var nativePanel = ComObject.As<ISwapChainPanelNative>(_renderPanel);
        _renderer.Initialize(nativePanel, GetRenderSize());
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _renderer.Dispose();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _renderer.Resize(GetRenderSize());
    }

    private void DoRender()
    {
        if (_renderer.SwapChain is null || _renderer.RenderTarget is null || _renderer.Factory is null)
        {
            return;
        }

        if (_context is null)
        {
            _context = new WinUIRenderContext(new WinUIRenderFactory(_renderer.RenderTarget), _renderer.Factory,
                _renderer.SwapChain, _renderer.RenderTarget);
            _context.CanRender = _renderer.CanRender;
        }

        RaiseRender(_context, false);
    }

    public override void Invalidate()
    {
        DoRender();
    }

    public override void Dispose()
    {
        base.Dispose();

        _renderer.RenderStateChanged -= RenderStateChanged;
        _renderer.Dispose();
        Disposer.SafeDispose(ref _context);
    }
}