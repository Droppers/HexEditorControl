#if !SKIA_RENDER
using System.Windows.Media;
using HexControl.Renderer.Direct2D;
using HexControl.SharedControl.Framework.Drawing;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using DashStyle = System.Windows.Media.DashStyle;
using SolidColorBrush = SharpDX.Direct2D1.SolidColorBrush;

namespace HexControl.Wpf.Host;

internal class WpfD2DRenderFactory : D2DRenderFactory
{
    private readonly RenderTarget? _target;

    public WpfD2DRenderFactory(RenderTarget target) : base(target)
    {
        _target = target;
    }

    public override SolidColorBrush CreateBrush(ISharedBrush brush)
    {
        if (brush is WpfBrush {Brush: System.Windows.Media.SolidColorBrush wpfColorBrush})
        {
            return Convert(wpfColorBrush);
        }

        return base.CreateBrush(brush);
    }

    private SolidColorBrush Convert(System.Windows.Media.SolidColorBrush brush) =>
        new(_target,
            new RawColor4(brush.Color.R / 255f, brush.Color.G / 255f,
                brush.Color.B / 255f, brush.Color.A / 255f));

    private static PenStyle Convert(DashStyle dash)
    {
        return 0 switch
        {
            _ when dash == DashStyles.Dot => PenStyle.Dotted,
            _ when dash == DashStyles.Dash => PenStyle.Dashed,
            _ when dash == DashStyles.Solid => PenStyle.Solid,
            _ => throw new ArgumentOutOfRangeException(nameof(dash), dash,
                "Only built-in WPF dash styles are supported.")
        };
    }

    public override D2DPen CreatePen(ISharedPen pen)
    {
        if (pen is WpfPen wpfPen && wpfPen.Pen.Brush is System.Windows.Media.SolidColorBrush wpfColorBrush)
        {
            return new D2DPen(Convert(wpfColorBrush), wpfPen.Pen.Thickness, Convert(wpfPen.Pen.DashStyle));
        }

        return base.CreatePen(pen);
    }
}
#endif