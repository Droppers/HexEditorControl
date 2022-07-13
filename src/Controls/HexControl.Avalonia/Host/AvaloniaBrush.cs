using Avalonia.Media;
using HexControl.Framework.Drawing;

namespace HexControl.Avalonia.Host;

internal class AvaloniaBrush : NativeBrush<IBrush>
{
    public AvaloniaBrush(IBrush brush) : base(brush) { }
}