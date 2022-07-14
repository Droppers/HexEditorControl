using HexControl.Framework.Visual;
using JetBrains.Annotations;

namespace HexControl.Framework.Host.Events;

[PublicAPI]
internal class HostKeyEventArgs : HandledEventArgs
{
    public HostKeyEventArgs(HostKeyModifier modifiers, HostKey key)
    {
        Modifiers = modifiers;
        Key = key;
    }
    
    public HostKeyModifier Modifiers { get; }
    public HostKey Key { get; }
}