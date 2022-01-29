using Avalonia.Media;
using HexControl.SharedControl.Framework.Drawing;
using Color = System.Drawing.Color;

namespace HexControl.Avalonia.Host;

internal class AvaloniaBrush : NativeBrush<IBrush>
{
    public AvaloniaBrush(IBrush brush) : base(brush) { }
}