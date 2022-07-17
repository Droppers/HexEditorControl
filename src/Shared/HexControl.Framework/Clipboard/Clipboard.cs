using System.Runtime.InteropServices;

namespace HexControl.Framework.Clipboard;

internal class Clipboard
{
    private static readonly Lazy<IClipboard> _lazy = new(CreateClipboard);

    public static IClipboard Instance => _lazy.Value;

    private static IClipboard CreateClipboard()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsClipboard();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new MacClipboard();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new LinuxClipboard();
        }

#if DEBUG
        throw new NotSupportedException("Clipboard operations for this platform are not supported.");
#else
        return new FallbackClipboard();
#endif
    }
}
