using HexControl.Renderer.Direct2D;
using SharpDX.Direct2D1;
using SharpDX.DXGI;
using Factory = SharpDX.Direct2D1.Factory;

namespace HexControl.WinUI.Host;

internal class WinUIRenderContextX : D2DRenderContext
{
    private readonly SwapChain _swapChain;

    public WinUIRenderContextX(D2DRenderFactory factory, Factory d2dFactory, SwapChain swapChain, RenderTarget context)
        : base(factory, d2dFactory, context)
    {
        _swapChain = swapChain;
    }

    public override void End()
    {
        if (!CanRender)
        {
            return;
        }

        base.End();
        _swapChain.Present(0, PresentFlags.None);
    }
}