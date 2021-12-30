using HexControl.Renderer.Skia;
using HexControl.SharedControl.Framework;
using HexControl.SharedControl.Framework.Host.Typeface;

namespace HexControl.Wpf.Host;

internal class WpfSkiaRenderFactory : SkiaRenderFactory
{
    //public override TTarget? SharedToNative<TValue, TTarget>(TValue value) where TTarget : default
    //{
    //    object? res = value switch
    //    {
    //        WpfBrush wpfBrush => wpfBrush.Brush switch
    //        {
    //            SolidColorBrush wpfColorBrush => new SKPaint
    //            {
    //                Color = new SKColor(wpfColorBrush.Color.R, wpfColorBrush.Color.G, wpfColorBrush.Color.B, wpfColorBrush.Color.A),
    //                Style = SKPaintStyle.Fill,
    //                IsAntialias = true,
    //                SubpixelText = true
    //            },
    //            _ => throw new NotSupportedException($"Brush type {wpfBrush.Brush.GetType().Namespace} is not supported for SkiaSharp rendering.")
    //        },
    //        ColorBrush colorBrush => new SKPaint
    //        {
    //            Color = new SKColor(colorBrush.Color.R, colorBrush.Color.G, colorBrush.Color.B, colorBrush.Color.A),
    //            Style = SKPaintStyle.Fill,
    //            IsAntialias = true,
    //            SubpixelText = true
    //        },
    //        SharedPen pen => CreatePenPaint(pen),
    //        _ => throw new NotSupportedException($"Could not convert property of type: {value?.GetType().Name}.")
    //    };

    //    return res is TTarget target ? target : default;
    //}

    //private SKPaint CreatePenPaint(SharedPen pen)
    //{
    //    var paint = SharedToNative<ISharedBrush, SKPaint>(pen.Brush);
    //    if (paint is null)
    //    {
    //        throw new Exception("Could not create SKPaint for pen.");
    //    }

    //    paint.StrokeWidth = (float)pen.Thickness;
    //    paint.Style = SKPaintStyle.Stroke;
    //    paint.PathEffect = pen.Style switch
    //    {
    //        PenStyle.Dashed => SKPathEffect.CreateDash(new float[] { 10, 10 }, 20),
    //        PenStyle.Dotted => SKPathEffect.CreateDash(new float[] { 1, 2 }, 2),
    //        PenStyle.Solid => null,
    //        _ => throw new NotSupportedException($"Pen style {pen.Style} is not yet supported.")
    //    };
    //    return paint;
    //}

    public override IGlyphTypeface CreateGlyphTypeface(string fontFamily) => new SkiaGlyphTypeface(fontFamily);


    // TODO: impl
    public override IGlyphTypeface CreateGlyphTypeface(EmbeddedAsset asset) => new SkiaGlyphTypeface("Courier New");
}