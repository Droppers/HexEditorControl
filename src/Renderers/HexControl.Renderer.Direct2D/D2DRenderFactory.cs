﻿using HexControl.Framework;
using HexControl.Framework.Drawing;
using HexControl.Framework.Host;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace HexControl.Renderer.Direct2D;

internal abstract class D2DRenderFactory : RenderFactory<Brush, D2DPen>
{
    private readonly RenderTarget _renderTarget;

    protected D2DRenderFactory(RenderTarget renderTarget)
    {
        _renderTarget = renderTarget;
    }

    public override IGlyphTypeface CreateGlyphTypeface(string fontFamily) => new D2DGlyphTypeface(fontFamily);

    // TODO: Support for embedded fonts
    public override IGlyphTypeface CreateGlyphTypeface(EmbeddedAsset asset) => throw new NotImplementedException();

    public override SolidColorBrush CreateBrush(ISharedBrush brush)
    {
        if (brush is ColorBrush colorBrush)
        {
            var color = colorBrush.Color;
            return new SolidColorBrush(_renderTarget,
                new RawColor4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f));
        }

        throw new NotImplementedException($"Shared brush of type {brush.GetType().Name} has not been implemented.");
    }

    public override D2DPen CreatePen(ISharedPen pen)
    {
        if (pen is SharedPen sharedPen)
        {
            return new D2DPen(CreateBrush(sharedPen.Brush), sharedPen.Thickness, sharedPen.Style);
        }

        throw new NotSupportedException($"Shared pen of type {pen.GetType().Name} has not been implemented.");
    }
}