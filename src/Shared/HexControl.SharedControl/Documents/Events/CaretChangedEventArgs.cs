using JetBrains.Annotations;

namespace HexControl.SharedControl.Documents.Events;

[PublicAPI]
public class CaretChangedEventArgs : EventArgs
{
    internal CaretChangedEventArgs(Caret oldCaret, Caret newCaret, bool scrollToCaret)
    {
        OldCaret = oldCaret;
        NewCaret = newCaret;
        ScrollToCaret = scrollToCaret;
    }

    public Caret OldCaret { get; }
    public Caret NewCaret { get; }
    public bool ScrollToCaret { get; }
}