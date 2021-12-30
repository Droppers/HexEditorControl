using HexControl.SharedControl.Framework.Drawing;
using HexControl.SharedControl.Framework.Visual;

namespace HexControl.SharedControl.Framework.Host.EventArgs;

internal abstract class PointerEventArgs : HandledEventArgs
{
    protected PointerEventArgs(SharedPoint point)
    {
        Point = point;
    }

    public SharedPoint Point { get; }

    public SharedPoint PointRelativeTo(VisualElement child)
    {
        var translatedX = child.Left;
        var translatedY = child.Top;
        return new SharedPoint(Point.X - translatedX, Point.Y - translatedY);
    }
}