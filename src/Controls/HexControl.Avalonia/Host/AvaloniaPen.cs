using Avalonia.Media;
using HexControl.SharedControl.Framework.Drawing;

namespace HexControl.Avalonia.Host;

internal class AvaloniaPen : NativePen<IPen>
{
    public AvaloniaPen(IPen pen) : base(pen) { }
    public override double Thickness => Pen.Thickness;
}