using System.Windows.Media;
using HexControl.Framework.Drawing;
using HexControl.Framework.Host;

namespace HexControl.Wpf.Host;

internal class WpfNativeFactory : NativeFactory<Brush, Pen>
{
    public override ISharedBrush WrapBrush(Brush brush) => new WpfBrush(brush);

    public override ISharedPen WrapPen(Pen pen) => new WpfPen(pen);

    public override Brush ConvertBrushToNative(ISharedBrush brush)
    {
        return brush switch
        {
            WpfBrush wpfBrush => wpfBrush.Brush,
            ColorBrush colorBrush => new SolidColorBrush(Color.FromArgb(colorBrush.Color.A, colorBrush.Color.R,
                colorBrush.Color.G, colorBrush.Color.B)),
            _ => throw new NotImplementedException("Cannot convert this brush type back to a native brush.")
        };
    }

    public override Pen ConvertPenToNative(ISharedPen pen)
    {
        return pen switch
        {
            WpfPen wpfPen => wpfPen.Pen,
            SharedPen sharedPen => new Pen(ConvertBrushToNative(sharedPen.Brush), sharedPen.Thickness)
            {
                DashStyle = Convert(sharedPen.Style)
            },
            _ => throw new NotImplementedException("Cannot convert this pen type back to a native pen.")
        };
    }

    private static DashStyle Convert(PenStyle style)
    {
        return style switch
        {
            PenStyle.Dashed => DashStyles.Dash,
            PenStyle.Dotted => DashStyles.Dot,
            PenStyle.Solid => DashStyles.Solid,
            _ => throw new ArgumentOutOfRangeException(nameof(style), style, null)
        };
    }
}