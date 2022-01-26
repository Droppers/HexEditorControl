using System.Buffers;
using System.Diagnostics;
using HexControl.Core.Buffers.Chunks;
using HexControl.Core.Buffers.History;
using HexControl.Core.Buffers.History.Changes;
using HexControl.Core.Events;

namespace HexControl.Core.Buffers;

// TODO: move to file
public record BufferModification(long Offset);

public record WriteModification(long Offset, byte[] Bytes) : BufferModification(Offset);

public record InsertModification(long Offset, byte[] Bytes) : BufferModification(Offset);

public record DeleteModification(long Offset, long Length) : BufferModification(Offset);

// TODO: move to file
public enum ModificationSource
{
    User,
    Undo,
    Redo
}

// TODO: move to file
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

// TODO: get rid if interface, only abstract class?
public abstract class BaseBuffer : IBuffer
{
    private readonly ArrayPool<byte> _arrayPool;

    protected BaseBuffer()
    {
        _arrayPool = ArrayPool<byte>.Shared;

        Chunks = new LinkedList<IChunk>();
        Changes = new ChangeTracker(this);
    }

    internal LinkedList<IChunk> Chunks { get; }
    public ChangeTracker Changes { get; }

    public long Length { get; set; }

    // TODO: track length when making changes to the buffer
    public event EventHandler<LengthChangedEventArgs>? LengthChanged;

    public long OriginalLength { get; private set; } = -1;

    public void Write(long writeOffset, byte value)
    {
        Write(writeOffset, new[] {value});
    }

    // TODO: this code is not pretty
    public async Task<long> ReadAsync(
        long readOffset,
        byte[] buffer,
        List<ModifiedRange>? modifications = null,
        CancellationToken cancellationToken = default)
    {
        if (readOffset > Length)
        {
            return 0;
        }

        var (node, currentOffset) = GetNodeAt(readOffset);

        // Shortcut for in case all data can be read from the current node
        if (node is not null && readOffset + buffer.Length < currentOffset + node.Value.Length)
        {
            if (node.Value is MemoryChunk)
            {
                modifications?.Add(new ModifiedRange(readOffset, buffer.Length));
            }
            return await node.Value.ReadAsync(buffer, readOffset, buffer.Length, cancellationToken);
        }

        var readLength = buffer.Length;
        var actualRead = 0L;
        var modificationStart = -1L;
        while (node != null && readLength - actualRead > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chunk = node.Value;
            var isLastChunk = node.Next is null || chunk.Length >= readLength - actualRead;

            if (chunk is MemoryChunk && modificationStart == -1)
            {
                modificationStart = currentOffset;
            }

            if ((chunk is not MemoryChunk || isLastChunk) && modificationStart != -1)
            {
                modifications?.Add(new ModifiedRange(modificationStart, currentOffset));
                modificationStart = -1;
            }

            var relativeOffset = readOffset + actualRead - currentOffset;
            var length = Math.Min(readLength - actualRead, chunk.Length - relativeOffset);
            var readBuffer = _arrayPool.Rent((int)length);
            var bufferLength = await chunk.ReadAsync(readBuffer, relativeOffset, length, cancellationToken);
            Array.Copy(readBuffer, 0, buffer, actualRead, bufferLength);
            _arrayPool.Return(readBuffer);

            actualRead += bufferLength;
            currentOffset += chunk.Length;
            node = node.Next;
        }

        return actualRead;
    }

    public async Task<byte[]> ReadAsync(long readOffset, long readLength, List<ModifiedRange>? modifiedRanges = null,
        CancellationToken cancellationToken = default)
    {
        var buffer = new byte[readLength];
        await ReadAsync(readOffset, buffer, modifiedRanges);
        return buffer;
    }

