namespace HexControl.Buffers.Modifications;

public class ModifiedEventArgs : EventArgs
{
    public ModifiedEventArgs(BufferModification modification, ModificationSource source = ModificationSource.User)
    {
        Modification = modification;
        Source = source;
    }

    public BufferModification Modification { get; }
    public ModificationSource Source { get; }
}