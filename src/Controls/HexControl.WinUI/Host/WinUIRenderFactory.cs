using HexControl.Renderer.Direct2D;
using HexControl.SharedControl.Framework.Drawing;
using SharpDX.Direct2D1;

namespace HexControl.WinUI.Host;

internal class WinUIRenderFactory : D2DRenderFactory
{
    public WinUIRenderFactory(RenderTarget renderTarget) : base(renderTarget) { }

    // TODO: IMPLEMENT THESE
    public override SolidColorBrush CreateBrush(ISharedBrush brush) => base.CreateBrush(brush);
    public override D2DPen CreatePen(ISharedPen pen) => base.CreatePen(pen);
}