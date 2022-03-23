using HexControl.Core.Buffers.History.Changes;
using HexControl.Core.Buffers.Modifications;

namespace HexControl.Core.Buffers.History;

public class ChangeCollection
{
    private readonly List<IChange> _changes;
    private bool? _startAtPrevious;

    public ChangeCollection(BufferModification modification, long changeOffset)
    {
        Modification = modification;
        ChangeOffset = changeOffset;

        _changes = new List<IChange>();
    }

    public bool StartAtPrevious
    {
        get => _startAtPrevious ?? false;
        private set
        {
            if (_startAtPrevious is null)
            {
                _startAtPrevious = value;
            }
        }
    }

    public int Count => _changes.Count;

    public IReadOnlyList<IChange> Changes => _changes;

    public BufferModification Modification { get; }
    public long ChangeOffset { get; }

    public static ChangeCollection Delete(long deleteOffset, long deleteLength) =>
        new(new DeleteModification(deleteOffset, deleteLength), deleteOffset);

    public static ChangeCollection Insert(long insertOffset, byte[] insertBytes) =>
        new(new InsertModification(insertOffset, insertBytes), insertOffset);

    public static ChangeCollection Write(long writeOffset, byte[] writeBytes) =>
        new(new WriteModification(writeOffset, writeBytes), writeOffset);

    public void SetStartAtPrevious(bool startAtPrevious = true)
    {
        StartAtPrevious = startAtPrevious;
    }

    public void Prepend(IChange change)
    {
        _changes.Insert(0, change);
    }

    public void Add(IChange change)
    {
        _changes.Add(change);
    }
}