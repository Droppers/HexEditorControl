using HexControl.SharedControl.Framework.Drawing;
using HexControl.SharedControl.Framework.Host.Typeface;

namespace HexControl.SharedControl.Framework.Host;

internal abstract class RenderFactory<TBrush, TPen> : RenderFactory
{
    public abstract TBrush CreateBrush(ISharedBrush brush);
    public abstract TPen CreatePen(ISharedPen pen);
}

internal abstract class RenderFactory
{
    public abstract IGlyphTypeface CreateGlyphTypeface(string fontFamily);
    public abstract IGlyphTypeface CreateGlyphTypeface(EmbeddedAsset asset);

    /*
        ConvertBrush(ISharedBrush brush);
        ConvertPen(ISharedPen pen);

        CreateTypeface(string fontFamily);
     */
}