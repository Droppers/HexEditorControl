using HexControl.SharedControl.Framework.Drawing;

namespace HexControl.WinForms.Host;

internal class WinFormsBrush : NativeBrush<Brush>
{
    public WinFormsBrush(Brush brush) : base(brush) { }
}