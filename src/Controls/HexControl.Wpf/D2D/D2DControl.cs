using System;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using HexControl.Core.Helpers;
using HexControl.SharedControl.Framework.Drawing;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Device = SharpDX.Direct3D11.Device;
using DeviceContext1 = SharpDX.Direct3D11.DeviceContext1;
using Factory = SharpDX.Direct2D1.Factory;
using Image = System.Windows.Controls.Image;
using PixelFormat = SharpDX.Direct2D1.PixelFormat;

namespace HexControl.Wpf.D2D;

internal delegate void RenderEvent(Factory factory, RenderTarget renderTarget, bool newSurface);

internal class D2DControl : Image, IRenderStateProvider
{
    private readonly FrameworkElement _parent;
    private readonly Timer _resizeTimer;

    private bool _canRender;

    private Factory? _d2dFactory;
    private RenderTarget? _d2dRenderTarget;
    private Dx11ImageSource? _d3dSurface;
    private Device? _device;
    private float _dpi = 1;
    private Texture2D? _renderTarget;

    public D2DControl(FrameworkElement parent)
    {
        _parent = parent;
        _parent.SizeChanged += OnSizeChanged;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;

        Stretch = Stretch.Fill;
        VerticalAlignment = VerticalAlignment.Top;
        HorizontalAlignment = HorizontalAlignment.Left;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;

        _resizeTimer = new Timer
        {
            Interval = 500,
            Enabled = true
        };
        _resizeTimer.Elapsed += OnResizeDone;
    }

    public float Dpi
    {
        get => _dpi;
        set
        {
            _dpi = value;
            CreateAndBindTargets();
        }
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

    private void OnResizeDone(object sender, ElapsedEventArgs e)
    {
        _resizeTimer.Stop();
        GC.Collect();
    }

    public event RenderEvent? Render;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        StartD3D();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _resizeTimer.Stop();
        _resizeTimer.Start();
        CreateAndBindTargets();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        EndD3D();
    }

    private void OnIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        CreateAndBindTargets();
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

    public void CreateAndBindTargets()
    {
        CanRender = false;
        if (_d3dSurface is null || _device is null)
        {
            return;
        }

        _d3dSurface.SetRenderTarget(null);

        var width = (int)(_parent.ActualWidth * _dpi);
        var height = (int)(_parent.ActualHeight * _dpi);

        // Not clearing state and flushing will result in a memory leak
        _device.ImmediateContext.ClearState();
        _device.ImmediateContext.Flush();

        Disposer.SafeDispose(ref _d2dFactory);
        Disposer.SafeDispose(ref _renderTarget);
        Disposer.SafeDispose(ref _d2dRenderTarget);

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

        using var surface = _renderTarget.QueryInterface<Surface>();

        _d2dFactory = new Factory();
        var rtp = new RenderTargetProperties(new PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied));
        _d2dRenderTarget = new RenderTarget(_d2dFactory, surface, rtp);
        _d3dSurface.SetRenderTarget(_renderTarget);
        _device.ImmediateContext.Rasterizer.SetViewport(0, 0, width, height);

        CanRender = true;

        OnRender(true);
    }

    private void OnRender(bool newSurface = false)
    {
        if (_d2dFactory is null || _d2dRenderTarget is null)
        {
            return;
        }

        Render?.Invoke(_d2dFactory, _d2dRenderTarget, newSurface);
    }

    public void Invalidate()
    {
        OnRender();
    }

    public void InvalidateImage(SharedRectangle? dirtyRect)
    {
        _device?.ImmediateContext.Flush();
        _d3dSurface?.InvalidateD3DImage(dirtyRect);
    }
}