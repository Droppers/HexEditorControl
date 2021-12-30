using System.Collections.Generic;
using HexControl.SharedControl.Framework.Drawing;
using HexControl.SharedControl.Framework.Drawing.Text;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using D2DFactory = SharpDX.Direct2D1.Factory;
using DWFactory = SharpDX.DirectWrite.Factory;

namespace HexControl.Renderer.Direct2D;

internal class D2DRenderContext : RenderContext<SolidColorBrush, D2DPen>
{
    private readonly RenderTarget _context;
    private readonly D2DFactory _d2dFactory;

    private readonly float[] _dashes = {2, 2};
    private readonly float[] _dots = {1.5f, 1.5f};
    private readonly DWFactory _dwFactory;

    private readonly Dictionary<SharedGlyphRun, GlyphRun> _instances = new();

    private readonly Stack<RawMatrix3x2> _transforms;

    private TextFormat? _format;

    public D2DRenderContext(D2DRenderFactory factory, D2DFactory d2dFactory, RenderTarget context) :
        base(factory)
    {
        _context = context;

        _d2dFactory = d2dFactory;
        _dwFactory = new DWFactory();

        _transforms = new Stack<RawMatrix3x2>();
    }

    public override bool PreferTextLayout => true;
    public override bool RequiresClear => true;

    public override void Begin()
    {
        if (!CanRender)
        {
            return;
        }

        _context.BeginDraw();
    }

    public override void End()
    {
        if (!CanRender)
        {
            return;
        }

        _context.EndDraw();
    }

    public override void PushTranslate(double offsetX, double offsetY)
    {
        if (!CanRender)
        {
            return;
        }

        _transforms.Push(_context.Transform);

        var matrix = _context.Transform;
        matrix.M31 += (float)offsetX;
        matrix.M32 += (float)offsetY;
        _context.Transform = matrix;
    }

    public override void PushClip(double x, double y, double width, double height) { }

    public override void Pop()
    {
        if (!CanRender)
        {
            return;
        }

        _context.Transform = _transforms.Pop();
    }

    protected override void Clear(SolidColorBrush? brush)
    {
        if (brush is not null)
        {
            _context.Clear(brush.Color);
        }
    }

    protected override void DrawRectangle(SolidColorBrush? brush, D2DPen? pen, SharedRectangle rectangle)
    {
        var rect = Convert(rectangle);
        if (brush is not null)
        {
            _context.FillRectangle(rect, brush);
        }

        if (pen is not null)
        {
            var strokeStyle = Convert(pen.Style);
            _context.DrawRectangle(rect, pen.Brush, (float)pen.Thickness, strokeStyle);
        }
    }

    protected override void DrawLine(D2DPen? pen, SharedPoint startPoint, SharedPoint endPoint)
    {
        if (pen is not null)
        {
            _context.DrawLine(Convert(startPoint), Convert(endPoint), pen.Brush, (float)pen.Thickness,
                Convert(pen.Style));
        }
    }

    private static RawRectangleF Convert(SharedRectangle rectangle) => new((float)rectangle.X,
        (float)rectangle.Y, (float)rectangle.X + (float)rectangle.Width,
        (float)rectangle.Y + (float)rectangle.Height);

    private StrokeStyle Convert(PenStyle style)
    {
        if (style is PenStyle.Dotted or PenStyle.Dashed)
        {
            return new StrokeStyle(_d2dFactory, new StrokeStyleProperties
            {
                DashStyle = DashStyle.Custom
            }, style is PenStyle.Dotted ? _dots : _dashes);
        }

        return new StrokeStyle(_d2dFactory, new StrokeStyleProperties
        {
            DashStyle = DashStyle.Solid
        });
    }

    private static RawVector2 Convert(SharedPoint position) => new((float)position.X, (float)position.Y);

    protected override void DrawPolygon(SolidColorBrush? brush, D2DPen? pen,
        IReadOnlyList<SharedPoint> points)
    {
        using var geometry = new PathGeometry(_d2dFactory);
        using var sink = geometry.Open();
        sink.BeginFigure(Convert(points[0]), FigureBegin.Filled);
        for (var i = 1; i < points.Count; i++)
        {
            var point = points[i];
            sink.AddLine(Convert(point));
        }

        sink.EndFigure(FigureEnd.Closed);
        sink.Close();

        if (brush is not null)
        {
            _context.FillGeometry(geometry, brush);
        }

        if (pen is not null)
        {
            var strokeStyle = Convert(pen.Style);
            _context.DrawGeometry(geometry, pen.Brush, (float)pen.Thickness, strokeStyle);
        }
    }

    protected override void DrawGlyphRun(SolidColorBrush? brush, SharedGlyphRun sharedGlyphRun)
    {
        var advances = new float[sharedGlyphRun.AdvanceWidths.Count];
        var indices = new short[sharedGlyphRun.GlyphIndices.Count];
        var offsets = new GlyphOffset[sharedGlyphRun.GlyphOffsets.Count];

        for (var i = 0; i < sharedGlyphRun.GlyphOffsets.Count; i++)
        {
            indices[i] = (short)sharedGlyphRun.GlyphIndices[i];
            var offset = sharedGlyphRun.GlyphOffsets[i];
            offsets[i] = new GlyphOffset
            {
                AdvanceOffset = (float)offset.X,
                AscenderOffset = -(float)offset.Y
            };
            advances[i] = 0;
        }

        if (!_instances.TryGetValue(sharedGlyphRun, out var glyphRun))
        {
            var typeface = (sharedGlyphRun.Typeface as D2DGlyphTypeface)?.Typeface;
            glyphRun = new GlyphRun();
            glyphRun.FontFace = typeface;
            glyphRun.FontSize = (float)sharedGlyphRun.FontSize;
            glyphRun.BidiLevel = 0;
            glyphRun.IsSideways = false;
            _instances[sharedGlyphRun] = glyphRun;
        }

        glyphRun.Indices = indices;
        glyphRun.Offsets = offsets;
        glyphRun.Advances = advances;

        _context.DrawGlyphRun(Convert(sharedGlyphRun.Position), glyphRun, brush, MeasuringMode.GdiClassic);
    }

    protected override void DrawTextLayout(SolidColorBrush? brush, SharedTextLayout sharedLayout)
    {
        _format ??= new TextFormat(_dwFactory, "Courier New", FontWeight.Regular, FontStyle.Normal,
            FontStretch.Normal, (float)sharedLayout.Size);

        // Simple mode
        if (sharedLayout.BrushRanges.Count == 0)
        {
            _context.DrawText(sharedLayout.Text, _format,
                new RawRectangleF((float)sharedLayout.Position.X, (float)sharedLayout.Position.Y, int.MaxValue,
                    int.MaxValue), brush, DrawTextOptions.None, MeasuringMode.GdiClassic);
            return;
        }

        using var layout = new TextLayout(_dwFactory, sharedLayout.Text, _format, int.MaxValue, int.MaxValue, 1.0f,
            true);
        foreach (var brushRange in sharedLayout.BrushRanges)
        {
            var rangeBrush = brushes[brushRange.Brush];
            if (brushRange.Start is 0 && brushRange.Length == sharedLayout.Text.Length)
            {
                brush = rangeBrush;
                continue;
            }

            if (rangeBrush is not null)
            {
                layout.SetDrawingEffect(rangeBrush.NativePointer,
                    new TextRange(brushRange.Start, brushRange.Length));
            }
        }

        _context.DrawTextLayout(Convert(sharedLayout.Position), layout, brush);
    }

    public override void Dispose()
    {
        base.Dispose();

        _d2dFactory.Dispose();
        _dwFactory.Dispose();
    }
}