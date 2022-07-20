using HexControl.Buffers.Chunks;
using HexControl.Buffers.History.Changes;
using HexControl.Buffers.Modifications;
#if DEBUG
using System.Diagnostics;
#endif

namespace HexControl.Buffers.History;

internal class ChangeTracker
{
    private readonly ByteBuffer _buffer;
    private ChangeTracking _changeTracking;
    private readonly Stack<ChangeCollectionGroup> _redoStack;
    private readonly Stack<ChangeCollectionGroup> _undoStack;
    private readonly List<ChangeCollection> _group;

    public ChangeTracker(ByteBuffer buffer, ChangeTracking changeTracking)
    {
        _buffer = buffer;
        _changeTracking = changeTracking;
        _undoStack = new Stack<ChangeCollectionGroup>();
        _redoStack = new Stack<ChangeCollectionGroup>();
        _group = new List<ChangeCollection>();
    }

    public ChangeTracking ChangeTracking
    {
        get => _changeTracking;
        set
        {
            if (_changeTracking is not ChangeTracking.None)
            {
                throw new InvalidOperationException(
                    "Cannot change change tracking when current change tracking is not none.");
            }

            _changeTracking = value;
        }
    }

    public IReadOnlyCollection<ChangeCollectionGroup> UndoStack => _undoStack;

    public IReadOnlyCollection<ChangeCollectionGroup> RedoStack => _redoStack;

    public bool CanUndo => _undoStack.Count > 0 && _buffer.ChangeTracking is not ChangeTracking.None;
    public bool CanRedo => _redoStack.Count > 0 && _buffer.ChangeTracking is ChangeTracking.UndoRedo;

    public bool IsGroup { get; private set; }

    public void Start()
    {
        IsGroup = true;
    }

    public IReadOnlyList<ChangeCollection> End()
    {
        IsGroup = false;

        var collections = new List<ChangeCollection>(_group);
        Push(new ChangeCollectionGroup(collections));
        _group.Clear();

        return collections;
    }


    public void Push(ChangeCollection collection)
    {
        if (ChangeTracking is ChangeTracking.None)
        {
            return;
        }

        if (IsGroup)
        {
            _group.Add(collection);
        }
        else
        {
            Push(new ChangeCollectionGroup(collection));
        }
    }

    private void Push(ChangeCollectionGroup group)
    {
        if (_buffer.ChangeTracking is ChangeTracking.None)
        {
            return;
        }

        _undoStack.Push(group);

        if (_buffer.ChangeTracking is ChangeTracking.UndoRedo)
        {
            _redoStack.Clear();
        }
    }

    public IReadOnlyList<BufferModification> Undo()
    {
        if (!CanUndo)
        {
            throw new InvalidOperationException("Undo stack is empty.");
        }

        // Debug utility
        WriteUndoStack();

        var group = _undoStack.Pop();
        var modifications = new BufferModification[group.Collections.Count];
        for (var i = 0; i < group.Collections.Count; i++)
        {
            var collection = group.Collections[group.Collections.Count - 1 - i];
            ApplyChanges(collection, true);

            modifications[i] = collection.Modification;
        }

        if (_buffer.ChangeTracking is ChangeTracking.UndoRedo)
        {
            _redoStack.Push(group);
        }

        return modifications;
    }

    public IReadOnlyList<BufferModification> Redo()
    {
        if (!CanRedo)
        {
            throw new InvalidOperationException("Redo stack is empty.");
        }

        var group = _redoStack.Pop();
        var modifications = new BufferModification[group.Collections.Count];
        for (var i = 0; i < group.Collections.Count; i++)
        {
            var collection = group.Collections[i];
            ApplyChanges(collection, false);

            modifications[i] = collection.Modification;
        }
        
        _undoStack.Push(group);

        return modifications;
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }

