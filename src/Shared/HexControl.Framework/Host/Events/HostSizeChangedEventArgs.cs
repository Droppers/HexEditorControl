using HexControl.Framework.Drawing;
using HexControl.Framework.Visual;

namespace HexControl.Framework.Host.Events;

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