using System.Diagnostics;
using HexControl.Core.Buffers.Chunks;
using HexControl.Core.Buffers.History.Changes;

namespace HexControl.Core.Buffers.History;

public class ChangeTracker
{
    private readonly BaseBuffer _buffer;
    private readonly Stack<ChangeCollection> _redoStack;
    private readonly Stack<ChangeCollection> _undoStack;

    public ChangeTracker(BaseBuffer buffer)
    {
        _buffer = buffer;
        _undoStack = new Stack<ChangeCollection>();
        _redoStack = new Stack<ChangeCollection>();
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    internal void Push(ChangeCollection changes)
    {
        _undoStack.Push(changes);
        _redoStack.Clear();
    }

    public BufferModification Undo()
    {
        if (!CanUndo)
        {
            throw new InvalidOperationException("Undo stack is empty.");
        }

        Debug.WriteLine("");
        Debug.WriteLine($"Current undo stack ({_undoStack.Count}):");
        foreach (var modification in _undoStack.Reverse())
        {
            switch (modification.Modification)
            {
                case InsertModification insert:
                    Debug.WriteLine(
                        $"doc.Buffer.Insert({insert.Offset}, new byte[] {{ {string.Join(", ", insert.Bytes)} }});");
                    break;
                case WriteModification write:
                    Debug.WriteLine(
                        $"doc.Buffer.Write({write.Offset}, new byte[] {{ {string.Join(", ", write.Bytes)} }});");
                    break;
                case DeleteModification delete:
                    Debug.WriteLine($"doc.Buffer.Delete({delete.Offset}, {delete.Length});");
                    break;
            }
        }

        var pop = _undoStack.Pop();
        ApplyChanges(pop, true);
        _redoStack.Push(pop);

        return pop.Modification;
    }

    public BufferModification Redo()
    {
        if (!CanRedo)
        {
            throw new InvalidOperationException("Redo stack is empty.");
        }

        var pop = _redoStack.Pop();
        ApplyChanges(pop, false);
        _undoStack.Push(pop);

        return pop.Modification;
    }

    private void ApplyChanges(ChangeCollection collection, bool revert)
    {
        var (node, _) = _buffer.GetNodeAt(collection.ChangeOffset);
        if (collection.StartAtPrevious && node?.Previous != null)
        {
            (node, _) = _buffer.GetNodeAt(collection.ChangeOffset - 1);
        }

        var currentTargetNode = node;
        for (var i = 0; i < collection.Count; i++)
        {
            var change = collection.Changes[i];
            var isInsertion = IsInsertChange(change, revert);
            var nextIsInsertion = i + 1 < collection.Changes.Count && IsInsertChange(collection.Changes[i + 1], revert);

            var next = currentTargetNode?.Next;
            var (removed, insertedBefore, insertedAfter) = ApplyChange(change, currentTargetNode, revert);

            if (_buffer.Chunks.Count == 0)
            {
                currentTargetNode = null;
            }
            else if (nextIsInsertion)
            {
                currentTargetNode =
                    (isInsertion ? insertedBefore ? currentTargetNode?.Previous : currentTargetNode?.Next : null) ??
                    currentTargetNode;
            }
            else
            {
                var nodeAfterPreviousDiffers = (insertedBefore || insertedAfter) && !removed
                    ? insertedBefore ? currentTargetNode : currentTargetNode?.Next?.Next
                    : next;
                var currentTargetNodeAfterRemoved = removed ? null : currentTargetNode;
                currentTargetNode =
                    nodeAfterPreviousDiffers ?? currentTargetNode?.Next ?? currentTargetNodeAfterRemoved;
            }
        }
    }

    public bool IsInsertChange(IChange change, bool revert) =>
        change is RemoveChunkChange && revert || change is InsertNewChunkChange && !revert;

    private (bool removed, bool insertedBefore, bool insertedAfter) ApplyChange(IChange change,
        LinkedListNode<IChunk>? currentNode, bool revert)
    {
        var previous = currentNode?.Previous;
        var next = currentNode?.Next;

        switch (change)
        {
            case IChunkChange<ReadOnlyChunk> virtualChange when currentNode?.Value is ReadOnlyChunk virtualChunk:
                Do(virtualChange, currentNode, virtualChunk, revert);
                break;
            case IChunkChange<MemoryChunk> memoryChange when currentNode?.Value is MemoryChunk memoryChunk:
                Do(memoryChange, currentNode, memoryChunk, revert);
                break;
            case IBufferChange documentModification:
                Do(documentModification, currentNode, revert);
                break;
            default:
                throw new InvalidOperationException("Invalid state while undoing or redoing changes.");
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
}