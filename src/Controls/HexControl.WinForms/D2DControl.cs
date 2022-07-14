using HexControl.Framework;
using HexControl.Framework.Drawing;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DXGI;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Factory = SharpDX.Direct2D1.Factory;

namespace HexControl.WinForms;

internal delegate void RenderEvent(Factory factory, RenderTarget renderTarget, bool newSurface);

internal class D2DControl : Control, IRenderStateProvider
{
    private readonly object _drawLock = new();

    private bool _canRender;

    private Factory? _d2dFactory;
    private RenderTarget? _d2dRenderTarget;

    private HwndRenderTargetProperties _hwndRenderTargetProperties;
    private bool _initialRender = true;
    private RenderTargetProperties _renderTargetProperties;

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
    public event RenderEvent? Render;

    private void InitRendering()
    {
        if (DesignMode || _d2dRenderTarget is not null)
        {
            return;
        }

        lock (_drawLock)
        {
            _d2dFactory = new Factory(FactoryType.MultiThreaded, DebugLevel.None);

            _renderTargetProperties =
                new RenderTargetProperties(new PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied));

            _hwndRenderTargetProperties = new HwndRenderTargetProperties
            {
                Hwnd = Handle,
                PixelSize = new Size2(Width, Height),
                PresentOptions = PresentOptions.Immediately
            };

            _d2dRenderTarget = _windowRenderTarget =
                new WindowRenderTarget(_d2dFactory, _renderTargetProperties, _hwndRenderTargetProperties);
            _d2dRenderTarget.TextAntialiasMode = TextAntialiasMode.Default;
        }

        CanRender = true;
    }

    protected override void Dispose(bool disposing)
    {
        CanRender = false;

        lock (_drawLock)
        {
            Disposer.SafeDispose(ref _d2dFactory);
            Disposer.SafeDispose(ref _d2dRenderTarget);
        }

        base.Dispose(disposing);
    }

    protected override void OnResize(EventArgs e)
    {
        CanRender = false;

        InitRendering();
        if (_windowRenderTarget is not null)
        {
            lock (_drawLock)
            {
                var maxSize = _windowRenderTarget.MaximumBitmapSize;
                _windowRenderTarget.Resize(new Size2(Math.Min(maxSize, Width), Math.Min(maxSize, Height)));
            }
        }

        base.OnResize(e);

        CanRender = true;
    }


    public void Draw()
    {
        if (_d2dFactory is null || _d2dRenderTarget is null)
        {
            return;
        }

        lock (_drawLock)
        {
            Render?.Invoke(_d2dFactory, _d2dRenderTarget, _initialRender);
        }

        _initialRender = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Draw();
        base.OnPaint(e);
    }
}