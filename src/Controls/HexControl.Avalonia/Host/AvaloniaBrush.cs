using Avalonia.Media;
using HexControl.SharedControl.Framework.Drawing;

namespace HexControl.Avalonia.Host;

internal class AvaloniaBrush : NativeBrush<IBrush>
{
    public AvaloniaBrush(IBrush brush) : base(brush) { }
}