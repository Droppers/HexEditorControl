using HexControl.SharedControl.Framework.Drawing;
using HexControl.SharedControl.Framework.Drawing.Text;
using SkiaSharp;

namespace HexControl.Renderer.Skia;

internal class SkiaRenderContext : RenderContext<SKPaint, SKPaint>
{
    private int _saveCount;

    public SkiaRenderContext(
        SKCanvas context,
        SkiaRenderFactory factory) : base(factory)
    {
        Context = context;
        Synchronous = true;
    }

    public override bool RequiresClear => true;
    public override bool PreferTextLayout => false;

    public SKCanvas Context { get; set; }

    protected override void Clear(SKPaint? paint)
    {
        if (paint is null)
        {
            Context.Clear();
        }
        else
        {
            Context.Clear(paint.Color);
        }
    }

    public override void PushTranslate(double offsetX, double offsetY)
    {
        Save();
        Context.Translate((float)offsetX, (float)offsetY);
    }

    public override void PushClip(SharedRectangle rectangle)
    {
        Save();
        Context.ClipRect(Convert(rectangle));
    }

    private void Save()
    {
        _saveCount++;
        Context.Save();
    }

    public override void Pop()
    {
        if (_saveCount <= 0)
        {
            throw new InvalidOperationException("Cannot pop state when no state has been set.");
        }

        _saveCount--;
        Context.Restore();
    }

    protected override void DrawRectangle(
        SKPaint? brush,
        SKPaint? pen,
        SharedRectangle rectangle)
    {
        var rect = Convert(rectangle);
        if (brush is not null)
        {
            Context.DrawRect(rect, brush);
        }

        if (pen is not null)
        {
            Context.DrawRect(rect, pen);
        }
    }

    protected override void DrawLine(SKPaint? pen, SharedPoint startPoint, SharedPoint endPoint)
    {
        Context.DrawLine(Convert(startPoint), Convert(endPoint), pen);
    }

    private static SKPoint Convert(SharedPoint point) => new((float)point.X, (float)point.Y);

    private static SKRect Convert(SharedRectangle rectangle) => new((float)rectangle.X, (float)rectangle.Y,
        (float)(rectangle.X + rectangle.Width), (float)(rectangle.Y + rectangle.Height));

    protected override void DrawPolygon(SKPaint? brush, SKPaint? pen, IReadOnlyList<SharedPoint> points)
    {
        using var path = new SKPath();
        for (var i = 0; i < points.Count; i++)
        {
            if (i == 0)
            {
                path.MoveTo((float)points[i].X, (float)points[i].Y);
            }
            else
            {
                path.LineTo((float)points[i].X, (float)points[i].Y);
            }
        }

        path.Close();

        if (brush is not null)
        {
            Context.DrawPath(path, brush);
        }

        if (pen is not null)
        {
            Context.DrawPath(path, pen);
        }
    }

    protected override void DrawGlyphRun(SKPaint? brush, SharedGlyphRun sharedGlyphRun)
    {
        var glyphs = new ushort[sharedGlyphRun.GlyphIndices.Count];
        var positions = new SKPoint[sharedGlyphRun.GlyphIndices.Count];
        for (var i = 0; i < sharedGlyphRun.GlyphIndices.Count; i++)
        {
            var index = sharedGlyphRun.GlyphIndices[i];
            var offset = sharedGlyphRun.GlyphOffsets[i];
            glyphs[i] = index;
            positions[i] = Convert(offset);
        }

        if (sharedGlyphRun.Typeface is not SkiaGlyphTypeface typeface)
        {
            return;
        }

        typeface.Typeface.Size = (float)sharedGlyphRun.FontSize;
        using var blob = new SKTextBlobBuilder();
        blob.AddPositionedRun(glyphs, typeface.Typeface, positions);
        Context.DrawText(blob.Build(), 0, 0, brush);
    }

    protected override void DrawTextLayout(SKPaint? brush, SharedTextLayout layout)
    {
        if (layout.BrushRanges.Count is 0)
        {
            Context.DrawText(layout.Text, Convert(layout.Position), brush);
        }
    }
}