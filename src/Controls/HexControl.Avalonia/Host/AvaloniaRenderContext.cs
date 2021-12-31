using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using HexControl.SharedControl.Framework.Drawing;
using HexControl.SharedControl.Framework.Drawing.Text;

namespace HexControl.Avalonia.Host;

internal class AvaloniaRenderContext : RenderContext<IBrush, IPen>
{
    private readonly Stack<State> _states;

    public AvaloniaRenderContext(DrawingContext context) : base(new AvaloniaRenderFactory())
    {
        CanRender = true; // Avalonia takes care of this for us

        Context = context;
        _states = new Stack<State>();
    }

    public override bool RequiresClear => true;

    public DrawingContext Context { get; set; }

    protected override void Clear(IBrush? brush)
    {
        // TODO: this is absolutely terrible but a temporary workaround since there is no Clear() API
        Context.DrawRectangle(brush, null, new Rect(0, 0, int.MaxValue, int.MaxValue));
    }

    protected override void DrawRectangle(IBrush? brush, IPen? pen, SharedRectangle rectangle)
    {
        Context.DrawRectangle(brush, pen, Convert(rectangle));
    }

    private static Rect Convert(SharedRectangle rectangle) =>
        new(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

    private static Point Convert(SharedPoint point) => new(point.X, point.Y);

    protected override void DrawLine(IPen? pen, SharedPoint startPoint, SharedPoint endPoint)
    {
        Context.DrawLine(pen, Convert(startPoint), Convert(endPoint));
    }

    protected override void DrawPolygon(IBrush? brush, IPen? pen, IReadOnlyList<SharedPoint> points)
    {
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();

        ctx.BeginFigure(Convert(points[0]), true);
        for (var i = 1; i < points.Count; i++)
        {
            ctx.LineTo(Convert(points[i]));
        }

        Context.DrawGeometry(brush, pen, geometry);
    }

    protected override void DrawGlyphRun(IBrush? brush, SharedGlyphRun sharedGlyphRun)
    {
        if (sharedGlyphRun.GlyphIndices.Count == 0)
        {
            return;
        }

        var glyphRun = CreateGlyphRun(sharedGlyphRun);
        Context.DrawGlyphRun(brush, glyphRun);
        AddGarbage(glyphRun);
    }

    protected override void DrawTextLayout(IBrush? brush, SharedTextLayout layout)
    {
        throw new NotImplementedException();
    }

    private static GlyphRun CreateGlyphRun(SharedGlyphRun data)
    {
        var typeFace = (data.Typeface as AvaloniaGlyphTypeface)?.Typeface;

        var glyphIndices = new ushort[data.GlyphIndices.Count];
        var glyphAdvances = new double[data.AdvanceWidths.Count];
        var glyphOffsets = new Vector[data.GlyphOffsets.Count];

        for (var i = 0; i < data.GlyphIndices.Count; i++)
        {
            glyphIndices[i] = data.GlyphIndices[i];
            glyphAdvances[i] = data.AdvanceWidths[i];

            var offset = data.GlyphOffsets[i];
            glyphOffsets[i] = new Vector(offset.X, offset.Y);
        }

        return new GlyphRun(typeFace, data.FontSize, glyphIndices, glyphAdvances, glyphOffsets);
    }

    public override void PushTranslate(double offsetX, double offsetY)
    {
        var actualOffsetX = offsetX;
        var actualOffsetY = offsetY;
        foreach (var state in _states)
        {
            actualOffsetX += state.TranslateX;
            actualOffsetY += state.TranslateY;
        }

        var transform = Context.PushSetTransform(Matrix.CreateTranslation(actualOffsetX, actualOffsetY));
        _states.Push(new State(transform)
        {
            TranslateX = offsetX,
            TranslateY = offsetY
        });
    }

    public override void PushClip(double x, double y, double width, double height)
    {
        var clip = Context.PushClip(new Rect(x, y, width, height));
        _states.Push(new State(clip));
    }

    public override void Pop()
    {
        if (_states.TryPop(out var state))
        {
            state.PushedState.Dispose();
        }
    }

    private struct State
    {
        public DrawingContext.PushedState PushedState { get; }

        public double TranslateX { get; init; }
        public double TranslateY { get; init; }


        public State(DrawingContext.PushedState state)
        {
            PushedState = state;
            TranslateX = 0;
            TranslateY = 0;
        }
    }
}