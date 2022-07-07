using System.Runtime.InteropServices;

namespace HexControl.WinUI;

// In Windows App SDK the GUID has changed compared to UWP, therefore we inherit the SharpDX interface an replace it with the correct GUID.
[Guid("63aad0b8-7c24-40ff-85a8-640d944cc325")]
// ReSharper disable once InconsistentNaming
internal class ISwapChainPanelNative : SharpDX.DXGI.ISwapChainPanelNative
{
    public ISwapChainPanelNative(IntPtr nativePtr) : base(nativePtr) { }
}