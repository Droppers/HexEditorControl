using System;
using System.Windows.Media;
using HexControl.SharedControl.Framework;
using HexControl.SharedControl.Framework.Drawing;
using HexControl.SharedControl.Framework.Host;
using HexControl.SharedControl.Framework.Host.Typeface;

namespace HexControl.Wpf.Host;

internal class WpfRenderFactory : RenderFactory<Brush, Pen>
{
    // TODO: freeze wpf brushes and pens
    //public override TTarget? SharedToNative<TValue, TTarget>(TValue value) where TTarget : default
    //{
    //    object? res = value switch
    //    {
    //        WpfBrush wpfBrush => wpfBrush.Brush,
    //        ColorBrush colorBrush => new SolidColorBrush(Color.FromArgb(colorBrush.Color.A, colorBrush.Color.R,
    //            colorBrush.Color.G, colorBrush.Color.B)),
    //        SharedPen pen => new Pen(SharedToNative<ISharedBrush, Brush>(pen.Brush), pen.Thickness)
    //        {
    //            DashStyle = pen.Style switch
    //            {
    //                PenStyle.Solid => DashStyles.Solid,
    //                PenStyle.Dashed => DashStyles.Dash,
    //                PenStyle.Dotted => DashStyles.Dot,
    //                _ => DashStyles.Solid
    //            }
    //        },
    //        _ => throw new NotSupportedException("Could not convert property of type: .")
    //    };

    //    return res is TTarget target ? target : default;
    //}

    public override IGlyphTypeface CreateGlyphTypeface(string fontFamily) => new WpfGlyphTypeface(fontFamily);

    public override IGlyphTypeface CreateGlyphTypeface(EmbeddedAsset asset) => new WpfGlyphTypeface(asset);


    public override Brush CreateBrush(ISharedBrush brush) => throw new NotImplementedException();

    public override Pen CreatePen(ISharedPen pen) => throw new NotImplementedException();
}