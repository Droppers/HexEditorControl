using HexControl.Framework.Drawing.Text;
using HexControl.Framework.Host;

namespace HexControl.Framework.Drawing;

internal interface IRenderContext
{
    bool CanRender { get; set; }
    bool Synchronous { get; }

    bool PreferTextLayout { get; }
    bool RequiresClear { get; }
    bool DirtyRect { get; }

    RenderFactory Factory { get; }

    void Clear(ISharedBrush? brush);

    void DrawRectangle(ISharedBrush? brush, ISharedPen? pen, SharedRectangle rectangle);
    void DrawPolygon(ISharedBrush? brush, ISharedPen? pen, ReadOnlySpan<SharedPoint> points);
    void DrawLine(ISharedPen? pen, SharedPoint startPoint, SharedPoint endPoint);

    void PushTranslate(double offsetX, double offsetY);
    void PushClip(SharedRectangle rectangle);
    void Pop();

    void Begin();
    void End(SharedRectangle? dirtyRect);

    void DrawGlyphRun(ISharedBrush? brush, SharedGlyphRun glyphRun);
    void DrawTextLayout(ISharedBrush? brush, SharedTextLayout textLayout);

    void CollectGarbage() { }
}