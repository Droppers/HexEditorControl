using HexControl.Framework.Visual;
using JetBrains.Annotations;

namespace HexControl.Framework.Host.Events;

[PublicAPI]
internal class HostKeyEventArgs : HandledEventArgs
{
    public HostKeyEventArgs(bool isRepeat, bool isUp, bool isDown, HostKeyModifier modifiers, HostKey key)
    {
        IsRepeat = isRepeat;
        IsUp = isUp;
        IsDown = isDown;
        Modifiers = modifiers;
        Key = key;
    }

    public bool IsRepeat { get; }
    public bool IsUp { get; }
    public bool IsDown { get; }
    public HostKeyModifier Modifiers { get; }
    public HostKey Key { get; }
}