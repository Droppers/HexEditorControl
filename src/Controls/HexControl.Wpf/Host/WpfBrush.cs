using System.Windows.Media;
using HexControl.SharedControl.Framework.Drawing;

namespace HexControl.Wpf.Host;

internal class WpfBrush : NativeBrush<Brush>
{
    public WpfBrush(Brush brush) : base(brush) { }
}