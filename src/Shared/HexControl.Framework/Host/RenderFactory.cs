using HexControl.Framework.Drawing;
using HexControl.Framework.Host.Typeface;

namespace HexControl.Framework.Host;

internal abstract class RenderFactory<TBrush, TPen> : RenderFactory
{
    public abstract TBrush CreateBrush(ISharedBrush brush);
    public abstract TPen CreatePen(ISharedPen pen);
}

internal abstract class RenderFactory
{
    public abstract IGlyphTypeface CreateGlyphTypeface(string fontFamily);
    public abstract IGlyphTypeface CreateGlyphTypeface(EmbeddedAsset asset);
}