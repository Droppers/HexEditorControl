using HexControl.Core.Buffers.History.Changes;

namespace HexControl.Core.Buffers.History;

public class ChangeCollection
{
    private List<IChange> _changes;
    private bool? _startAtPrevious;

    public ChangeCollection(BufferModification modification, long changeOffset)
    {
        Modification = modification;
        ChangeOffset = changeOffset;

        _changes = new List<IChange>();
    }

    public static ChangeCollection Delete(long deleteOffset, long deleteLength)
    {
        return new ChangeCollection(new DeleteModification(deleteOffset, deleteLength), deleteOffset);
    }

    public static ChangeCollection Insert(long insertOffset, byte[] insertBytes)
    {
        return new ChangeCollection(new InsertModification(insertOffset, insertBytes), insertOffset);
    }

    public static ChangeCollection Write(long writeOffset, byte[] writeBytes)
    {
        return new ChangeCollection(new WriteModification(writeOffset, writeBytes), writeOffset);
    }

    public bool StartAtPrevious
    {
        get => _startAtPrevious ?? false; private set
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