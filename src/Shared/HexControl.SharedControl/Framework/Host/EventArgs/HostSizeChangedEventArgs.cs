using HexControl.SharedControl.Framework.Drawing;
using HexControl.SharedControl.Framework.Visual;

namespace HexControl.SharedControl.Framework.Host.EventArgs;

internal class HostSizeChangedEventArgs : HandledEventArgs
{
    public HostSizeChangedEventArgs(SharedSize oldSize, SharedSize newSize)
    {
        OldSize = oldSize;
        NewSize = newSize;
    }

    public SharedSize OldSize { get; }
    public SharedSize NewSize { get; }
}