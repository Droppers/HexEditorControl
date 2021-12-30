using HexControl.SharedControl.Framework.Drawing;
using Microsoft.UI.Xaml.Media;

namespace HexControl.WinUI.Host;

internal class WinUIBrush : NativeBrush<Brush>
{
    public WinUIBrush(Brush brush) : base(brush) { }
}