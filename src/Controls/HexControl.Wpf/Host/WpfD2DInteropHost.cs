using HexControl.Renderer.Direct2D;
using HexControl.Wpf.D2D;
using HexControl.Wpf.Host.Controls;
using Microsoft.Wpf.Interop.DirectX;
using SharpDX.Direct2D1;
using System;
using System.Windows;
using System.Windows.Controls;

namespace HexControl.Wpf.Host;

internal class WpfD2DInteropHost : WpfControl
{
    private readonly System.Windows.Controls.Image _element;
    private readonly D3D11Image _image;
    private D2DRenderFactory? _factory;

    private D2DRenderContext? _renderContext;

    private RenderTarget? _renderTarget;
    private Factory? _d2dFactory;

    public WpfD2DInteropHost(IntPtr owner, System.Windows.Controls.Image element, D3D11Image image) : base(element)
    {
        _element = element;
        _image = image;

        var parent = (FrameworkElement)_element.Parent;
        parent.SizeChanged += _element_SizeChanged;

        _image.WindowOwner = owner;
        _image.OnRender += OnRender;
    }

    private void _element_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
    {
        var parent = (FrameworkElement)_element.Parent;
        _image.SetPixelSize((int)((parent.ActualWidth-17) * 2.5f), (int)((parent.ActualHeight-17) * 2.5f));
        _image.RequestRender();
    }

    private void OnRender(IntPtr handle, bool newSurface)
    {
        if (newSurface)
        {
            _d2dFactory?.Dispose();
            _renderTarget?.Dispose();

            SharpDX.ComObject comObject = new SharpDX.ComObject(handle);
            SharpDX.DXGI.Resource resource = comObject.QueryInterface<SharpDX.DXGI.Resource>();
            SharpDX.Direct3D11.Texture2D texture = resource.QueryInterface<SharpDX.Direct3D11.Texture2D>();
            using var surface = texture.QueryInterface<SharpDX.DXGI.Surface>();

            var properties = new RenderTargetProperties
            {
                DpiX = 0,
                DpiY = 0,
                MinLevel = FeatureLevel.Level_DEFAULT,
                PixelFormat = new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.Unknown, AlphaMode.Ignore),
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
            //_renderContext.AttachStateProvider(_element);
        }

        if(_renderContext is not null)
        {
            RaiseRender(_renderContext);
        }

    }

    public override void Invalidate()
    {
        _image.RequestRender();
        //_element.Invalidate();
    }
}