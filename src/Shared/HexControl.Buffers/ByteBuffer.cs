using System.Buffers;
using HexControl.Buffers.Chunks;
using HexControl.Buffers.Events;
using HexControl.Buffers.Find;
using HexControl.Buffers.History;
using HexControl.Buffers.History.Changes;
using HexControl.Buffers.Modifications;
using JetBrains.Annotations;

namespace HexControl.Buffers;

public static class SemaphoreSlimExtensions
{
    public static async Task<TReturn> RunAsync<TReturn>(this SemaphoreSlim semaphore, Func<Task<TReturn>> body)
    {
        await semaphore.WaitAsync();
        try
        {
            return await body();
        }
        finally
        {
            semaphore.Release();
        }
    }

    public static async Task<TResult> RunAsync<TResult>(this SemaphoreSlim semaphore, Func<TResult> body)
    {
        await semaphore.WaitAsync();
        try
        {
            return body();
        }
        finally
        {
            semaphore.Release();
        }
    }

    public static async Task RunAsync(this SemaphoreSlim semaphore, Action body)
    {
        await semaphore.WaitAsync();
        try
        {
            body();
        }
        finally
        {
            semaphore.Release();
        }
    }

    public static TReturn Run<TReturn>(this SemaphoreSlim semaphore, Func<TReturn> body)
    {
        semaphore.Wait();
        try
        {
            return body();
        }
        finally
        {
            semaphore.Release();
        }
    }

    public static void Run(this SemaphoreSlim semaphore, Action body)
    {
        semaphore.Wait();
        try
        {
            body();
        }
        finally
        {
            semaphore.Release();
        }
    }
}

[PublicAPI]
public abstract class ByteBuffer
{
    private readonly ChangeTracker _changes;
    private readonly SemaphoreSlim _lock;

    protected ByteBuffer(ChangeTracking changeTracking)
    {
        ChangeTracking = changeTracking;
        Chunks = new LinkedList<IChunk>();
        _changes = new ChangeTracker(this);
        _lock = new SemaphoreSlim(1, 1);
    }

    internal LinkedList<IChunk> Chunks { get; }

    public int Version { get; private set; }

    public bool IsModified => Version > 0;

    public long Length { get; set; }

    public bool IsReadOnly { get; protected set; }

    public long OriginalLength { get; private set; } = -1;

    public ChangeTracking ChangeTracking { get; }

    public bool CanUndo => _changes.CanUndo;
    public bool CanRedo => _changes.CanRedo;

    public event EventHandler<LengthChangedEventArgs>? LengthChanged;
    public event EventHandler<ModifiedEventArgs>? Modified;
    public event EventHandler<EventArgs>? Saved;

    public Task<long> ReadAsync(
        long offset,
        byte[] buffer,
        List<ModifiedRange>? modifications = null,
        CancellationToken cancellationToken = default) =>
        ReadAsync(buffer, offset, buffer.LongLength, modifications, cancellationToken);

    public async Task<long> ReadAsync(byte[] buffer,
        long offset,
        long count,
        List<ModifiedRange>? modifications = null,
        CancellationToken cancellationToken = default)
    {
        return await _lock.RunAsync(async () => await InternalReadAsync(buffer, offset, count, modifications, cancellationToken));
    }

    private async Task<long> InternalReadAsync(byte[] buffer,
        long offset,
        long count,
        List<ModifiedRange>? modifications = null,
        CancellationToken cancellationToken = default)
    {
        if (offset > Length)
        {
            return 0;
        }

        var (node, currentOffset) = GetNodeAt(offset);

        // Shortcut for in case all data can be read from the current node
        if (node is not null && offset + count < currentOffset + node.Value.Length)
        {
            if (node.Value is MemoryChunk)
            {
                modifications?.Add(new ModifiedRange(offset, count));
            }

            return await node.Value.ReadAsync(buffer, offset - currentOffset, count, cancellationToken);
        }

        var actualRead = 0L;
        var modificationStart = -1L;
        while (node != null && count - actualRead > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chunk = node.Value;
            var isLastChunk = node.Next is null || chunk.Length >= count - actualRead;

            if (chunk is MemoryChunk && modificationStart == -1)
            {
                modificationStart = currentOffset;
            }

            if ((chunk is not MemoryChunk || isLastChunk) && modificationStart != -1)
            {
                modifications?.Add(new ModifiedRange(modificationStart, currentOffset));
                modificationStart = -1;
            }

            var relativeOffset = offset + actualRead - currentOffset;
            var length = Math.Min(count - actualRead, chunk.Length - relativeOffset);
            var readBuffer = ArrayPool<byte>.Shared.Rent((int)length);
            var bufferLength = await chunk.ReadAsync(readBuffer, relativeOffset, length, cancellationToken);
            Array.Copy(readBuffer, 0, buffer, actualRead, bufferLength);
            ArrayPool<byte>.Shared.Return(readBuffer);

            actualRead += bufferLength;
            currentOffset += chunk.Length;
            node = node.Next;
        }

        return actualRead;
    }

