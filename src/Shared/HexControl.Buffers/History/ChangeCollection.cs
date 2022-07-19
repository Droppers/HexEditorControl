using HexControl.Buffers.History.Changes;
using HexControl.Buffers.Modifications;

namespace HexControl.Buffers.History;

internal class ChangeCollection
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