namespace HexControl.Core.Events;

public class OffsetChangedEventArgs : EventArgs
{
    public OffsetChangedEventArgs(long oldOffset, long newOffset)
    {
        OldOffset = oldOffset;
        NewOffset = newOffset;
    }

    public long OldOffset { get; set; }
    public long NewOffset { get; set; }
}