using Avalonia.Media;
using HexControl.Framework;
using HexControl.Framework.Drawing;
using HexControl.Framework.Host;
using HexControl.Framework.Host.Typeface;

namespace HexControl.Avalonia.Host;

internal class AvaloniaRenderFactory : RenderFactory<IBrush, IPen>
{
    public override IGlyphTypeface CreateGlyphTypeface(string fontFamily) => new AvaloniaGlyphTypeface(fontFamily);

    public override IGlyphTypeface CreateGlyphTypeface(EmbeddedAsset asset) => new AvaloniaGlyphTypeface(asset);

    public override IBrush CreateBrush(ISharedBrush brush)
    {
        return brush switch
        {
            AvaloniaBrush avaloniaBrush => avaloniaBrush.Brush,
            ColorBrush colorBrush => new SolidColorBrush(Convert(colorBrush.Color)),
            _ => throw new NotImplementedException("Cannot convert this brush type back to a native brush.")
        };
    }

    private static Color Convert(System.Drawing.Color color) => new(color.A, color.R, color.G, color.B);

    private static IDashStyle? Convert(PenStyle style)
    {
        return style switch
        {
            PenStyle.Dotted => DashStyle.Dot,
            PenStyle.Dashed => DashStyle.Dash,
            PenStyle.Solid => null,
            _ => throw new ArgumentOutOfRangeException(nameof(style), style, null)
        };
    }

    public override IPen CreatePen(ISharedPen pen)
    {
        return pen switch
        {
            AvaloniaPen avaloniaPen => avaloniaPen.Pen,
            SharedPen sharedPen => new Pen(CreateBrush(sharedPen.Brush), sharedPen.Thickness, Convert(sharedPen.Style)),
            _ => throw new NotImplementedException("Cannot convert this pen type back to a native pen.")
        };
    }
}