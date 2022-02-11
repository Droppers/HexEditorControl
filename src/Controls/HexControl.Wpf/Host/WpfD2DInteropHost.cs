using System;
using System.Diagnostics;
using System.Windows;
using HexControl.Renderer.Direct2D;
using HexControl.Wpf.Host.Controls;
using Microsoft.Wpf.Interop.DirectX;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Factory = SharpDX.Direct2D1.Factory;
using Resource = SharpDX.DXGI.Resource;
using WpfImage = System.Windows.Controls.Image;

namespace HexControl.Wpf.Host;

internal class WpfD2DInteropHost : WpfControl
{
    private readonly FrameworkElement _container;
    private readonly WpfImage _element;
    private readonly D3D11Image _image;
    private Factory? _d2dFactory;

    private float _dpi = 1.0f;
    private D2DRenderFactory? _factory;

    private D2DRenderContext? _renderContext;

    private RenderTarget? _renderTarget;

    public WpfD2DInteropHost(FrameworkElement container, WpfImage element, D3D11Image image, bool antiAliased) : base(element)
    {
        this.antiAliased = antiAliased;
        _container = container;
        _element = element;
        _image = image;

        container.SizeChanged += OnSizeChanged;
        _image.OnRender += OnRender;
    }

    private readonly bool antiAliased;

    public float Dpi
    {
        get => _dpi;
        set
        {
            _dpi = value;
            if (_renderContext is not null)
            {
                _renderContext.Dpi = value;
                Invalidate();
            }
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        Resize();
    }

    private void OnRender(IntPtr handle, bool newSurface)
    {
        if (newSurface)
        {
            _d2dFactory?.Dispose();
            _renderTarget?.Dispose();
            _renderContext?.Dispose();

            using var comObject = new ComObject(handle);
            using var resource = comObject.QueryInterface<Resource>();
            using var texture = resource.QueryInterface<Texture2D>();
            using var surface = texture.QueryInterface<Surface>();

            var properties = new RenderTargetProperties
            {
                DpiX = 0,
                DpiY = 0,
                MinLevel = FeatureLevel.Level_DEFAULT,
                PixelFormat = new PixelFormat(Format.Unknown, AlphaMode.Premultiplied),
                Type = RenderTargetType.Default,
                Usage = RenderTargetUsage.None
            };

            _d2dFactory = new Factory();
            _renderTarget = new RenderTarget(_d2dFactory, surface, properties)
            {
                AntialiasMode = antiAliased ? AntialiasMode.PerPrimitive : AntialiasMode.Aliased,
                TextAntialiasMode = TextAntialiasMode.Cleartype
            };

            _factory = new WpfD2DFactory(_renderTarget);
            _renderContext = new D2DRenderContext(_factory, _d2dFactory, _renderTarget)
            {
                CanRender = true,
                Dpi = _dpi
            };
        }

        if (_renderContext is not null)
        {
            RaiseRender(_renderContext);
        }
    }
    
    public void Resize()
    {
        _image.SetPixelSize((int)(_container.ActualWidth * Dpi), (int)(_container.ActualHeight * Dpi));
        _image.RequestRender();
    }

    public override void Invalidate()
    {
        _image.RequestRender();
    }
}