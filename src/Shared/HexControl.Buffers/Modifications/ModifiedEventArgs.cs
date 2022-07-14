namespace HexControl.Buffers.Modifications;

public class ModifiedEventArgs : EventArgs
{
    public ModifiedEventArgs(IReadOnlyList<BufferModification> modifications, ModificationSource source = ModificationSource.User)
    {
        Modifications = modifications;
        Source = source;
    }

    public IReadOnlyList<BufferModification> Modifications { get; }
    public ModificationSource Source { get; }
}