using JetBrains.Annotations;

namespace HexControl.Buffers;

[PublicAPI]
public enum ChangeTracking {
    None,
    Undo,
    UndoRedo
}