    // TODO: fill gaps with zeros, e.g. writing at 300 when document is completely empty, or writing past the length.
    public void Write(long writeOffset, byte[] writeBytes)
    {
        var oldLength = Length;
        var changes = ChangeCollection.Write(writeOffset, writeBytes);

        var (node, currentOffset) = GetNodeAt(writeOffset);

        if (node == null)
        {
            changes.Add(InsertChunk(null, new MemoryChunk(this, writeBytes)));
            PushChanges(changes, oldLength);
            return;
        }

        var relativeOffset = writeOffset - currentOffset;
        var chunk = node.Value;
        if (chunk is MemoryChunk memoryChunk)
        {
            WriteToMemoryChunk(changes, node, relativeOffset, writeBytes, memoryChunk);
        }
        else if (writeOffset == currentOffset && node.Previous?.Value is MemoryChunk previousMemoryChunk)
        {
            changes.SetStartAtPrevious();
            WriteToMemoryChunk(changes, node.Previous, previousMemoryChunk.Length, writeBytes, previousMemoryChunk);
        }
        else if (writeOffset + writeBytes.Length > currentOffset + chunk.Length &&
                 node.Next?.Value is MemoryChunk nextMemoryChunk)
        {
            WriteToMemoryChunk(changes, node.Next, -(currentOffset + chunk.Length - writeOffset), writeBytes,
                nextMemoryChunk);
        }
        else if (writeOffset == currentOffset && node.Previous is null)
        {
            changes.Add(InsertChunk(node, new MemoryChunk(this, writeBytes), true));
            if (node.Previous is null)
            {
                throw new InvalidOperationException("Previous change should have created a new previous node.");
            }

            RemoveAfter(changes, node.Previous, writeBytes.Length);
        }
        else
        {
            WriteCreateNewMemoryChunk(node, relativeOffset, writeBytes, changes);
        }

        PushChanges(changes, oldLength);
    }

    public void Insert(long insertOffset, byte[] insertBytes)
    {
        var oldLength = Length;

        var (node, currentOffset) = GetNodeAt(insertOffset);
        if (node == null)
        {
            return;
        }

        var changes = ChangeCollection.Insert(insertOffset, insertBytes);

        var relativeOffset = insertOffset - currentOffset;
        var chunk = node.Value;
        if (chunk is MemoryChunk memoryChunk)
        {
            changes.Add(new InsertToMemoryChange(relativeOffset, insertBytes).Apply(this, node, memoryChunk));
        }
        else if (currentOffset == insertOffset && node.Previous?.Value is MemoryChunk previousChunk)
        {
            changes.SetStartAtPrevious();
            changes.Add(new InsertToMemoryChange(relativeOffset, insertBytes).Apply(this, node, previousChunk));
        }
        else if (insertOffset == 0)
        {
            changes.Add(InsertChunk(node, new MemoryChunk(this, insertBytes), true));
        }
        else if (insertOffset == Length)
        {
            changes.Add(InsertChunk(node, new MemoryChunk(this, insertBytes)));
        }
        else
        {
            InsertInMiddleOfChunk(node, relativeOffset, new MemoryChunk(this, insertBytes), changes);
        }

        PushChanges(changes, oldLength);
    }

    public void Delete(long deleteOffset, long deleteLength)
    {
        var oldLength = Length;
        var (node, currentOffset) = GetNodeAt(deleteOffset);

        var changes = ChangeCollection.Delete(deleteOffset, deleteLength);

        if (node?.Value is ReadOnlyChunk && deleteOffset - currentOffset + deleteLength < node.Value.Length && deleteOffset - currentOffset is not 0)
        {
            DeleteInMiddleOfChunk(changes, node, deleteOffset - currentOffset, deleteLength);
            PushChanges(changes, oldLength);
            return;
        }

        var bytesToDelete = deleteLength;
        while (bytesToDelete > 0 && node != null)
        {
            var nextNode = node.Next;
            var chunk = node.Value;
            var chunkLength = chunk.Length;

            var relativeOffset = deleteOffset + (deleteLength - bytesToDelete) - currentOffset;
            var length = Math.Min(bytesToDelete, chunk.Length - relativeOffset);
            RemoveFromChunk(changes, node, relativeOffset, length);

            if (Chunks.First == null)
            {
                changes.Add(InsertChunk(null, new MemoryChunk(this, Array.Empty<byte>())));
            }

            bytesToDelete -= length;
            currentOffset += chunkLength;
            node = nextNode;
        }

        PushChanges(changes, oldLength);
    }

    public void Undo()
    {
        var modification = Changes.Undo();
        OnModified(modification, ModificationSource.Undo);
    }

    public void Redo()
    {
        var modification = Changes.Redo();
        OnModified(modification, ModificationSource.Redo);
    }

