namespace HexControl.Core.Events;

public class LengthChangedEventArgs : EventArgs
{
    public LengthChangedEventArgs(long oldLength, long newLength)
    {
        OldLength = oldLength;
        NewLength = newLength;
    }

    public long OldLength { get; set; }
    public long NewLength { get; set; }
}