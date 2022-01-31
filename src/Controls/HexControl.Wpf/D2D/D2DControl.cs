using System;
using System.Windows;
using System.Windows.Media;
using HexControl.SharedControl.Framework.Drawing;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Device = SharpDX.Direct3D11.Device;
using Factory = SharpDX.Direct2D1.Factory;
using Image = System.Windows.Controls.Image;
using PixelFormat = SharpDX.Direct2D1.PixelFormat;

namespace HexControl.Wpf.D2D;

internal class RenderEventArgs : EventArgs
{
    public RenderEventArgs(Factory factory, RenderTarget renderTarget)
    {
        Factory = factory;
        RenderTarget = renderTarget;
    }

    public Factory Factory { get; }
    public RenderTarget RenderTarget { get; }
}

internal class D2DControl : Image, IRenderStateProvider
{
    private readonly float _dpi = 2.5f;
    private bool _canRender;

    private Factory? _d2dFactory;
    private RenderTarget? _d2dRenderTarget;
    private Dx11ImageSource? _d3dSurface;
    private Device? _device;
    private Texture2D? _renderTarget;

    public D2DControl()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;

        Stretch = Stretch.Fill;
        VerticalAlignment = VerticalAlignment.Top;
        HorizontalAlignment = HorizontalAlignment.Left;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
    }

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

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        StartD3D();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        EndD3D();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        CreateAndBindTargets();
        base.OnRenderSizeChanged(sizeInfo);
    }

    private void OnIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        CanRender = _d3dSurface?.IsFrontBufferAvailable ?? false;
    }

    private void StartD3D()
    {
        _device = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);

        _d3dSurface = new Dx11ImageSource();
        _d3dSurface.IsFrontBufferAvailableChanged += OnIsFrontBufferAvailableChanged;

        CreateAndBindTargets();

        Source = _d3dSurface;
        CanRender = true;
    }

    private void EndD3D()
    {
        CanRender = false;

        if (_d3dSurface is not null)
        {
            _d3dSurface.EndD3D();
            _d3dSurface.IsFrontBufferAvailableChanged -= OnIsFrontBufferAvailableChanged;
        }

        Source = null;

        Disposer.SafeDispose(ref _d2dRenderTarget);
        Disposer.SafeDispose(ref _d2dFactory);
        Disposer.SafeDispose(ref _d3dSurface);
        Disposer.SafeDispose(ref _renderTarget);
        Disposer.SafeDispose(ref _device);
    }

    private void CreateAndBindTargets()
    {
        if (_d3dSurface is null || _device is null)
        {
            return;
        }

        _d3dSurface.SetRenderTarget(null);

        Disposer.SafeDispose(ref _d2dRenderTarget);
        Disposer.SafeDispose(ref _d2dFactory);
        Disposer.SafeDispose(ref _renderTarget);

        var width = Math.Max((int)(ActualWidth * _dpi), 100);
        var height = Math.Max((int)(ActualHeight * _dpi), 100);

        var renderDesc = new Texture2DDescription
        {
            BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
            Format = Format.B8G8R8A8_UNorm,
            Width = width,
            Height = height,
            MipLevels = 1,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            OptionFlags = ResourceOptionFlags.Shared,
            CpuAccessFlags = CpuAccessFlags.None,
            ArraySize = 1
        };

        _renderTarget = new Texture2D(_device, renderDesc);

        var surface = _renderTarget.QueryInterface<Surface>();

        _d2dFactory = new Factory();
        var rtp = new RenderTargetProperties(new PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Ignore));
        _d2dRenderTarget = new RenderTarget(_d2dFactory, surface, rtp);

        _d3dSurface.SetRenderTarget(_renderTarget);

        _device.ImmediateContext.Rasterizer.SetViewport(0, 0, width, height);
    }

    private void OnRender()
    {
        if (_d2dFactory is null || _d2dRenderTarget is null)
        {
            return;
        }

        Render?.Invoke(this, new RenderEventArgs(_d2dFactory, _d2dRenderTarget));
    }

    public void Invalidate()
    {
        OnRender();
    }

    public void InvalidateImage()
    {
        _device?.ImmediateContext.Flush();
        _d3dSurface?.InvalidateD3DImage();
    }
}