using System.Drawing.Drawing2D;
using HexControl.Framework.Drawing;
using HexControl.Framework.Host;

namespace HexControl.WinForms.Host;

internal class WinFormsNativeFactory : NativeFactory<Color, Pen>
{
    public override ISharedBrush WrapBrush(Color brush) => new WinFormsBrush(brush);

    public override ISharedPen WrapPen(Pen pen) => new WinFormsPen(pen);

    public override Color ConvertBrushToNative(ISharedBrush brush)
    {
        return brush switch
        {
            WinFormsBrush winFormsBrush => winFormsBrush.Brush,
            ColorBrush colorBrush => colorBrush.Color,
            _ => throw new ArgumentOutOfRangeException(nameof(brush), brush, null)
        };
    }

    public override Pen ConvertPenToNative(ISharedPen pen)
    {
        return pen switch
        {
            WinFormsPen winFormsPen => winFormsPen.Pen,
            SharedPen sharedPen => new Pen(ConvertBrushToNative(sharedPen.Brush), (float)sharedPen.Thickness)
            {
                DashStyle = Convert(sharedPen.Style)
            },
            _ => throw new ArgumentOutOfRangeException(nameof(pen), pen, null)
        };
    }

    private static DashStyle Convert(PenStyle style)
    {
        return style switch
        {
            PenStyle.Dotted => DashStyle.Dot,
            PenStyle.Dashed => DashStyle.Dash,
            PenStyle.Solid => DashStyle.Solid,
            _ => throw new ArgumentOutOfRangeException(nameof(style), style, null)
        };
    }
}