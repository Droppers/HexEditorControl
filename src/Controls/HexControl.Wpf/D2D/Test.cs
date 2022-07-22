using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using HexControl.Framework;
using HexControl.Framework.Drawing;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Direct3D9;
using SharpDX.DXGI;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Device = SharpDX.Direct3D11.Device;
using Format = SharpDX.DXGI.Format;
using Query = SharpDX.Direct3D11.Query;
using QueryType = SharpDX.Direct3D11.QueryType;
using RenderTarget = SharpDX.Direct2D1.RenderTarget;
using Surface = SharpDX.DXGI.Surface;
using Usage = SharpDX.Direct3D9.Usage;

namespace HexControl.Wpf.D2D
{
    struct IntSize : IEquatable<IntSize>
    {
        public bool Equals(IntSize other)
        {
            return Width == other.Width && Height == other.Height;
        }

        public IntSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public IntSize(double width, double height) : this((int)width, (int)height)
        {

        }

        public static implicit operator IntSize(System.Windows.Size size)
        {
            return new IntSize { Width = (int)size.Width, Height = (int)size.Height };
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is IntSize && Equals((IntSize)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Width * 397) ^ Height;
            }
        }

        public static bool operator ==(IntSize left, IntSize right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(IntSize left, IntSize right)
        {
            return !left.Equals(right);
        }

        public int Width { get; set; }
        public int Height { get; set; }
    }


    class Direct2DImageSurface : IDisposable
    {
        class SwapBuffer : IDisposable
        {
            private readonly Query _event;
            private readonly SharpDX.Direct3D11.Resource _resource;
            private readonly SharpDX.Direct3D11.Resource _sharedResource;
            public SharpDX.Direct3D9.Surface Texture { get; }
            public RenderTarget Target { get; }
            public IntSize Size { get; }

            private readonly SharpDX.Direct2D1.Factory _d2dFactory;

            public SwapBuffer(SharpDX.Direct2D1.Factory d2dFactory, IntSize size, Vector dpi)
            {
                _d2dFactory = d2dFactory;
                int width = (int)size.Width;
                int height = (int)size.Height;
                _event = new Query(s_dxDevice, new QueryDescription { Type = QueryType.Event });
                using (var texture = new Texture2D(s_dxDevice, new Texture2DDescription
                {
                    Width = width,
                    Height = height,
                    ArraySize = 1,
                    MipLevels = 1,
                    Format = Format.B8G8R8A8_UNorm,
                    Usage = ResourceUsage.Default,
                    SampleDescription = new SampleDescription(2, 0),
                    BindFlags = BindFlags.RenderTarget,
                }))
                using (var surface = texture.QueryInterface<Surface>())

                {
                    _resource = texture.QueryInterface<SharpDX.Direct3D11.Resource>();

                    Target = new RenderTarget(_d2dFactory, surface,
                        new RenderTargetProperties
                        {
                            DpiX = (float)dpi.X,
                            DpiY = (float)dpi.Y,
                            MinLevel = SharpDX.Direct2D1.FeatureLevel.Level_10,
                            PixelFormat = new PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),

                        });
                }
                using (var texture = new Texture2D(s_dxDevice, new Texture2DDescription
                {
                    Width = width,
                    Height = height,
                    ArraySize = 1,
                    MipLevels = 1,
                    Format = Format.B8G8R8A8_UNorm,
                    Usage = ResourceUsage.Default,
                    SampleDescription = new SampleDescription(1, 0),
                    BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                    OptionFlags = ResourceOptionFlags.Shared,
                }))
                using (var resource = texture.QueryInterface<SharpDX.DXGI.Resource>())
                {
                    _sharedResource = texture.QueryInterface<SharpDX.Direct3D11.Resource>();
                    var handle = resource.SharedHandle;
                    using (var texture9 = new Texture(s_d3DDevice, texture.Description.Width,
                        texture.Description.Height, 1,
                        Usage.RenderTarget, SharpDX.Direct3D9.Format.A8R8G8B8, Pool.Default, ref handle))
                        Texture = texture9.GetSurfaceLevel(0);
                }
                Size = size;
            }

            public void Dispose()
            {
                Texture?.Dispose();
                Target?.Dispose();
                _resource?.Dispose();
                _sharedResource?.Dispose();
                _event?.Dispose();
            }

            public void Flush()
            {
                s_dxDevice.ImmediateContext.ResolveSubresource(_resource, 0, _sharedResource, 0, Format.B8G8R8A8_UNorm);
                s_dxDevice.ImmediateContext.Flush();
                s_dxDevice.ImmediateContext.End(_event);
                s_dxDevice.ImmediateContext.GetData(_event).Dispose();
            }
        }

