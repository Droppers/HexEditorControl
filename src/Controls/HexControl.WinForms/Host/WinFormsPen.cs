using HexControl.SharedControl.Framework.Drawing;

namespace HexControl.WinForms.Host;

internal class WinFormsPen : NativePen<Pen>
{
    private readonly Pen _pen;

    public WinFormsPen(Pen pen) : base(pen)
    {
        _pen = pen;
    }

    public override double Thickness => _pen.Width;
}