using HexControl.Core.Buffers.History.Changes;

namespace HexControl.Core.Buffers.History;

// TODO: Get rid of the whole groups thing. Groups will be determined based on the modification time difference instead (probably)
internal class ChangeCollection
{
    private readonly List<ChangeGroup> _groups;

    public ChangeCollection(BufferModification modification, long changeOffset)
    {
        Modification = modification;
        ChangeOffset = changeOffset;
        _groups = new List<ChangeGroup>();
        Next();
    }

    public int Count => Groups.Last().Changes.Count;

    public BufferModification Modification { get; }
    public long ChangeOffset { get; }

    public IEnumerable<ChangeGroup> Groups => _groups;

    public void Next()
    {
        _groups.Add(new ChangeGroup());
    }

    public void StartAtPrevious(bool startAtPrevious = true)
    {
        if (_groups.LastOrDefault() is { } group)
        {
            group.StartAtPrevious = startAtPrevious;
        }
    }

    public void Prepend(IChange change)
    {
        _groups.LastOrDefault()?.Changes.Insert(0, change);
    }

    public void Add(IChange change)
    {
        _groups.LastOrDefault()?.Changes.Add(change);
    }

    public class ChangeGroup
    {
        private bool _startAtPrevious;
        private bool _startConfigured;

        public ChangeGroup()
        {
            Changes = new List<IChange>();
        }

        public bool StartAtPrevious
        {
            get => _startAtPrevious;
            set
            {
                if (_startConfigured)
                {
                    return;
                }

                _startConfigured = true;
                _startAtPrevious = value;
            }
        }

        public List<IChange> Changes { get; }
    }
}