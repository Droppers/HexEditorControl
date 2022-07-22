﻿#if !SKIA_RENDER
using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using HexControl.Framework;
using HexControl.Framework.Drawing;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Device = SharpDX.Direct3D11.Device;
using Factory = SharpDX.Direct2D1.Factory;
using Image = System.Windows.Controls.Image;
using PixelFormat = SharpDX.Direct2D1.PixelFormat;
using Timer = System.Timers.Timer;

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

    private Texture2D? _renderTexture;
    private Texture2D? _displayTexture;

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
            Interval = 300,
            Enabled = true
        };
        _resizeTimer.Elapsed += OnResizeFinished;
    }

    public ResizeMode ResizeMode { get; set; } = ResizeMode.Debounce;

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

    private void OnResizeFinished(object? sender, ElapsedEventArgs e)
    {
        if (ResizeMode is ResizeMode.Debounce)
        {
            Dispatcher.Invoke(() =>
            {
                CreateAndBindTargets();
                Width = double.NaN;
                Height = double.NaN;
            });
        }

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
        switch (ResizeMode)
        {
            case ResizeMode.Immediate:
            // First resize call when debounce is enable will be immediate to accept programmatic resizes
            case ResizeMode.Debounce when !_resizeTimer.Enabled:
                CreateAndBindTargets();
                break;
            case ResizeMode.Debounce:
                CanRender = false;
                Width = (_d3dSurface?.PixelWidth ?? 0) / _dpi;
                Height = (_d3dSurface?.PixelHeight ?? 0) / _dpi;
                break;
        }

        // Initial render with actual size
        if (e.PreviousSize.Width is 0 || e.PreviousSize.Height is 0)
        {
            return;
        }

        _resizeTimer.Stop();
        _resizeTimer.Start();
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
        _device = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.SingleThreaded);

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
        Disposer.SafeDispose(ref _renderTexture);
        Disposer.SafeDispose(ref _displayTexture);
        Disposer.SafeDispose(ref _device);
    }

    public void CreateAndBindTargets()
    {
        CanRender = false;
        if (_d3dSurface is null || _device is null)
        {
            return;
        }

        var width = (int)(_parent.ActualWidth * _dpi);
        var height = (int)(_parent.ActualHeight * _dpi);

        // Not clearing state and flushing will result in a memory leak
        _device.ImmediateContext.ClearState();
        _device.ImmediateContext.Flush();

        Disposer.SafeDispose(ref _d2dFactory);
        Disposer.SafeDispose(ref _renderTexture);
        Disposer.SafeDispose(ref _displayTexture);
        Disposer.SafeDispose(ref _d2dRenderTarget);

        _renderTexture = CreateTexture(width, height);
        _displayTexture = CreateTexture(width, height);

        using var surface = _renderTexture.QueryInterface<Surface>();

        _d2dFactory = new Factory();
        var rtp = new RenderTargetProperties(new PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied));
        _d2dRenderTarget = new RenderTarget(_d2dFactory, surface, rtp);

        _d3dSurface.ClearRenderTarget();
        _d3dSurface.SetRenderTarget(_displayTexture);

        _device.ImmediateContext.Rasterizer.SetViewport(0, 0, width, height);

        CanRender = true;

        OnRender(true);
    }

    private Texture2D CreateTexture(int width, int height)
    {

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
        return new Texture2D(_device, renderDesc);
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
        if (_device is null || _d3dSurface is null)
        {
            return;
        }

        _device.ImmediateContext.ResolveSubresource(_renderTexture, 0, _displayTexture, 0, Format.B8G8R8A8_UNorm);
        _device.ImmediateContext.Flush();

        _d3dSurface.InvalidateD3DImage(dirtyRect);
    }
}
#endif