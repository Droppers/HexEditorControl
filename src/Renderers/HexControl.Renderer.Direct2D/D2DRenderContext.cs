using HexControl.Framework.Drawing;
using HexControl.Framework.Drawing.Text;
using HexControl.Framework.Optimizations;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using D2DFactory = SharpDX.Direct2D1.Factory;
using DWFactory = SharpDX.DirectWrite.Factory;

namespace HexControl.Renderer.Direct2D;

internal class D2DRenderContext : RenderContext<SolidColorBrush, D2DPen>
{
    private readonly RenderTarget _context;

    private readonly float[] _dashes = {2, 2};
    private readonly float[] _dots = {1.5f, 1.5f};
    private readonly Stack<PushedType> _pushedTypes;

    private readonly ObjectCache<PenStyle, StrokeStyle> _strokeStyles;
    private readonly ObjectCache<(string fontFamily, float fontSize), TextFormat> _textFormats;

    private readonly Stack<RawMatrix3x2> _transforms;
    private D2DFactory _d2dFactory;
    private DWFactory _dwFactory;

    public D2DRenderContext(D2DRenderFactory factory, D2DFactory d2dFactory, RenderTarget context) :
        base(factory)
    {
        _context = context;

        _d2dFactory = d2dFactory;
        _dwFactory = new DWFactory();

        _transforms = new Stack<RawMatrix3x2>();
        _pushedTypes = new Stack<PushedType>();

        _strokeStyles = new ObjectCache<PenStyle, StrokeStyle>(Convert);
        _textFormats = new ObjectCache<(string fontFamily, float fontSize), TextFormat>(item =>
            new TextFormat(_dwFactory,
                item.fontFamily, FontWeight.Regular, FontStyle.Normal, FontStretch.Normal,
                item.fontSize));
    }

    public float Dpi { get; set; } = 1.0f;

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

    public override void End(SharedRectangle? dirtyRect)
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
        _pushedTypes.Push(PushedType.Transform);

        var matrix = _context.Transform;
        matrix.M31 += Convert(offsetX);
        matrix.M32 += Convert(offsetY);
        _context.Transform = matrix;
    }

    public float Convert(double number) => (float)number * Dpi;

    public override void PushClip(SharedRectangle rectangle)
    {
        _context.PushAxisAlignedClip(Convert(rectangle), AntialiasMode.Aliased);
        _pushedTypes.Push(PushedType.Clip);
    }

    public override void Pop()
    {
        if (!CanRender)
        {
            return;
        }

        var type = _pushedTypes.Pop();
        switch (type)
        {
            case PushedType.Transform:
                _context.Transform = _transforms.Pop();
                break;
            case PushedType.Clip:
                _context.PopAxisAlignedClip();
                break;
        }
    }

    protected override void Clear(SolidColorBrush? brush)
    {
        if (brush is not null)
        {
            _context.Clear(new RawColor4(1, 1, 1, 0));
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
            var strokeStyle = _strokeStyles[pen.Style];
            _context.DrawRectangle(rect, pen.Brush, Convert(pen.Thickness), strokeStyle);
        }
    }

    protected override void DrawLine(D2DPen? pen, SharedPoint startPoint, SharedPoint endPoint)
    {
        if (pen is not null)
        {
            _context.DrawLine(Convert(startPoint), Convert(endPoint), pen.Brush, Convert(pen.Thickness),
                Convert(pen.Style));
        }
    }

    private RawRectangleF Convert(SharedRectangle rectangle)
    {
        // TODO: This is wrong for higher than 1 DPI when coordinate if for example 10.5
        var left = (float)(rectangle.X * Dpi);
        var top = (float)(rectangle.Y * Dpi);
        var width = (float)(rectangle.Width * Dpi);
        var height = (float)(rectangle.Height * Dpi);
        return new RawRectangleF(left, top, left + width, top + height);
    }

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

    private RawVector2 Convert(SharedPoint position) => new((float)(position.X * Dpi), (float)(position.Y * Dpi));

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
            _context.DrawGeometry(geometry, pen.Brush, Convert(pen.Thickness), strokeStyle);
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

        var typeface = (sharedGlyphRun.Typeface as D2DGlyphTypeface)?.Typeface;
        var glyphRun = new GlyphRun
        {
            FontFace = typeface,
            FontSize = Convert(sharedGlyphRun.FontSize),
            BidiLevel = 0,
            IsSideways = false
        };

        glyphRun.Indices = indices;
        glyphRun.Offsets = offsets;
        glyphRun.Advances = advances;
        _context.DrawGlyphRun(Convert(sharedGlyphRun.Position), glyphRun, brush, MeasuringMode.GdiClassic);
    }

    protected override void DrawTextLayout(SolidColorBrush? brush, SharedTextLayout sharedLayout)
    {
        var fontFamily = ((D2DGlyphTypeface)sharedLayout.Typeface).FontFamily;
        var format = _textFormats[(fontFamily, Convert(sharedLayout.Size))];

        // Simple mode
        if (sharedLayout.BrushRanges.Count == 0)
        {
            _context.DrawText(sharedLayout.Text, format,
                new RawRectangleF((int)(sharedLayout.Position.X * Dpi), (int)(sharedLayout.Position.Y * Dpi),
                    int.MaxValue,
                    int.MaxValue), brush, DrawTextOptions.None, MeasuringMode.GdiClassic);
            return;
        }

        using var layout = new TextLayout(_dwFactory, sharedLayout.Text, format, int.MaxValue, int.MaxValue, 1.0f,
            true);
        for (var i = 0; i < sharedLayout.BrushRanges.Count; i++)
        {
            var brushRange = sharedLayout.BrushRanges[i];
            var rangeBrush = GetBrush(brushRange.Brush);
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

        _strokeStyles.Dispose();
        _textFormats.Dispose();

        _d2dFactory = null!;
        _dwFactory.Dispose();
        _dwFactory = null!;
    }

    private enum PushedType
    {
        Transform,
        Clip
    }
}