    public event EventHandler<ModifiedEventArgs>? Modified;

    public long Read(
        long readOffset,
        byte[] buffer,
        List<ModifiedRange>? modifications = null)
    {
        if (readOffset > Length)
        {
            return 0;
        }

        var (node, currentOffset) = GetNodeAt(readOffset);
        
        // Shortcut for in case all data can be read from the current node
        if (node is not null && readOffset + buffer.Length < currentOffset + node.Value.Length)
        {
            if (node.Value is MemoryChunk)
            {
                modifications?.Add(new ModifiedRange(readOffset, buffer.Length));
            }
            return node.Value.Read(buffer, readOffset, buffer.Length);
        }

        var readLength = buffer.Length;
        var actualRead = 0L;
        var modificationStart = -1L;
        while (node != null && readLength - actualRead > 0)
        {
            var chunk = node.Value;

            var isLastChunk = node.Next is null || chunk.Length >= readLength - actualRead;
            if (chunk is MemoryChunk && modificationStart is -1)
            {
                modificationStart = currentOffset;
            }

            if ((chunk is not MemoryChunk || isLastChunk) && modificationStart is not -1)
            {
                modifications?.Add(new ModifiedRange(modificationStart, node.Next is null ? Length : currentOffset));
                modificationStart = -1;
            }

            var relativeOffset = readOffset + actualRead - currentOffset;
            var length = Math.Min(readLength - actualRead, chunk.Length - relativeOffset);

            var readBuffer = _arrayPool.Rent((int)length);
            var bufferLength = chunk.Read(readBuffer, relativeOffset, length);
            Array.Copy(readBuffer, 0, buffer, actualRead, bufferLength);
            _arrayPool.Return(readBuffer);

            actualRead += bufferLength;
            currentOffset += chunk.Length;
            node = node.Next;
        }

        return actualRead;
    }

    // TODO: insert beyond the document Length, fill gap with 0's, and insert that buffer at 'Length'
    public void Insert(long insertOffset, byte insertByte)
    {
        Insert(insertOffset, new[] {insertByte});
    }

    public long Find(byte[] pattern, long startOffset, bool backward, bool wrapAround)
    {
        // TODO: this is a complete mess and should be rewritten
        var strategy = new KmpFindStrategy(pattern);
        var (startNode, _) = GetNodeAt(startOffset);
        IChunk? groupFirstChunk = null;
        long groupOffset = 0; // TODO: startOffset (startNodeOffset)
        long groupLength = 0;
        var didWrapAround = false;
        var node = startNode;
        var i = 0;
        while (node != null)
        {
            var chunk = node.Value;
            var endReached = node == startNode && i > 0;
            var next = backward ? node.Previous : node.Next;
            groupFirstChunk ??= chunk;
            Debug.WriteLine(
                $"{chunk.GetType().Name} ({groupFirstChunk.GetType().Name}) ({chunk.Length}), {groupOffset} with length {groupLength}");
            groupLength += chunk.Length;
            var nextIsDifferent = groupFirstChunk is MemoryChunk && next?.Value is ReadOnlyChunk ||
                                  groupFirstChunk is ReadOnlyChunk && next?.Value is MemoryChunk;
            if (nextIsDifferent && next?.Value.Length > pattern.Length || endReached || next is null)
            {
                Debug.WriteLine($"SEARCH: {groupOffset} with length {groupLength} {nextIsDifferent}");
                if (groupFirstChunk is ReadOnlyChunk)
                {
                    var res = FindInVirtual(strategy, groupOffset, groupLength, backward);
                    if (res != -1)
                    {
                        return res;
                    }
                }
                else
                {
                    var res = FindInMemory(strategy, Math.Max(0, groupOffset - (pattern.Length - 1)),
                        groupLength + (pattern.Length * 2 - 2), backward);
                    if (res != -1)
                    {
                        return res;
                    }
                }

                if (endReached)
                {
                    break;
                }

                groupFirstChunk = null;
                groupOffset += groupLength;
                groupLength = 0;
            }

            if (next == null && wrapAround)
            {
                Console.WriteLine("---------------- WRAP");
                didWrapAround = true;
                groupOffset = backward ? Length - 1 : 0;
                node = backward ? Chunks.Last : Chunks.First;
            }
            else
            {
                node = next;
            }

            i++;
        }

        return -1;
    }

