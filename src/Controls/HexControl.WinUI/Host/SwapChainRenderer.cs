using System;
using HexControl.SharedControl.Framework.Drawing;
using SharpDX.Direct3D;
using SharpDX.DXGI;
using D2D = SharpDX.Direct2D1;
using D3D = SharpDX.Direct3D11;

namespace HexControl.WinUI.Host;

internal class SwapChainRendererBak : IDisposable, IRenderStateProvider
{
    private Surface? _backBuffer;

    private bool _canRender;

    private D2D.Bitmap1? _d2dBitmapTarget;

    private D2D.DeviceContext? _d2dContext;

    private D2D.Device? _d2dDevice;
    private D3D.Device2? _d3dDevice2;

    private SwapChain2? _swapChain;

    public SwapChain? SwapChain => _swapChain;
    public D2D.RenderTarget? RenderTarget => _d2dContext;
    public D2D.Factory? Factory { get; private set; }

    public void Dispose()
    {
        CanRender = false;

        _backBuffer?.Dispose();
        _d2dBitmapTarget?.Dispose();
        _d2dContext?.Dispose();
        _d2dDevice?.Dispose();
        _swapChain?.Dispose();
        _d3dDevice2?.Dispose();
    }

    public event EventHandler<RenderStateChangedEventArgs>? RenderStateChanged;

    public bool CanRender
    {
        get => _canRender;
        set
        {
            if (_canRender == value)
            {
                return;
            }

            _canRender = value;
            RenderStateChanged?.Invoke(this, new RenderStateChangedEventArgs(value));
        }
    }

    public void Initialize(SharpDX.DXGI.ISwapChainPanelNative nativePanel, SharedSize size)
    {
        const D3D.DeviceCreationFlags creationFlags = D3D.DeviceCreationFlags.BgraSupport;

        using var d3dDevice = new D3D.Device(DriverType.Hardware, creationFlags);
        _d3dDevice2 = d3dDevice.QueryInterface<D3D.Device2>();

        var swapChainDescription1 = new SwapChainDescription1
        {
            AlphaMode = AlphaMode.Ignore,
            BufferCount = 2,
            Flags = SwapChainFlags.AllowModeSwitch,
            Format = Format.B8G8R8A8_UNorm,
            Height = (int)size.Width,
            Width = (int)size.Width,
            SampleDescription = new SampleDescription(1, 0),
            Scaling = Scaling.Stretch,
            Stereo = false,
            SwapEffect = SwapEffect.FlipSequential,
            Usage = Usage.BackBuffer | Usage.RenderTargetOutput
        };


        using var d3dDevice3 = _d3dDevice2.QueryInterface<Device3>();
        using var d3dFactory3 = d3dDevice3.Adapter.GetParent<Factory3>();
        using var swapChain1 = new SwapChain1(d3dFactory3, d3dDevice3, ref swapChainDescription1);
        _swapChain = swapChain1.QueryInterface<SwapChain2>();

        _d2dDevice = new D2D.Device(d3dDevice3);
        Factory = _d2dDevice.Factory;
        _d2dContext = new D2D.DeviceContext(_d2dDevice, D2D.DeviceContextOptions.EnableMultithreadedOptimizations);

        nativePanel.SwapChain = _swapChain;

        CreateRenderTarget();
    }

    public void Resize(SharedSize size)
    {
        if (_swapChain is null)
        {
            return;
        }

        DisposeRenderTarget();

        _swapChain?.ResizeBuffers(
            _swapChain.Description.BufferCount,
            (int)size.Width, (int)size.Height,
            _swapChain.Description1.Format,
            _swapChain.Description1.Flags);

        CreateRenderTarget();
    }

    public void Suspend()
    {
        if (_d3dDevice2 is null)
        {
            return;
        }

        using var dxgiDevice = _d3dDevice2.QueryInterface<Device3>();
        dxgiDevice.Trim();
    }

    private void DisposeRenderTarget()
    {
        CanRender = false;

        if (_d2dContext is not null)
        {
            _d2dContext.Target = null;
        }

        _backBuffer?.Dispose();
        _d2dBitmapTarget?.Dispose();
    }

    private void CreateRenderTarget()
    {
        if (_swapChain is null || _d2dContext is null)
        {
            return;
        }

        var bitmapProperties = new D2D.BitmapProperties1(
            new D2D.PixelFormat(Format.B8G8R8A8_UNorm, D2D.AlphaMode.Premultiplied), 1, 1,
            D2D.BitmapOptions.Target | D2D.BitmapOptions.CannotDraw);

        _backBuffer = _swapChain.GetBackBuffer<Surface>(0);
        _d2dBitmapTarget = new D2D.Bitmap1(_d2dContext, _backBuffer, bitmapProperties);
        _d2dContext.Target = _d2dBitmapTarget;

        CanRender = true;
    }
}