    private void ApplyChanges(ChangeCollection collection, bool revert)
    {
        var firstIsRemove = revert && IsRemoveChange(collection.Changes[0], revert);

        var (node, _) = _buffer.GetNodeAt(collection.ChangeOffset);
        if (collection.StartAtPrevious && !firstIsRemove && node?.Previous != null)
        {
            (node, _) = _buffer.GetNodeAt(collection.ChangeOffset - 1);
        }
        
        var currentTargetNode = node;
        for (var i = 0; i < collection.Count; i++)
        {
            var change = collection.Changes[i];
            var isInsertion = IsInsertChange(change, revert);
            var nextIsInsertion = i + 1 < collection.Changes.Count && IsInsertChange(collection.Changes[i + 1], revert);

            var previous = currentTargetNode?.Previous;
            var next = currentTargetNode?.Next;
            var (removed, insertedBefore, insertedAfter) = ApplyChange(change, currentTargetNode, revert);

            if (_buffer.Chunks.Count is 0)
            {
                currentTargetNode = null;
            }
            else if (nextIsInsertion)
            {
                var insertion = (isInsertion ? (insertedBefore ? currentTargetNode?.Previous : currentTargetNode?.Next) : null);
                var other = removed ? previous : null;
                currentTargetNode = insertion ?? other ?? currentTargetNode;
            }
            else
            {
                var insertion = (insertedBefore || insertedAfter) && !removed
                    ? (insertedBefore ? currentTargetNode : currentTargetNode?.Next?.Next)
                    : next;
                var currentTargetNodeAfterRemoved = removed ? null : currentTargetNode;
                currentTargetNode = insertion ?? currentTargetNode?.Next ?? currentTargetNodeAfterRemoved;
            }
        }
    }

    private static bool IsInsertChange(IChange change, bool revert)
    {
        return change is RemoveChunkChange && revert || change is InsertNewChunkChange && !revert;
    }

    private static bool IsRemoveChange(IChange change, bool revert)
    {
        return change is RemoveChunkChange && !revert || change is InsertNewChunkChange && revert;
    }

    private (bool removed, bool insertedBefore, bool insertedAfter) ApplyChange(IChange change,
        LinkedListNode<IChunk>? currentNode, bool revert)
    {
        var previous = currentNode?.Previous;
        var next = currentNode?.Next;

        switch (change)
        {
            case IChunkChange<IImmutableChunk> immutableChange
                when currentNode?.Value is IImmutableChunk immutableChunk:
                Do(immutableChange, currentNode, immutableChunk, revert);
                break;
            case IChunkChange<MemoryChunk> memoryChange when currentNode?.Value is MemoryChunk memoryChunk:
                Do(memoryChange, currentNode, memoryChunk, revert);
                break;
            case IBufferChange documentModification:
                Do(documentModification, currentNode, revert);
                break;
            default:
                throw new InvalidOperationException("Change type has not been implemented");
        }

        var removed = currentNode is {List: null};
        var insertedBefore = !removed && currentNode?.Previous?.Value != previous?.Value;
        var insertedAfter = !removed && currentNode?.Next?.Value != next?.Value;

        return (removed, insertedBefore, insertedAfter);
    }

    private void Do(IBufferChange change, LinkedListNode<IChunk>? currentNode, bool revert)
    {
        if (revert)
        {
            change.Revert(_buffer, currentNode);
        }
        else
        {
            change.Apply(_buffer, currentNode);
        }
    }

    private void Do<TChunk>(IChunkChange<TChunk> change, LinkedListNode<IChunk> currentNode, TChunk chunk, bool revert)
        where TChunk : IChunk
    {
        if (revert)
        {
            change.Revert(_buffer, currentNode, chunk);
        }
        else
        {
            change.Apply(_buffer, currentNode, chunk);
        }
    }

    private void WriteUndoStack()
    {
#if DEBUG
        Debug.WriteLine("-----------------------------------------");
        Debug.WriteLine($"Current undo stack ({_undoStack.Count}):");

        foreach (var debugGroup in _undoStack.Reverse())
        {
            if (debugGroup.Collections.Count > 1)
            {
                Debug.WriteLine("// Start group");
            }

            foreach (var collection in debugGroup.Collections)
            {
                switch (collection.Modification)
                {
                    case InsertModification insert:
                        Debug.WriteLine(
                            $"_buffer.Insert({insert.Offset}, new byte[] {{ {string.Join(", ", insert.Bytes)} }});");
                        break;
                    case WriteModification write:
                        Debug.WriteLine(
                            $"_buffer.Write({write.Offset}, new byte[] {{ {string.Join(", ", write.Bytes)} }});");
                        break;
                    case DeleteModification delete:
                        Debug.WriteLine($"_buffer.Delete({delete.Offset}, {delete.Length});");
                        break;
                }
            }

            if (debugGroup.Collections.Count > 1)
            {
                Debug.WriteLine("// End group");
            }
        }

        Debug.WriteLine("-----------------------------------------");
#endif
    }
}