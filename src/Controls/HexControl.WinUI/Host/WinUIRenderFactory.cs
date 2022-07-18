using HexControl.Renderer.Direct2D;
using SharpDX.Direct2D1;

namespace HexControl.WinUI.Host;

internal class WinUIRenderFactory : D2DRenderFactory
{
    public WinUIRenderFactory(RenderTarget renderTarget) : base(renderTarget) { }
}