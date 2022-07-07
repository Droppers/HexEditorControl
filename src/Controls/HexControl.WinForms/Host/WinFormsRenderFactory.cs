using HexControl.Renderer.Direct2D;
using HexControl.Framework.Drawing;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace HexControl.WinForms.Host;

internal class WinFormsRenderFactory : D2DRenderFactory
{
    private readonly RenderTarget? _target;

    public WinFormsRenderFactory(RenderTarget target) : base(target)
    {
        _target = target;
    }

    public override SolidColorBrush CreateBrush(ISharedBrush brush)
    {
        if (brush is WinFormsBrush {Brush: SolidBrush solidBrush})
        {
            return new SolidColorBrush(_target, Convert(solidBrush.Color));
        }

        return base.CreateBrush(brush);
    }

    private static RawColor4 Convert(Color color) =>
        new(color.R / 255f, color.G / 255f, color.B / 255f,
            color.A / 255f);

    public override D2DPen CreatePen(ISharedPen pen)
    {
        if (pen is WinFormsPen wfPen)
        {
            var brush = new SolidColorBrush(_target, Convert(wfPen.Pen.Color));
            return new D2DPen(brush, wfPen.Pen.Width, PenStyle.Solid);
        }

        return base.CreatePen(pen);
    }
}