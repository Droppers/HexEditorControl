using Avalonia.Media;
using HexControl.SharedControl.Framework.Drawing;
using HexControl.SharedControl.Framework.Host;

namespace HexControl.Avalonia.Host;

internal class AvaloniaNativeFactory : NativeFactory<IBrush, IPen>
{
    public override ISharedBrush WrapBrush(IBrush brush) => new AvaloniaBrush(brush);

    public override ISharedPen WrapPen(IPen pen) => new AvaloniaPen(pen);

    public override IBrush ConvertBrushToNative(ISharedBrush brush)
    {
        return brush switch
        {
            AvaloniaBrush avaloniaBrush => avaloniaBrush.Brush,
            ColorBrush colorBrush => new SolidColorBrush(Color.FromArgb(colorBrush.Color.A, colorBrush.Color.R,
                colorBrush.Color.G, colorBrush.Color.B)),
            _ => throw new NotImplementedException("Cannot convert this brush type back to a native brush.")
        };
    }

    public override IPen ConvertPenToNative(ISharedPen pen)
    {
        return pen switch
        {
            AvaloniaPen avaloniaPen => avaloniaPen.Pen,
            SharedPen sharedPen => new Pen(ConvertBrushToNative(sharedPen.Brush), sharedPen.Thickness)
            {
                DashStyle = Convert(sharedPen.Style)
            },
            _ => throw new NotImplementedException("Cannot convert this pen type back to a native pen.")
        };
    }

    private static IDashStyle? Convert(PenStyle style)
    {
        return style switch
        {
            PenStyle.Dashed => DashStyle.Dash,
            PenStyle.Dotted => DashStyle.Dot,
            PenStyle.Solid => null,
            _ => throw new ArgumentOutOfRangeException(nameof(style), style, null)
        };
    }
}