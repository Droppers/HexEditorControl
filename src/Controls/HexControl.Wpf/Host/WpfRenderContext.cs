using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using HexControl.SharedControl.Framework.Drawing;
using HexControl.SharedControl.Framework.Drawing.Text;

namespace HexControl.Wpf.Host;

internal class WpfRenderContext : RenderContext<Brush, Pen>
{
    // TODO: remove or improve this, just experimental
    private readonly NumberSubstitution _subst = new();
    private FontFamily? _family;
    private Typeface? _font;

    public WpfRenderContext(DrawingContext context) : base(new WpfRenderFactory())
    {
        Context = context;
    }

    public DrawingContext Context { get; set; }

    protected override void Clear(Brush? brush)
    {
        throw new NotImplementedException("Clear()");
    }

    protected override void DrawRectangle(Brush? brush, Pen? pen, SharedRectangle rectangle)
    {
        Context.DrawRectangle(brush, pen, Convert(rectangle));
    }

    protected override void DrawLine(Pen? pen, SharedPoint startPoint, SharedPoint endPoint)
    {
        Context.DrawLine(pen, Convert(startPoint), Convert(endPoint));
    }

    private static Rect Convert(SharedRectangle rectangle) =>
        new(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

    private static Point Convert(SharedPoint point) => new(point.X, point.Y);

    protected override void DrawPolygon(Brush? brush, Pen? pen, IReadOnlyList<SharedPoint> points)
    {
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();

        var first = true;
        foreach (var point in points)
        {
            if (first)
            {
                ctx.BeginFigure(Convert(point), true, true);
            }
            else
            {
                ctx.LineTo(Convert(point), true, true);
            }

            first = false;
        }

        geometry.Freeze();
        Context.DrawGeometry(brush, pen, geometry);
    }

    protected override void DrawGlyphRun(Brush? brush, SharedGlyphRun glyphRun)
    {
        if (glyphRun.GlyphIndices.Count == 0)
        {
            return;
        }

        Context.DrawGlyphRun(brush, CreateGlyphRun(glyphRun));
    }

    protected override void DrawTextLayout(Brush? brush, SharedTextLayout layout)
    {
        _family ??= new FontFamily("Courier New");
        _font ??= new Typeface(_family, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        var text = new FormattedText(layout.Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, _font,
            layout.Size, brush, _subst, TextFormattingMode.Display, 1.0f);
        foreach (var range in layout.BrushRanges)
        {
            text.SetForegroundBrush(GetBrush(range.Brush), range.Start, range.Length);
        }

        Context.DrawText(text, Convert(layout.Position));
    }

    public override void PushTranslate(double offsetX, double offsetY)
    {
        Context.PushTransform(new TranslateTransform(offsetX, offsetY));
    }

    public override void PushClip(SharedRectangle rectangle)
    {
        Context.PushClip(new RectangleGeometry(new Rect(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height)));
    }

    public override void Pop()
    {
        Context.Pop();
    }

    private static GlyphRun CreateGlyphRun(SharedGlyphRun glyphRun)
    {
        var glyphOffsets = new Point[glyphRun.GlyphOffsets.Count];
        for (var i = 0; i < glyphRun.GlyphOffsets.Count; i++)
        {
            var offset = glyphRun.GlyphOffsets[i];
            glyphOffsets[i] = new Point(offset.X, -offset.Y);
        }

        var typeFace = (glyphRun.Typeface as WpfGlyphTypeface)?.Typeface;
        return new GlyphRun(typeFace,
            0,
            false,
            glyphRun.FontSize,
            1.0f,
            glyphRun.GlyphIndices,
            Convert(glyphRun.Position),
            glyphRun.AdvanceWidths,
            glyphOffsets,
            null,
            null,
            null,
            null,
            null);
    }
}