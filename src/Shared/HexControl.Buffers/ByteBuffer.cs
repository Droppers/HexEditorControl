using HexControl.Buffers.Chunks;
using HexControl.Buffers.Events;
using HexControl.Buffers.Find;
using HexControl.Buffers.Helpers;
using HexControl.Buffers.History;
using HexControl.Buffers.History.Changes;
using HexControl.Buffers.Modifications;
using JetBrains.Annotations;

namespace HexControl.Buffers;

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

    public bool Locked { get; set; }

    public event EventHandler<LengthChangedEventArgs>? LengthChanged;
    public event EventHandler<ModifiedEventArgs>? Modified;
    public event EventHandler<EventArgs>? Saved;

    public async ValueTask<long> ReadAsync(Memory<byte> buffer,
        long offset,
        List<ModifiedRange>? modifications = null,
        CancellationToken cancellationToken = default)
    {
        using var _ = await _lock.LockAsync();
        return await InternalReadAsync(buffer, offset, modifications, cancellationToken);
    }

    private async ValueTask<long> InternalReadAsync(
        Memory<byte> buffer,
        long offset,
        List<ModifiedRange>? modifications = null,
        CancellationToken cancellationToken = default)
    {
        if (offset > Length)
        {
            return 0;
        }

        var (node, currentOffset) = GetNodeAt(offset);

        // Shortcut for in case all data can be read from the current node
        if (node is not null && offset + buffer.Length < currentOffset + node.Value.Length)
        {
            if (node.Value is MemoryChunk)
            {
                modifications?.Add(new ModifiedRange(offset, buffer.Length));
            }

            return await node.Value.ReadAsync(buffer, offset - currentOffset, cancellationToken);
        }

        var actualRead = 0L;
        var modificationStart = -1L;
        while (node != null && buffer.Length - actualRead > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chunk = node.Value;
            var chunkLength = currentOffset < offset ? chunk.Length - (offset - currentOffset) : chunk.Length;
            var isLastChunk = node.Next is null || chunkLength >= buffer.Length - actualRead;

            if (chunk is MemoryChunk && modificationStart == -1)
            {
                modificationStart = currentOffset;
            }

            if ((chunk is not MemoryChunk || isLastChunk) && modificationStart != -1)
            {
                modifications?.Add(new ModifiedRange(modificationStart, modificationStart == currentOffset ? currentOffset + chunk.Length : currentOffset));
                modificationStart = -1;
            }

            var relativeOffset = offset + actualRead - currentOffset;
            var length = Math.Min(buffer.Length - actualRead, chunk.Length - relativeOffset);

            await chunk.ReadAsync(buffer.Slice((int)actualRead, (int)length), relativeOffset, cancellationToken);

            actualRead += length;
            currentOffset += chunk.Length;
            node = node.Next;
        }

        return actualRead;
    }
    
    public async Task WriteAsync(long offset, byte value)
    {
        await WriteAsync(offset, new[] { value });
    }

    public async Task WriteAsync(long offset, byte[] bytes)
    {
        using var _ = await _lock.LockAsync();
        InternalWrite(offset, bytes);
    }

    public void Write(long offset, byte value)
    {
        Write(offset, new[] { value });
    }

    public void Write(long offset, byte[] bytes)
    {
        using var _ = _lock.Lock();
        InternalWrite(offset, bytes);
    }

    // TODO: fill gaps with zeros, e.g. writing at startOffset 300 when document is completely empty, or writing past the maxLength.
    private void InternalWrite(long offset, byte[] bytes)
    {
        GuardAgainstInvalidState();

        var oldLength = Length;
        var changes = ChangeCollection.Write(offset, bytes);

        var (node, currentOffset) = GetNodeAt(offset);

        if (node == null)
        {
            changes.Add(InsertChunk(null, new MemoryChunk(this, bytes)));
            PushChanges(changes, oldLength);
            return;
        }

        var relativeOffset = offset - currentOffset;
        var chunk = node.Value;
        if (chunk is MemoryChunk memoryChunk)
        {
            WriteToMemoryChunk(changes, node, relativeOffset, bytes, memoryChunk);
        }
        else if (offset == currentOffset && node.Previous?.Value is MemoryChunk previousMemoryChunk)
        {
            changes.SetStartAtPrevious();
            WriteToMemoryChunk(changes, node.Previous, previousMemoryChunk.Length, bytes, previousMemoryChunk);
        }
        else if (offset + bytes.Length > currentOffset + chunk.Length &&
                 node.Next?.Value is MemoryChunk nextMemoryChunk)
        {
            WriteToMemoryChunk(changes, node.Next, -(currentOffset + chunk.Length - offset), bytes,
                nextMemoryChunk);
        }
        else if (offset == currentOffset && node.Previous is null)
        {
            changes.Add(InsertChunk(node, new MemoryChunk(this, bytes), true));
            if (node.Previous is null)
            {
                throw new InvalidOperationException("Previous change should have created a new previous node.");
            }

            RemoveAfter(changes, node.Previous, bytes.Length);
        }
        else
        {
            WriteCreateNewMemoryChunk(node, relativeOffset, bytes, changes);
        }

        PushChanges(changes, oldLength);
    }

    // TODO: insert beyond the document Length, fill gap with 0's, and insert that buffer at 'Length'
    public async Task InsertAsync(long offset, byte @byte)
    {
        await InsertAsync(offset, new[] { @byte });
    }

    public async Task InsertAsync(long offset, byte[] bytes)
    {

        using var _ = await _lock.LockAsync();
        InternalInsert(offset, bytes);
    }

    // TODO: insert beyond the document Length, fill gap with 0's, and insert that buffer at 'Length'
    public void Insert(long offset, byte @byte)
    {
        Insert(offset, new[] { @byte });
    }

    public void Insert(long offset, byte[] bytes)
    {

        using var _ = _lock.Lock();
        InternalInsert(offset, bytes);
    }

    private void InternalInsert(long offset, byte[] bytes)
    {
        GuardAgainstInvalidState();

        var oldLength = Length;

        var (node, currentOffset) = GetNodeAt(offset);
        if (node == null)
        {
            return;
        }

        var changes = ChangeCollection.Insert(offset, bytes);

        var relativeOffset = offset - currentOffset;
        var chunk = node.Value;
        if (chunk is MemoryChunk memoryChunk)
        {
            changes.Add(new InsertToMemoryChange(relativeOffset, bytes).Apply(this, node, memoryChunk));
        }
        else if (currentOffset == offset && node.Previous?.Value is MemoryChunk previousChunk)
        {
            changes.SetStartAtPrevious();
            changes.Add(new InsertToMemoryChange(relativeOffset + previousChunk.Length, bytes).Apply(this, node, previousChunk));
        }
        else if (offset == 0)
        {
            changes.Add(InsertChunk(node, new MemoryChunk(this, bytes), true));
        }
        else if (offset == Length)
        {
            changes.Add(InsertChunk(node, new MemoryChunk(this, bytes)));
        }
        else
        {
            InsertInMiddleOfChunk(node, relativeOffset, new MemoryChunk(this, bytes), changes);
        }

        PushChanges(changes, oldLength);
    }

    public async Task DeleteAsync(long offset, long length)
    {
        using var _ = await _lock.LockAsync();
        InternalDelete(offset, length);
    }

    public void Delete(long offset, long length)
    {

        using var _ = _lock.Lock();
        InternalDelete(offset, length);
    }

    private void InternalDelete(long offset, long length)
    {
        GuardAgainstInvalidState();

        var oldLength = Length;
        var (node, currentOffset) = GetNodeAt(offset);

        var changes = ChangeCollection.Delete(offset, length);

        if (node?.Value is IImmutableChunk && offset - currentOffset + length < node.Value.Length &&
            offset - currentOffset is not 0)
        {
            DeleteInMiddleOfChunk(changes, node, offset - currentOffset, length);
            PushChanges(changes, oldLength);
            return;
        }

        var bytesToDelete = length;
        while (bytesToDelete > 0 && node is not null)
        {
            var nextNode = node.Next;
            var chunk = node.Value!;
            var chunkLength = chunk.Length;

            var relativeOffset = offset + (length - bytesToDelete) - currentOffset;
            var deleteLength = Math.Min(bytesToDelete, chunk.Length - relativeOffset);
            RemoveFromChunk(changes, node, relativeOffset, deleteLength);

            if (Chunks.First == null)
            {
                changes.Add(InsertChunk(null, new MemoryChunk(this, Array.Empty<byte>())));
            }

            bytesToDelete -= deleteLength;
            currentOffset += chunkLength;
            node = nextNode;
        }

        PushChanges(changes, oldLength);
    }

    public async Task UndoAsync()
    {
        using var _ = await _lock.LockAsync();
        var modification = _changes.Undo();
        OnModified(modification, ModificationSource.Undo);
    }

    public async Task RedoAsync()
    {
        using var _ = await _lock.LockAsync();
        var modification = _changes.Redo();
        OnModified(modification, ModificationSource.Redo);
    }

    public void Undo()
    {
        using var _ = _lock.Lock();
        var modification = _changes.Undo();
        OnModified(modification, ModificationSource.Undo);
    }

    public void Redo()
    {
        using var _ = _lock.Lock();
        var modification = _changes.Redo();
        OnModified(modification, ModificationSource.Redo);
    }
    
    public long Read(Span<byte> buffer,
        long offset,
        List<ModifiedRange>? modifications = null)
    {
        using var _ = _lock.Lock();
        return InternalRead(buffer, offset, modifications);
    }

    private long InternalRead(Span<byte> buffer,
        long offset,
        List<ModifiedRange>? modifications = null)
    {
        if (offset > Length)
        {
            return 0;
        }

        var (node, currentOffset) = GetNodeAt(offset);

        // Shortcut for in case all data can be read from the current node
        if (node is not null && offset + buffer.Length < currentOffset + node.Value.Length)
        {
            if (node.Value is MemoryChunk)
            {
                modifications?.Add(new ModifiedRange(offset, buffer.Length));
            }

            return node.Value.Read(buffer[..buffer.Length], offset - currentOffset);
        }

        var actualRead = 0L;
        var modificationStart = -1L;
        while (node != null && buffer.Length - actualRead > 0)
        {
            var chunk = node.Value;

            var isLastChunk = node.Next is null || chunk.Length >= buffer.Length - actualRead;
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
            var length = Math.Min(buffer.Length - actualRead, chunk.Length - relativeOffset);

            var bytesRead = chunk.Read(buffer.Slice((int)actualRead, (int)length), relativeOffset);

            actualRead += bytesRead;
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
        if (node.Value.Clone() is not IImmutableChunk newImmutableChunk)
        {
            throw new InvalidOperationException("Expected new chunk to be immutable.");
        }

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
        if (node.Value.Clone() is not IImmutableChunk newImmutableChunk)
        {
            throw new InvalidOperationException("Expected new chunk to be immutable.");
        }

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
        byte[] bytes,
        ChangeCollection changes)
    {
        changes.SetStartAtPrevious();

        var newChunk = new MemoryChunk(this, bytes);
        changes.Add(InsertChunk(node, newChunk));

        var memoryNode = node.Next;
        if (memoryNode is null)
        {
            throw new InvalidOperationException("Next node is not supposed to be null.");
        }

        var newImmutableLength = node.Value.Length - (relativeOffset + bytes.Length);
        RemoveBefore(changes, memoryNode, node.Value.Length - relativeOffset);

        if (newImmutableLength <= 0)
        {
            RemoveAfter(changes, memoryNode, Math.Abs(newImmutableLength));
        }
        else
        {
            if (node.Value.Clone() is not IImmutableChunk newImmutableChunk)
            {
                throw new InvalidOperationException("Expected new chunk to be immutable.");
            }

            newImmutableChunk.SourceOffset += relativeOffset + bytes.Length;
            newImmutableChunk.Length = newImmutableLength;
            changes.Add(InsertChunk(memoryNode, newImmutableChunk));
        }
    }

    private void WriteToMemoryChunk(ChangeCollection changes, LinkedListNode<IChunk> node,
        long offset,
        byte[] bytes,
        MemoryChunk chunk)
    {
        var modification = new WriteToMemoryChange(offset, bytes);
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
        GuardAgainstInvalidState();

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
        Locked = true;
        try
        {
            await using var stream = File.Open(fileName, FileMode.OpenOrCreate);
            return await SaveToFileAsync(stream, cancellationToken);
        }
        finally
        {
            Locked = false;
        }
    }

    public async Task<bool> SaveToFileAsync(FileStream fileStream, CancellationToken cancellationToken = default)
    {
        Locked = true;
        try
        {
            return await InternalSaveToFileAsync(fileStream, cancellationToken);
        }
        finally
        {
            Locked = false;
        }
    }

    private async Task<bool> InternalSaveToFileAsync(FileStream fileStream, CancellationToken cancellationToken = default)
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
                    var readLength = Math.Min(chunk.Length, readBuffer.Length);
                    var bytesRead = await chunk.ReadAsync(readBuffer.AsMemory(0, (int)readLength), currentOffset, cancellationToken);
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

    private void GuardAgainstInvalidState()
    {
        if (Locked)
        {
            throw new InvalidOperationException("Modifications are not permitted, buffer is currently locked due to other operations.");
        }

        if (IsReadOnly)
        {
            throw new InvalidOperationException("Modifications are not permitted, buffer is opened in readonly mode.");
        }
    }
}