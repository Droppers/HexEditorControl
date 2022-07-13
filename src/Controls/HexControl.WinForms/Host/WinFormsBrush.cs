using HexControl.Framework.Drawing;

namespace HexControl.WinForms.Host;

internal class WinFormsBrush : NativeBrush<Color>
{
    public WinFormsBrush(Color brush) : base(brush) { }
}