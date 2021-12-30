using HexControl.SharedControl.Framework.Drawing;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Device = SharpDX.Direct3D11.Device;
using Factory = SharpDX.Direct2D1.Factory;
using FeatureLevel = SharpDX.Direct3D.FeatureLevel;

namespace HexControl.WinForms;

internal class RenderEventArgs : EventArgs
{
    public RenderEventArgs(Factory factory, RenderTarget renderTarget, SwapChain swapChain)
    {
        Factory = factory;
        RenderTarget = renderTarget;
        SwapChain = swapChain;
    }

    public Factory Factory { get; }
    public RenderTarget RenderTarget { get; }
    public SwapChain SwapChain { get; }
}

internal class D2DControl : Control, IRenderStateProvider
{
    private readonly object _drawLock = new();

    private bool _canRender;

    private Factory? _d2dFactory;
    private RenderTarget? _d2dRenderTarget;

    private Device? _device;
    private HwndRenderTargetProperties _hwndRenderTargetProperties;

    private RenderTargetProperties _renderTargetProperties;
    private SwapChain? _swapChain;

    private WindowRenderTarget? _windowRenderTarget;

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

    public event EventHandler<RenderStateChangedEventArgs>? RenderStateChanged;
    public event EventHandler<RenderEventArgs>? Render;

    public void InitRendering()
    {
        if (_d2dRenderTarget is not null)
        {
            return;
        }

        lock (_drawLock)
        {
            _d2dFactory = new Factory(FactoryType.MultiThreaded, DebugLevel.None);

            _renderTargetProperties =
                new RenderTargetProperties(new PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Ignore));

            _hwndRenderTargetProperties = new HwndRenderTargetProperties
            {
                Hwnd = Handle,
                PixelSize = new Size2(Width, Height),
                PresentOptions = PresentOptions.Immediately
            };

            _d2dRenderTarget = _windowRenderTarget =
                new WindowRenderTarget(_d2dFactory, _renderTargetProperties, _hwndRenderTargetProperties);
            _d2dRenderTarget.TextAntialiasMode = TextAntialiasMode.Default;


            var desc = new SwapChainDescription
            {
                BufferCount = 1,
                ModeDescription = new ModeDescription(ClientSize.Width, ClientSize.Height, new Rational(60, 1),
                    Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput | Usage.BackBuffer
            };

            Device.CreateWithSwapChain(DriverType.Hardware,
                DeviceCreationFlags.BgraSupport | DeviceCreationFlags.SingleThreaded,
                new[] {FeatureLevel.Level_10_0},
                desc,
                out _device,
                out _swapChain);
        }

        CanRender = true;
    }

    protected override void Dispose(bool disposing)
    {
        CanRender = false;

        lock (_drawLock)
        {
            _d2dFactory?.Dispose();
            _d2dRenderTarget?.Dispose();
            _swapChain?.Dispose();
            _device?.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void OnResize(EventArgs e)
    {
        CanRender = false;

        InitRendering();
        lock (_drawLock)
        {
            if (_device is null)
            {
                throw new InvalidOperationException("Device cannot be null.");
            }

            _windowRenderTarget?.Resize(new Size2(Width, Height));
        }

        base.OnResize(e);

        CanRender = true;
    }


    public void Draw()
    {
        if (_d2dFactory is null || _d2dRenderTarget is null || _swapChain is null)
        {
            return;
        }

        lock (_drawLock)
        {
            Render?.Invoke(this, new RenderEventArgs(_d2dFactory, _d2dRenderTarget, _swapChain));
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Draw();
        base.OnPaint(e);
    }
}