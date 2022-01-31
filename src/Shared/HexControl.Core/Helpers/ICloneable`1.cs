namespace HexControl.Core.Helpers;

internal interface ICloneable<out TClone>
{
    public TClone Clone();
}