    private long FindInMemory(IFindStrategy strategy, long startOffset, long length, bool backward) =>
        strategy.SearchInBuffer(this, startOffset, length, backward);

    protected abstract long FindInVirtual(IFindStrategy strategy, long startOffset, long length, bool backward);

    private void PushChanges(ChangeCollection changes, long oldLength)
    {
        Changes.Push(changes);
        OnModified(changes.Modification, ModificationSource.User);

        if(oldLength != Length)
        {
            OnLengthChanged(oldLength, Length);
        }
    }

    protected virtual void OnLengthChanged(long oldLength, long newLength)
    {
        LengthChanged?.Invoke(this, new LengthChangedEventArgs(oldLength, newLength));
    }

    protected virtual void OnModified(BufferModification modification, ModificationSource source)
    {
        Modified?.Invoke(this, new ModifiedEventArgs(modification, source));
    }

    private void InsertInMiddleOfChunk(
        LinkedListNode<IChunk> node,
        long relativeOffset,
        IChunk chunk,
        in ChangeCollection changes)
    {
        var newVirtualChunk = node.Value.Clone();
        newVirtualChunk.SourceOffset += relativeOffset;
        newVirtualChunk.Length -= relativeOffset;

        changes.Add(InsertChunk(node, chunk));
        var memoryNode = node.Next;
        if (memoryNode is null)
        {
            throw new InvalidOperationException("Expected inserted node to be present.");
        }

        RemoveBefore(changes, memoryNode, node.Value.Length - relativeOffset);
        changes.Add(InsertChunk(memoryNode, newVirtualChunk));
    }

    private void DeleteInMiddleOfChunk(
        ChangeCollection changes,
        LinkedListNode<IChunk> node,
        long relativeOffset,
        long deleteLength)
    {
        var newVirtualChunk = node.Value.Clone();
        newVirtualChunk.SourceOffset += relativeOffset + deleteLength;
        newVirtualChunk.Length -= relativeOffset + deleteLength;

        changes.Add(InsertChunk(node, newVirtualChunk));
        var memoryNode = node.Next;
        if (memoryNode is null)
        {
            throw new InvalidOperationException("The next node should not be null.");
        }

        RemoveBefore(changes, memoryNode, node.Value.Length - relativeOffset);
    }

    protected void Init(IChunk chunk)
    {
        Length = chunk.Length;
        OriginalLength = chunk.Length;
        Chunks.AddFirst(chunk);
    }

    private void WriteCreateNewMemoryChunk(
        LinkedListNode<IChunk> node,
        long relativeOffset,
        byte[] writeBuffer,
        ChangeCollection changes)
    {
        changes.SetStartAtPrevious();

        var newChunk = new MemoryChunk(this, writeBuffer);
        changes.Add(InsertChunk(node, newChunk));

        var memoryNode = node.Next;
        if (memoryNode is null)
        {
            throw new InvalidOperationException("Next node is not supposed to be null.");
        }

        var newVirtualLength = node.Value.Length - (relativeOffset + writeBuffer.Length);
        RemoveBefore(changes, memoryNode, node.Value.Length - relativeOffset);

        if (newVirtualLength <= 0)
        {
            RemoveAfter(changes, memoryNode, Math.Abs(newVirtualLength));
        }
        else
        {
            var newVirtualChunk = node.Value.Clone();
            newVirtualChunk.SourceOffset += relativeOffset + writeBuffer.Length;
            newVirtualChunk.Length = newVirtualLength;
            changes.Add(InsertChunk(memoryNode, newVirtualChunk));
        }
    }

    private void WriteToMemoryChunk(ChangeCollection changes, LinkedListNode<IChunk> node,
        long writeOffset,
        byte[] writeBuffer,
        MemoryChunk chunk)
    {
        var modification = new WriteToMemoryChange(writeOffset, writeBuffer);
        modification.Apply(this, node, chunk);

        changes.Add(modification);

        if (modification.Data.Direction is GrowDirection.Start or GrowDirection.Both && node.Previous != null)
        {
            RemoveBefore(changes, node, modification.Data.GrowStart);
        }

        if (modification.Data.Direction is GrowDirection.End or GrowDirection.Both)
        {
            RemoveAfter(changes, node, modification.Data.GrowEnd);
        }
    }

