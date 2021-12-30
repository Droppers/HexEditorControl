using HexControl.SharedControl.Framework;
using HexControl.SharedControl.Framework.Drawing;
using HexControl.SharedControl.Framework.Host;
using HexControl.SharedControl.Framework.Host.Typeface;
using SkiaSharp;

namespace HexControl.Renderer.Skia;

internal abstract class SkiaRenderFactory : RenderFactory<SKPaint, SKPaint>
{
    public override IGlyphTypeface CreateGlyphTypeface(string fontFamily) => throw new NotImplementedException();

    public override IGlyphTypeface CreateGlyphTypeface(EmbeddedAsset asset) => throw new NotImplementedException();

    public override SKPaint CreateBrush(ISharedBrush brush) => throw new NotImplementedException();

    public override SKPaint CreatePen(ISharedPen pen) => throw new NotImplementedException();
}