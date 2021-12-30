using System;
using Windows.UI;
using HexControl.SharedControl.Framework.Drawing;
using HexControl.SharedControl.Framework.Host;
using Microsoft.UI.Xaml.Media;

namespace HexControl.WinUI.Host;

internal class WinUINativeFactory : NativeFactory<Brush, int>
{
    public override ISharedBrush WrapBrush(Brush brush) => new WinUIBrush(brush);

    public override ISharedPen WrapPen(int pen) => throw new NotSupportedException("Pens are not supported in WinUI.");

    public override Brush ConvertBrushToNative(ISharedBrush brush)
    {
        return brush switch
        {
            WinUIBrush wpfBrush => wpfBrush.Brush,
            ColorBrush colorBrush => new SolidColorBrush(Color.FromArgb(colorBrush.Color.A, colorBrush.Color.R,
                colorBrush.Color.G, colorBrush.Color.B)),
            _ => throw new NotImplementedException("Cannot convert this brush type back to a native brush.")
        };
    }

    public override int ConvertPenToNative(ISharedPen pen) =>
        throw new NotSupportedException("Pens are not supported in WinUI.");
}