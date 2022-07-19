using System.Buffers;
using System.Text;
using System.Runtime.InteropServices;
#if DEBUG
using System.ComponentModel;
#endif

namespace HexControl.Framework.Clipboard;

internal class WindowsClipboard : IClipboard
{
    private const uint CF_UNICODE_TEXT = 13;

    public async Task<(bool IsSuccess, string content)> TryReadAsync(CancellationToken cancellationToken = default)
    {
        if (!IsClipboardFormatAvailable(CF_UNICODE_TEXT))
        {
            return (false, null!);
        }

        if (!await TryOpenClipboardAsync(cancellationToken))
        {
            return (false, null!);
        }

        var pointer = IntPtr.Zero;
        var handle = IntPtr.Zero;
        byte[]? rentedBuffer = null;

        try
        {
            handle = GetClipboardData(CF_UNICODE_TEXT);
            if (handle == IntPtr.Zero)
            {
                return (false, null!);
            }

            pointer = GlobalLock(handle);
            if (pointer == IntPtr.Zero)
            {
                return (false, null!);
            }

            var size = GlobalSize(handle);

            rentedBuffer = ArrayPool<byte>.Shared.Rent(size);
            Marshal.Copy(pointer, rentedBuffer, 0, size);

            return (true, Encoding.Unicode.GetString(rentedBuffer.AsSpan(0, size)).TrimEnd('\0'));
        }
        finally
        {
            if (pointer != IntPtr.Zero)
            {
                GlobalUnlock(handle);
            }

            CloseClipboard();

            if (rentedBuffer is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }
    }

    public async Task<bool> TrySetAsync(string content, CancellationToken cancellationToken = default)
    {
        if (!await TryOpenClipboardAsync(cancellationToken))
        {
            return false;
        }

        EmptyClipboard();
        var ptr = IntPtr.Zero;
        try
        {
            var size = (content.Length + 1) * 2;
            ptr = Marshal.AllocHGlobal(size);

            if (ptr == IntPtr.Zero)
            {
#if DEBUG
                ThrowLastError();
#endif
                return false;
            }

            var lockPtr = GlobalLock(ptr);

            if (lockPtr == IntPtr.Zero)
            {
#if DEBUG
                ThrowLastError();
#endif
                return false;
            }

            try
            {
                Marshal.Copy(content.ToCharArray(), 0, lockPtr, content.Length);
            }
            finally
            {
                GlobalUnlock(lockPtr);
            }

            if (!SetClipboardData(CF_UNICODE_TEXT, ptr))
            {
#if DEBUG
                ThrowLastError();
#endif
                return false;
            }

            ptr = IntPtr.Zero;
        }
        finally
        {
            if (ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(ptr);
            }

            CloseClipboard();
        }

        return true;
    }

    private static async Task<bool> TryOpenClipboardAsync(CancellationToken cancellationToken)
    {
        var num = 10;
        while (true)
        {
            if (OpenClipboard(IntPtr.Zero))
            {
                return true;
            }

            if (--num == 0)
            {
#if DEBUG
                ThrowLastError();
#endif
                return false;
            }

            await Task.Delay(100, cancellationToken);
        }
    }

#if DEBUG
    private static void ThrowLastError()
    {
        throw new Win32Exception(Marshal.GetLastWin32Error());
    }
#endif

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetClipboardData(uint uFormat, IntPtr data);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int GlobalSize(IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsClipboardFormatAvailable(uint format);
}