    private void RemoveBefore(ChangeCollection changes, LinkedListNode<IChunk> node, long removeLength)
    {
        if (node.Previous == null)
        {
            return;
        }

        changes.SetStartAtPrevious();

        var previous = node.Previous;
        RemoveFromChunk(changes, previous, previous.Value.Length - removeLength, removeLength, true);
    }

    private void RemoveAfter(ChangeCollection changes, LinkedListNode<IChunk> node, long removeLength)
    {
        if (node.Next == null)
        {
            return;
        }

        long removedBytes = 0;
        var removeNode = node.Next;
        while (removeNode != null && removedBytes < removeLength)
        {
            var nextNode = removeNode.Next;
            var removableLength = removeNode.Value.Length;

            RemoveFromChunk(changes, removeNode, 0, removeLength - removedBytes);

            removedBytes += removableLength;
            removeNode = nextNode;
        }
    }

    private void RemoveFromChunk(ChangeCollection changes, LinkedListNode<IChunk> node, long offset,
        long length, bool prependChange = false)
    {
        if (length >= node.Value.Length)
        {
            changes.SetStartAtPrevious();
            changes.Add(new RemoveChunkChange(changes.Count is 0 && node.Previous is null).Apply(this, node));
            return;
        }

        IChange change = node.Value switch
        {
            ReadOnlyChunk virtualChunk => new RemoveFromVirtualChange(offset, length).Apply(this, node, virtualChunk),
            MemoryChunk memoryChunk => new RemoveFromMemoryChange(offset, length).Apply(this, node, memoryChunk),
            _ => throw new NotSupportedException($"Chunk {node.Value.GetType().Name} not supported for removing.")
        };

        changes.SetStartAtPrevious(offset is not 0);
        if (prependChange)
        {
            changes.Prepend(change);
        }
        else
        {
            changes.Add(change);
        }
    }

    private IChange InsertChunk(LinkedListNode<IChunk>? contextNode, IChunk newChunk, bool before = false) =>
        new InsertNewChunkChange(newChunk, before).Apply(this, contextNode);

    internal (LinkedListNode<IChunk>? node, long offset) GetNodeAt(long findOffset)
    {
        var offset = 0L;
        var node = Chunks.First;
        while (node != null)
        {
            var chunk = node.Value;
            if (offset <= findOffset && offset + chunk.Length > findOffset || node.Next == null)
            {
                return (node, offset);
            }

            offset += chunk.Length;
            node = node.Next;
        }

        return (null, 0);
    }

    public BufferStream GetStream() => new(this);

    public class FindResult
    {
        private readonly bool _backward;
        private readonly BaseBuffer _buffer;

        private readonly long _currentOffset;
        private readonly byte[] _pattern;
        private readonly bool _wrapAround;

        private bool _used;

        public FindResult(BaseBuffer buffer, byte[] pattern, bool backward, bool wrapAround, long currentOffset)
        {
            _buffer = buffer;
            _pattern = pattern;
            _backward = backward;
            _wrapAround = wrapAround;
            _currentOffset = currentOffset;

            _used = false;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<long> Next()
        {
            if (_used)
            {
                throw new InvalidOperationException(
                    $"Use the new {nameof(FindResult)} instance returned by 'Next()' or 'Previous()'.");
            }

            _used = true;
            var startOffset = _backward ? _currentOffset - _pattern.Length : _currentOffset + _pattern.Length;
            return _buffer.Find(_pattern, startOffset, _backward, _wrapAround);
        }

        public async Task<long> Previous()
        {
            if (_used)
            {
                throw new InvalidOperationException(
                    $"Use the new {nameof(FindResult)} instance returned by 'Next()' or 'Previous()'.");
            }

            _used = true;
            var startOffset = _backward ? _currentOffset - _pattern.Length : _currentOffset + _pattern.Length;
            return _buffer.Find(_pattern, startOffset, _backward, _wrapAround);
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}

public readonly struct ModifiedRange
{
    public ModifiedRange(long startOffset, long endOffset)
    {
        StartOffset = startOffset;
        EndOffset = endOffset;
    }

    public long StartOffset { get; }
    public long EndOffset { get; }
    public long Length => EndOffset - StartOffset;
}