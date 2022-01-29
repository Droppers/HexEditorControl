using HexControl.Renderer.Direct2D;
using HexControl.Wpf.D2D;
using HexControl.Wpf.Host.Controls;
using Microsoft.Wpf.Interop.DirectX;
using SharpDX.Direct2D1;
using System;
using System.Windows;
using System.Windows.Controls;

using WpfImage = System.Windows.Controls.Image;

namespace HexControl.Wpf.Host;

internal class WpfD2DInteropHost : WpfControl
{
    private readonly FrameworkElement _container;
    private readonly WpfImage _element;
    private readonly D3D11Image _image;
    private D2DRenderFactory? _factory;

    private D2DRenderContext? _renderContext;

    private RenderTarget? _renderTarget;
    private Factory? _d2dFactory;

    private float _dpi = 1.0f;

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
    
    public WpfD2DInteropHost(FrameworkElement container, WpfImage element, D3D11Image image) : base(element)
    {
        _container = container;
        _element = element;
        _image = image;
        
        container.SizeChanged += OnSizeChanged;
        _image.OnRender += OnRender;
    }

    private void OnSizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
    {
        Resize();
    }

    private void OnRender(IntPtr handle, bool newSurface)
    {
        if (newSurface)
        {
            _d2dFactory?.Dispose();
            _renderTarget?.Dispose();

            var comObject = new SharpDX.ComObject(handle);
            var resource = comObject.QueryInterface<SharpDX.DXGI.Resource>();
            var texture = resource.QueryInterface<SharpDX.Direct3D11.Texture2D>();
            using var surface = texture.QueryInterface<SharpDX.DXGI.Surface>();

            var properties = new RenderTargetProperties
            {
                DpiX = 0,
                DpiY = 0,
                MinLevel = FeatureLevel.Level_DEFAULT,
                PixelFormat = new PixelFormat(SharpDX.DXGI.Format.Unknown, AlphaMode.Ignore),
                Type = RenderTargetType.Default,
                Usage = RenderTargetUsage.None
            };

            _d2dFactory = new Factory();
            _renderTarget = new RenderTarget(_d2dFactory, surface, properties);
            _renderTarget.AntialiasMode = AntialiasMode.Aliased;
            _renderTarget.TextAntialiasMode = TextAntialiasMode.Cleartype;

            _renderContext?.Dispose();

            _factory = new WpfD2DFactory(_renderTarget);
            _renderContext = new D2DRenderContext(_factory, _d2dFactory, _renderTarget);
            _renderContext.CanRender = true;
            _renderContext.Dpi = _dpi;
        }

        if(_renderContext is not null)
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