        private D3DImage _image;
        private SwapBuffer? _backBuffer;
        private readonly FrameworkElement _container;
        private readonly D2DControl2 _impl;
        private static Device s_dxDevice;
        private static Direct3DEx s_d3DContext;
        private static DeviceEx s_d3DDevice;
        private Vector _oldDpi;

        public SharpDX.Direct2D1.Factory D2DFactory { get; }

        [DllImport("user32.dll", SetLastError = false)]
        private static extern IntPtr GetDesktopWindow();

        void EnsureDirectX()
        {
            if (s_d3DDevice != null)
                return;
            s_d3DContext = new Direct3DEx();

            SharpDX.Direct3D9.PresentParameters presentparams = new SharpDX.Direct3D9.PresentParameters
            {
                Windowed = true,
                SwapEffect = SharpDX.Direct3D9.SwapEffect.Discard,
                DeviceWindowHandle = GetDesktopWindow(),
                PresentationInterval = PresentInterval.Default
            };
            s_dxDevice = s_dxDevice ?? new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.SingleThreaded);
            s_d3DDevice = new DeviceEx(s_d3DContext, 0, DeviceType.Hardware, IntPtr.Zero, CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.FpuPreserve, presentparams);

        }
            
        public Direct2DImageSurface(FrameworkElement container, D2DControl2 impl)
        {
            _container = container;
            _impl = impl;

            D2DFactory = new SharpDX.Direct2D1.Factory();
        }

        public RenderTarget? GetOrCreateRenderTarget()
        {
            EnsureDirectX();

            s_dxDevice.ImmediateContext.ClearState();
            s_dxDevice.ImmediateContext.Flush();

            var scale = new Vector(_impl.Dpi, _impl.Dpi);//_impl.GetScaling();
            var size = new IntSize(_container.ActualWidth * scale.X, _container.ActualHeight * scale.Y);
            var dpi =  scale * 96;
            _oldDpi = dpi;

            if (_backBuffer != null && _backBuffer.Size == size)
                return _backBuffer.Target;

            if (_image == null || _oldDpi.X != dpi.X || _oldDpi.Y != dpi.Y)
            {
                _image = new D3DImage(dpi.X, dpi.Y);
            }
            _impl.Source = _image;

            Disposer.SafeDispose(ref _backBuffer);
            if (size == default(IntSize))
            {
                _image.Lock();
                _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
                _image.Unlock();
                return null;
            }
            _backBuffer = new SwapBuffer(D2DFactory, size, dpi);

            return _backBuffer.Target;
        }

        void Swap(SharedRectangle? dirtyRect)
        {
            _backBuffer?.Flush();

            var lol = new Stopwatch();
            lol.Start();

            if (!_image.TryLock(TimeSpan.FromMilliseconds(16))) {
                _image.Unlock();
                return;
            }
            _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _backBuffer?.Texture?.NativePointer ?? IntPtr.Zero, true);

            Debug.WriteLine($"part one: {lol.ElapsedMilliseconds}");

            lol.Restart();

            if (dirtyRect is { } rect)
            {
                var dpi = _oldDpi.X / 96;
                rect = new SharedRectangle(rect.X * dpi, rect.Y * dpi, rect.Width * dpi, rect.Height * dpi);

                if (rect.X > _image.PixelWidth || rect.Y > _image.PixelHeight)
                {
                    return;
                }

                var width = rect.Width;
                if (rect.X + rect.Width > _image.PixelWidth)
                {
                    width = _image.PixelWidth - rect.X;
                }

                var height = rect.Height;
                if (rect.Y + rect.Height > _image.PixelHeight)
                {
                    height = _image.PixelHeight - rect.Y;
                }

                _image.AddDirtyRect(new Int32Rect((int)rect.X, (int)rect.Y, (int)width, (int)height));
            }
            else
            {
                _image.AddDirtyRect(new Int32Rect(0, 0, _image.PixelWidth, _image.PixelHeight));
            }

            _image.Unlock();

            Debug.WriteLine($"part two: {lol.ElapsedMilliseconds}");
        }

        public void DestroyRenderTarget()
        {
            Disposer.SafeDispose(ref _backBuffer);
        }

        public void BeforeDrawing()
        {
            // nothing
        }

        public void AfterDrawing(SharedRectangle? dirtyRect = null) => Swap(dirtyRect);

        public void Dispose()
        {
            Disposer.SafeDispose(ref _backBuffer);
        }
    }
}