    public async Task<byte[]> ReadAsync(long offset, long readLength, List<ModifiedRange>? modifiedRanges = null,
        CancellationToken cancellationToken = default)
    {
        var buffer = new byte[readLength];
        await ReadAsync(offset, buffer, modifiedRanges, cancellationToken);
        return buffer;
    }

    public async Task WriteAsync(long writeOffset, byte value)
    {
        await WriteAsync(writeOffset, new[] { value });
    }

    public async Task WriteAsync(long writeOffset, byte[] writeBytes)
    {
        await _lock.RunAsync(() => InternalWrite(writeOffset, writeBytes));
    }

    public void Write(long writeOffset, byte value)
    {
        Write(writeOffset, new[] { value });
    }

    public void Write(long writeOffset, byte[] writeBytes)
    {
        _lock.Run(() => InternalWrite(writeOffset, writeBytes));
    }

    // TODO: fill gaps with zeros, e.g. writing at startOffset 300 when document is completely empty, or writing past the maxLength.
    private void InternalWrite(long writeOffset, byte[] writeBytes)
    {
        GuardAgainstReadOnly();

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

    // TODO: insert beyond the document Length, fill gap with 0's, and insert that buffer at 'Length'
    public async Task InsertAsync(long insertOffset, byte insertByte)
    {
        await InsertAsync(insertOffset, new[] { insertByte });
    }

    public async Task InsertAsync(long insertOffset, byte[] insertBytes)
    {
        await _lock.RunAsync(() => InternalInsert(insertOffset, insertBytes));
    }

    // TODO: insert beyond the document Length, fill gap with 0's, and insert that buffer at 'Length'
    public void Insert(long insertOffset, byte insertByte)
    {
        Insert(insertOffset, new[] { insertByte });
    }

    public void Insert(long insertOffset, byte[] insertBytes)
    {
        _lock.Run(() => InternalInsert(insertOffset, insertBytes));
    }

    private void InternalInsert(long insertOffset, byte[] insertBytes)
    {
        GuardAgainstReadOnly();

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

    public async Task DeleteAsync(long deleteOffset, long deleteLength)
    {
        await _lock.RunAsync(() => InternalDelete(deleteOffset, deleteLength));
    }

    public void Delete(long deleteOffset, long deleteLength)
    {
        _lock.Run(() => InternalDelete(deleteOffset, deleteLength));
    }

    private void InternalDelete(long deleteOffset, long deleteLength)
    {
        GuardAgainstReadOnly();

        var oldLength = Length;
        var (node, currentOffset) = GetNodeAt(deleteOffset);

        var changes = ChangeCollection.Delete(deleteOffset, deleteLength);

        if (node?.Value is IImmutableChunk && deleteOffset - currentOffset + deleteLength < node.Value.Length &&
            deleteOffset - currentOffset is not 0)
        {
            DeleteInMiddleOfChunk(changes, node, deleteOffset - currentOffset, deleteLength);
            PushChanges(changes, oldLength);
            return;
        }

        var bytesToDelete = deleteLength;
        while (bytesToDelete > 0 && node is not null)
        {
            var nextNode = node.Next;
            var chunk = node.Value!;
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

    public async Task UndoAsync()
    {
        var modification = await _lock.RunAsync(() => _changes.Undo());
        OnModified(modification, ModificationSource.Undo);
    }

    public async Task RedoAsync()
    {
        var modification = await _lock.RunAsync(() => _changes.Redo());
        OnModified(modification, ModificationSource.Redo);
    }

    public void Undo()
    {
        var modification = _lock.Run(() => _changes.Undo());
        OnModified(modification, ModificationSource.Undo);
    }

    public void Redo()
    {
        var modification = _lock.Run(() => _changes.Redo());
        OnModified(modification, ModificationSource.Redo);
    }

    public long Read(byte[] buffer,
        long offset,
        List<ModifiedRange>? modifications = null) =>
        Read(buffer, offset, buffer.LongLength, modifications);

    public long Read(byte[] buffer,
        long offset,
        long count,
        List<ModifiedRange>? modifications = null)
    {
        return _lock.Run(() => InternalRead(buffer, offset, count, modifications));
    }

    private long InternalRead(byte[] buffer,
        long offset,
        long count,
        List<ModifiedRange>? modifications = null)
    {
        if (offset > Length)
        {
            return 0;
        }

        var (node, currentOffset) = GetNodeAt(offset);

        // Shortcut for in case all data can be read from the current node
        if (node is not null && offset + count < currentOffset + node.Value.Length)
        {
            if (node.Value is MemoryChunk)
            {
                modifications?.Add(new ModifiedRange(offset, count));
            }

            return node.Value.Read(buffer, offset - currentOffset, count);
        }

        var actualRead = 0L;
        var modificationStart = -1L;
        while (node != null && count - actualRead > 0)
        {
            var chunk = node.Value;

            var isLastChunk = node.Next is null || chunk.Length >= count - actualRead;
            if (chunk is MemoryChunk && modificationStart is -1)
            {
                modificationStart = currentOffset;
            }

            if ((chunk is not MemoryChunk || isLastChunk) && modificationStart is not -1)
            {
                modifications?.Add(new ModifiedRange(modificationStart, node.Next is null ? Length : currentOffset));
                modificationStart = -1;
            }

            var relativeOffset = offset + actualRead - currentOffset;
            var length = Math.Min(count - actualRead, chunk.Length - relativeOffset);

            var readBuffer = ArrayPool<byte>.Shared.Rent((int)length);
            var bufferLength = chunk.Read(readBuffer, relativeOffset, length);
            Array.Copy(readBuffer, 0, buffer, actualRead, bufferLength);
            ArrayPool<byte>.Shared.Return(readBuffer);

            actualRead += bufferLength;
            currentOffset += chunk.Length;
            node = node.Next;
        }

        return actualRead;
    }

    public long Find(long startOffset, bool backward, byte[] pattern,
        CancellationToken cancellationToken = default)
    {
        return Find(startOffset, null, backward, pattern, cancellationToken);
    }

    public long Find(long startOffset, long? maxLength, bool backward, byte[] pattern,
        CancellationToken cancellationToken = default)
    {
        var strategy = new KmpFindStrategy(pattern);
        
        var currentStartOffset = startOffset;
        var didWrap = false;

        var remainingMaxLength = maxLength ?? long.MaxValue;

        while (true)
        {
            var findOffset = currentStartOffset;
            long findLength;
            if (backward)
            {
                findLength = didWrap
                    ? currentStartOffset - startOffset + (pattern.Length - 1)
                    : startOffset - (startOffset - currentStartOffset) + 1;
            }
            else
            {
                findLength = didWrap
                    ? startOffset - findOffset + pattern.Length
                    : Length - findOffset;
            }

            if (maxLength is not null)
            {
                findLength = Math.Min(findLength, remainingMaxLength);
                remainingMaxLength -= findLength;
            }

            // Find without overhead when the buffer is not modified.
            var foundOffset = IsModified
                ? FindInMemory(strategy, findOffset, findLength, backward, cancellationToken)
                : FindInImmutable(strategy, findOffset, findLength, backward, cancellationToken);
            if (foundOffset is not -1)
            {
                return foundOffset;
            }

            if (!didWrap)
            {
                didWrap = true;
                currentStartOffset = backward ? Length : 0;

            }
            else
            {
                return -1;
            }
        }
    }

    private long FindInMemory(IFindStrategy strategy, long offset, long length, bool backward,
        CancellationToken cancellationToken) =>
        strategy.FindInBuffer(this, offset, length, backward, cancellationToken);

    protected abstract long FindInImmutable(IFindStrategy strategy, long offset, long length, bool backward,
        CancellationToken cancellationToken);

    protected abstract IChunk CreateDefaultChunk();

    private void PushChanges(ChangeCollection changes, long oldLength)
    {
        Version++;

        _changes.Push(changes);
        OnModified(changes.Modification, ModificationSource.User);

        if (oldLength != Length)
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

    protected virtual void OnSaved()
    {
        Saved?.Invoke(this, EventArgs.Empty);
    }

    private void InsertInMiddleOfChunk(
        LinkedListNode<IChunk> node,
        long relativeOffset,
        IChunk chunk,
        in ChangeCollection changes)
    {
        var newImmutableChunk = node.Value.Clone();
        newImmutableChunk.SourceOffset += relativeOffset;
        newImmutableChunk.Length -= relativeOffset;

        changes.Add(InsertChunk(node, chunk));
        var memoryNode = node.Next;
        if (memoryNode is null)
        {
            throw new InvalidOperationException("Expected inserted node to be present.");
        }

        RemoveBefore(changes, memoryNode, node.Value.Length - relativeOffset);
        changes.Add(InsertChunk(memoryNode, newImmutableChunk));
    }

    private void DeleteInMiddleOfChunk(
        ChangeCollection changes,
        LinkedListNode<IChunk> node,
        long relativeOffset,
        long deleteLength)
    {
        var newImmutableChunk = node.Value.Clone();
        newImmutableChunk.SourceOffset += relativeOffset + deleteLength;
        newImmutableChunk.Length -= relativeOffset + deleteLength;

        changes.Add(InsertChunk(node, newImmutableChunk));
        var memoryNode = node.Next;
        if (memoryNode is null)
        {
            throw new InvalidOperationException("The next node should not be null.");
        }

        RemoveBefore(changes, memoryNode, node.Value.Length - relativeOffset);
    }

    protected void Initialize(IChunk chunk)
    {
        // Reset
        Chunks.Clear();
        _changes.Clear();
        Version = 0;

        // Initialize
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

        var newImmutableLength = node.Value.Length - (relativeOffset + writeBuffer.Length);
        RemoveBefore(changes, memoryNode, node.Value.Length - relativeOffset);

        if (newImmutableLength <= 0)
        {
            RemoveAfter(changes, memoryNode, Math.Abs(newImmutableLength));
        }
        else
        {
            var newImmutableChunk = node.Value.Clone();
            newImmutableChunk.SourceOffset += relativeOffset + writeBuffer.Length;
            newImmutableChunk.Length = newImmutableLength;
            changes.Add(InsertChunk(memoryNode, newImmutableChunk));
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
            IImmutableChunk immutableChunk => new RemoveFromImmutableChange(offset, length).Apply(this, node,
                immutableChunk),
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

    public ByteBufferStream AsStream() => new(this);

    public async Task<bool> SaveAsync(CancellationToken cancellationToken = default)
    {
        GuardAgainstReadOnly();

        if (!IsModified)
        {
            return false;
        }

        var result = await SaveInternalAsync(cancellationToken);
        if (!result)
        {
            return false;
        }

        // Reset buffer after saving
        Initialize(CreateDefaultChunk());
        OnSaved();

        return true;
    }

    protected abstract Task<bool> SaveInternalAsync(CancellationToken cancellationToken);

    public async Task<bool> SaveToFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        await using var stream = File.Open(fileName, FileMode.OpenOrCreate);
        return await SaveToFileAsync(stream, cancellationToken);
    }

    public async Task<bool> SaveToFileAsync(FileStream fileStream, CancellationToken cancellationToken = default)
    {
        const int flushSize = 256 * 1024 * 1024;
        const int chunkSize = 32 * 1024;

        var readBuffer = new byte[chunkSize];

        fileStream.SetLength(Length);

        foreach (var chunk in Chunks)
        {
            if (chunk is MemoryChunk memory)
            {
                await fileStream.WriteAsync(memory.Bytes, cancellationToken);
            }
            else
            {
                var notFlushedSize = 0L;
                var currentOffset = 0L;
                while (true)
                {
                    var bytesRead = await chunk.ReadAsync(readBuffer, currentOffset, chunkSize, cancellationToken);
                    await fileStream.WriteAsync(readBuffer.AsMemory(0, (int)bytesRead), cancellationToken);

                    if (bytesRead != chunkSize)
                    {
                        break;
                    }

                    notFlushedSize += bytesRead;

                    if (notFlushedSize >= flushSize)
                    {
                        await fileStream.FlushAsync(cancellationToken);
                        notFlushedSize = 0;
                    }

                    currentOffset += bytesRead;
                }

                await fileStream.FlushAsync(cancellationToken);
            }

            await fileStream.FlushAsync(cancellationToken);
        }

        return true;
    }

    /// <summary>
    ///     Check whether the structure of the buffer might have changed. This implies verifying if any insert of delete
    ///     modifications have been made.
    /// </summary>
    public bool HasStructureChanged()
    {
        return IsModified &&
               _changes.UndoStack.Any(entry => entry.Modification is InsertModification or DeleteModification);
    }

    private void GuardAgainstReadOnly()
    {
        if (IsReadOnly)
        {
            throw new InvalidOperationException("Modifications are not permitted, buffer is opened in readonly mode.");
        }
    }
}