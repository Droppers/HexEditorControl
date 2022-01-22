using System;
using HexControl.Renderer.Direct2D;
using HexControl.SharedControl.Framework.Drawing;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using DashStyle = System.Windows.Media.DashStyle;

namespace HexControl.Wpf.Host;

internal class WpfD2DFactory : D2DRenderFactory
{
    private readonly RenderTarget? _target;

    public WpfD2DFactory() { }

    public WpfD2DFactory(RenderTarget target) : base(target)
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

    private static PenStyle Convert(DashStyle dash) => throw new NotSupportedException("Cannot convert WPF dashes");

    public override D2DPen CreatePen(ISharedPen pen)
    {
        if (pen is WpfPen wpfPen && wpfPen.Pen.Brush is System.Windows.Media.SolidColorBrush wpfColorBrush)
        {
            return new D2DPen(Convert(wpfColorBrush), wpfPen.Pen.Thickness, Convert(wpfPen.Pen.DashStyle));
        }

        return base.CreatePen(pen);
    }
}