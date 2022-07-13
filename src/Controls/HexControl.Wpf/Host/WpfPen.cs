using System.Windows.Media;
using HexControl.Framework.Drawing;

namespace HexControl.Wpf.Host;

internal class WpfPen : NativePen<Pen>
{
    private readonly Pen _pen;

    public WpfPen(Pen pen) : base(pen)
    {
        _pen = pen;
    }

    public override double Thickness => _pen.Thickness;
}