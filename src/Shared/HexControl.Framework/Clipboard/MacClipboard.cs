using System.Runtime.InteropServices;

namespace HexControl.Framework.Clipboard;

internal class MacClipboard : IClipboard
{
    private const string NS_PASTEBOARD_TYPE_STRING = "public.utf8-plain-text";

    public Task<(bool IsSuccess, string content)> TryReadAsync(CancellationToken cancellationToken = default)
    {
        var nsString = objc_getClass("NSString");
        
        var stringForTypeRegister = sel_registerName("stringForType:");

        var nsPasteboard = objc_getClass("NSPasteboard");
        var generalPasteboard = objc_msgSend(nsPasteboard, sel_registerName("generalPasteboard"));

        var utfTextType = objc_msgSend(objc_msgSend(nsString, sel_registerName("alloc")), sel_registerName("initWithUTF8String:"), "public.utf8-plain-text");
        var nsStringPboardType = objc_msgSend(objc_msgSend(nsString, sel_registerName("alloc")), sel_registerName("initWithUTF8String:"), "NSStringPboardType");


        var ptr = objc_msgSend(generalPasteboard, stringForTypeRegister, nsStringPboardType);
        var charArray = objc_msgSend(ptr, sel_registerName("initWithUTF8String:"));

        var content = Marshal.PtrToStringAnsi(charArray);
        return Task.FromResult((content is not null, content!));
    }

    public Task<bool> TrySetAsync(string content, CancellationToken cancellationToken = default)
    {
        var nsString = objc_getClass("NSString");
        var str = IntPtr.Zero;
        var dataType = IntPtr.Zero;
        try
        {
            str = objc_msgSend(objc_msgSend(nsString, sel_registerName("alloc")), sel_registerName("initWithUTF8String:"), content);
            dataType = objc_msgSend(objc_msgSend(nsString, sel_registerName("alloc")), sel_registerName("initWithUTF8String:"), NS_PASTEBOARD_TYPE_STRING);

            var nsPasteboard = objc_getClass("NSPasteboard");
            var generalPasteboard = objc_msgSend(nsPasteboard, sel_registerName("generalPasteboard"));

            objc_msgSend(generalPasteboard, sel_registerName("clearContents"));
            objc_msgSend(generalPasteboard, sel_registerName("setString:forType:"), str, dataType);
        }
        finally
        {
            if (str != IntPtr.Zero)
            {
                objc_msgSend(str, sel_registerName("release"));
            }

            if (dataType != IntPtr.Zero)
            {
                objc_msgSend(dataType, sel_registerName("release"));
            }
        }

        return Task.FromResult(true);
    }

    [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static extern IntPtr objc_getClass(string className);

    [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, string arg1);

    [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

    [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static extern IntPtr sel_registerName(string selectorName);
}
