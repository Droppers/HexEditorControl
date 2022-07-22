#if !SKIA_RENDER
using System.Windows;
using System.Windows.Interop;
using HexControl.Framework;
using HexControl.Framework.Drawing;
using SharpDX.Direct3D11;
using SharpDX.Direct3D9;
using Resource = SharpDX.DXGI.Resource;

namespace HexControl.Wpf.D2D;

internal class Dx11ImageSource : D3DImage, IDisposable
{
    private static int _activeClients;
    private static Direct3DEx? _d3dContext;
    private static DeviceEx? _d3dDevice;

    private Texture? _renderTarget;


    public Dx11ImageSource()
    {
        InitializeD3D();
        _activeClients++;
    }

    public void Dispose()
    {
        ClearRenderTarget();
        Disposer.SafeDispose(ref _renderTarget);

        _activeClients--;
        EndD3D();
    }

    public void InvalidateD3DImage(SharedRectangle? dirtyRect)
    {
        if (_renderTarget is null)// || dirtyRect is not { } rect)
        {
            return;
        }

        //if (rect.X > PixelWidth || rect.Y > PixelHeight)
        //{
        //    return;
        //}

        //var width = rect.Width;
        //if (rect.X + rect.Width > PixelWidth)
        //{
        //    width = PixelWidth - rect.X;
        //}

        //var height = rect.Height;
        //if (rect.Y + rect.Height > PixelHeight)
        //{
        //    height = PixelHeight - rect.Y;
        //}

        if (TryLock(default(TimeSpan)))
        {
            AddDirtyRect(new Int32Rect(0, 0, PixelWidth, PixelHeight));
            //AddDirtyRect(new Int32Rect((int)rect.X, (int)rect.Y, (int)width, (int)height));
        }

        Unlock();
    }

    public void ClearRenderTarget()
    {
        Lock();
        SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero, false);
        Unlock();
    }

    public void SetRenderTarget(Texture2D target)
    {
        Disposer.SafeDispose(ref _renderTarget);

        var format = TranslateFormat(target);
        var handle = GetSharedHandle(target);

        if (!IsShareable(target))
        {
            throw new ArgumentException("Texture must be created with ResourceOptionFlags.Shared");
        }

        if (format is Format.Unknown)
        {
            throw new ArgumentException("Texture format is not compatible with OpenSharedResource");
        }

        if (handle == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid handle");
        }

        _renderTarget = new Texture(_d3dDevice, target.Description.Width, target.Description.Height, 1,
            Usage.RenderTarget, format, Pool.Default, ref handle);

        using var surface = _renderTarget.GetSurfaceLevel(0);

        Lock();
        SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface.NativePointer, false);
        Unlock();
    }

    private static void InitializeD3D()
    {
        const CreateFlags createFlags =
            CreateFlags.HardwareVertexProcessing | CreateFlags.FpuPreserve;

        if (_activeClients is not 0)
        {
            return;
        }

        _activeClients++;

        var presentParams = GetPresentParameters();

        _d3dContext ??= new Direct3DEx();
        _d3dDevice ??= new DeviceEx(_d3dContext, 0, DeviceType.Hardware, NativeMethods.GetDesktopWindow(), createFlags,
            presentParams);
    }

    public void EndD3D()
    {
        Disposer.SafeDispose(ref _renderTarget);

        if (_activeClients is not 0)
        {
            return;
        }

        Disposer.SafeDispose(ref _d3dDevice);
        Disposer.SafeDispose(ref _d3dContext);
    }

    private static PresentParameters GetPresentParameters() =>
        new()
        {
            Windowed = true,
            SwapEffect = SwapEffect.Discard,
            DeviceWindowHandle = NativeMethods.GetDesktopWindow(),
            PresentationInterval = PresentInterval.Default
        };

    private static IntPtr GetSharedHandle(Texture2D texture)
    {
        using var resource = texture.QueryInterface<Resource>();
        return resource.SharedHandle;
    }

    private static Format TranslateFormat(Texture2D texture)
    {
        return texture.Description.Format switch
        {
            SharpDX.DXGI.Format.R10G10B10A2_UNorm => Format.A2B10G10R10,
            SharpDX.DXGI.Format.R16G16B16A16_Float => Format.A16B16G16R16F,
            SharpDX.DXGI.Format.B8G8R8A8_UNorm => Format.A8R8G8B8,
            _ => Format.Unknown
        };
    }

    private static bool IsShareable(Texture2D texture) =>
        (texture.Description.OptionFlags & ResourceOptionFlags.Shared) != 0;
}
#endif