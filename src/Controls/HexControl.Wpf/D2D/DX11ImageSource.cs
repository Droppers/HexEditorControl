﻿using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using SharpDX.Direct3D11;
using SharpDX.Direct3D9;
using Resource = SharpDX.DXGI.Resource;

namespace HexControl.Wpf.D2D;

public static class Disposer
{
    public static void SafeDispose<T>(ref T? resource) where T : class
    {
        if (resource is IDisposable disposer)
        {
            try
            {
                disposer.Dispose();
            }
            catch
            {
                // ignored
            }
        }

        resource = null;
    }
}

internal static class NativeMethods
{
    [DllImport("user32.dll", SetLastError = false)]
    public static extern IntPtr GetDesktopWindow();
}

internal class Dx11ImageSource : D3DImage, IDisposable
{
    private static int _activeClients;
    private Direct3DEx? _d3dContext;
    private DeviceEx? _d3dDevice;

    private Texture? _renderTarget;


    public Dx11ImageSource()
    {
        InitializeD3D();
        _activeClients++;
    }

    public void Dispose()
    {
        SetRenderTarget(null);

        Disposer.SafeDispose(ref _renderTarget);

        _activeClients--;
        EndD3D();
    }

    public void InvalidateD3DImage()
    {
        if (_renderTarget is null)
        {
            return;
        }

        if (TryLock(new Duration(default)))
        {
            AddDirtyRect(new Int32Rect(0, 0, PixelWidth, PixelHeight));
        }

        Unlock();
    }

    public void SetRenderTarget(Texture2D? target)
    {
        if (target is null)
        {
            if (TryLock(new Duration(default)))
            {
                SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
                Unlock();
            }

            return;
        }

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

        if (TryLock(new Duration(default)))
        {
            SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface.NativePointer);
        }

        Unlock();
    }

    private void InitializeD3D()
    {
        const CreateFlags createFlags =
            CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.FpuPreserve;

        if (_activeClients != 0)
        {
            return;
        }

        var presentParams = GetPresentParameters();

        _d3dContext = new Direct3DEx();
        _d3dDevice = new DeviceEx(_d3dContext, 0, DeviceType.Hardware, NativeMethods.GetDesktopWindow(), createFlags,
            presentParams);
    }

    public void EndD3D()
    {
        if (_activeClients is not 0)
        {
            return;
        }

        Disposer.SafeDispose(ref _renderTarget);
        Disposer.SafeDispose(ref _d3dDevice);
        Disposer.SafeDispose(ref _d3dContext);
    }

    private void ResetD3D()
    {
        if (_activeClients is 0)
        {
            return;
        }

        var presentParams = GetPresentParameters();
        _d3dDevice?.ResetEx(ref presentParams);
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