using System.Diagnostics;
using System.Runtime.CompilerServices;
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
    private readonly ChangeTracker _changeTracker;
    private readonly AsyncReaderWriterLock _lock;

    protected ByteBuffer(ChangeTracking changeTracking)
    {
        Chunks = new LinkedList<IChunk>();
        _changeTracker = new ChangeTracker(this, changeTracking);
        _lock = new AsyncReaderWriterLock();
    }

    internal LinkedList<IChunk> Chunks { get; }

    public int Version { get; private set; }

    public bool IsModified => Version > 0;

    public long Length { get; set; }

    public bool IsReadOnly { get; protected set; }

    public long OriginalLength { get; private set; } = -1;

    public ChangeTracking ChangeTracking
    {
        get => _changeTracker.ChangeTracking;
        set => _changeTracker.ChangeTracking = value;
    }

    public bool CanUndo => _changeTracker.CanUndo;
    public bool CanRedo => _changeTracker.CanRedo;

    public bool Locked { get; set; }

    public event EventHandler<LengthChangedEventArgs>? LengthChanged;
    public event EventHandler<ModifiedEventArgs>? Modified;
    public event EventHandler<EventArgs>? Saved;

    public async Task ReplaceAsync(long offset, long length, byte @byte, CancellationToken cancellationToken = default)
    {
        using var _ = await _lock.AcquireWriterLockAsync(cancellationToken);
        InternalReplace(offset, length, new[] {@byte});
    }

    public async Task ReplaceAsync(long offset, long length, byte[] bytes, CancellationToken cancellationToken = default)
    {
        using var _ = await _lock.AcquireWriterLockAsync(cancellationToken);
        InternalReplace(offset, length, bytes);
    }

    public void Replace(long offset, long length, byte @byte)
    {
        using var _ = _lock.AcquireWriterLock();
        InternalReplace(offset, length, new[] {@byte});
    }

    public void Replace(long offset, long length, byte[] bytes)
    {
        using var _ = _lock.AcquireWriterLock();
        InternalReplace(offset, length, bytes);
    }

    private void InternalReplace(long offset, long length, byte[] bytes)
    {
        GroupChanges(() =>
        {
            InternalDelete(offset, length);
            InternalInsert(offset, bytes);
        });
    }

    public async Task WriteAsync(long offset, byte value, CancellationToken cancellationToken = default)
    {
        await WriteAsync(offset, new[] { value }, cancellationToken);
    }

    public async Task WriteAsync(long offset, byte[] bytes, CancellationToken cancellationToken = default)
    {
        GuardAgainstInvalidState();
        GuardAgainstInvalidOffsetAndLength(offset, bytes.Length);

        using var _ = await _lock.AcquireWriterLockAsync(cancellationToken);
        InternalWrite(offset, bytes);
    }

    public void Write(long offset, byte value)
    {
        Write(offset, new[] { value });
    }

    public void Write(long offset, byte[] bytes)
    {
        GuardAgainstInvalidState();
        GuardAgainstInvalidOffsetAndLength(offset, bytes.Length);

        using var _ = _lock.AcquireWriterLock();
        InternalWrite(offset, bytes);
    }
    
    private void InternalWrite(long offset, byte[] bytes)
    {
        using var scope = ChangeScope.Write(this, offset, bytes);

        var (node, currentOffset) = GetNodeAt(offset);

        if (node is null)
        {
            scope.Changes.Add(InsertChunk(null, new MemoryChunk(this, bytes)));
            return;
        }

        var relativeOffset = offset - currentOffset;
        var chunk = node.Value;
        if (chunk is MemoryChunk memoryChunk)
        {
            WriteToMemoryChunk(scope.Changes, node, relativeOffset, bytes, memoryChunk);
        }
        else if (offset == currentOffset && node.Previous?.Value is MemoryChunk previousMemoryChunk)
        {
            scope.Changes.SetStartAtPrevious();
            WriteToMemoryChunk(scope.Changes, node.Previous, previousMemoryChunk.Length, bytes, previousMemoryChunk);
        }
        else if (offset + bytes.Length >= currentOffset + chunk.Length &&
                 node.Next?.Value is MemoryChunk nextMemoryChunk)
        {
            WriteToMemoryChunk(scope.Changes, node.Next, -(currentOffset + chunk.Length - offset), bytes,
                nextMemoryChunk);
        }
        else if (offset == currentOffset && node.Previous is null)
        {
            scope.Changes.Add(InsertChunk(node, new MemoryChunk(this, bytes), true));
            if (node.Previous is null)
            {
                throw new InvalidOperationException("Previous change should have created a new previous node.");
            }

            RemoveAfter(scope.Changes, node.Previous, bytes.Length);
        }
        else
        {
            WriteCreateNewMemoryChunk(node, relativeOffset, bytes, scope.Changes);
        }
    }
    
    public async Task InsertAsync(long offset, byte @byte, CancellationToken cancellationToken = default)
    {
        await InsertAsync(offset, new[] { @byte }, cancellationToken);
    }

    public async Task InsertAsync(long offset, byte[] bytes, CancellationToken cancellationToken = default)
    {
        GuardAgainstInvalidState();
        GuardAgainstInvalidOffset(offset);

        using var _ = await _lock.AcquireWriterLockAsync(cancellationToken);
        InternalInsert(offset, bytes);
    }
    
    public void Insert(long offset, byte @byte)
    {
        Insert(offset, new[] { @byte });
    }

    public void Insert(long offset, byte[] bytes)
    {
        GuardAgainstInvalidState();
        GuardAgainstInvalidOffset(offset);

        using var _ = _lock.AcquireWriterLock();
        InternalInsert(offset, bytes);
    }

    private void InternalInsert(long offset, byte[] bytes)
    {
        var (node, currentOffset) = GetNodeAt(offset);
        if (node is null)
        {
            return;
        }

        using var scope = ChangeScope.Insert(this, offset, bytes);

        var relativeOffset = offset - currentOffset;
        var chunk = node.Value;
        if (chunk is MemoryChunk memoryChunk)
        { 
            scope.Changes.Add(new InsertToMemoryChange(relativeOffset, bytes).Apply(this, node, memoryChunk));
        }
        else if (currentOffset == offset && node.Previous?.Value is MemoryChunk previousChunk)
        {
            scope.Changes.SetStartAtPrevious();
            scope.Changes.Add(
                new InsertToMemoryChange(relativeOffset + previousChunk.Length, bytes)
                    .Apply(this, node, previousChunk));
        }
        else if (currentOffset == offset && node.Previous?.Value is IImmutableChunk)
        {
            scope.Changes.SetStartAtPrevious();
            scope.Changes.Add(InsertChunk(node.Previous, new MemoryChunk(this, bytes)));
        }
        else if (offset is 0)
        {
            scope.Changes.Add(InsertChunk(node, new MemoryChunk(this, bytes), true));
        }
        else if (offset == Length)
        {
            scope.Changes.Add(InsertChunk(node, new MemoryChunk(this, bytes)));
        }
        else
        {
            InsertInMiddleOfChunk(node, relativeOffset, new MemoryChunk(this, bytes), scope.Changes);
        }
    }

    public async Task DeleteAsync(long offset, long length, CancellationToken cancellationToken = default)
    {
        GuardAgainstInvalidState();
        GuardAgainstInvalidOffsetAndLength(offset, length);

        using var _ = await _lock.AcquireWriterLockAsync(cancellationToken);
        InternalDelete(offset, length);
    }

    public void Delete(long offset, long length)
    {
        GuardAgainstInvalidState();
        GuardAgainstInvalidOffsetAndLength(offset, length);

        using var _ = _lock.AcquireWriterLock();
        InternalDelete(offset, length);
    }

    private void InternalDelete(long offset, long length)
    {
        using var scope = ChangeScope.Delete(this, offset, length);

        var (node, currentOffset) = GetNodeAt(offset);
        
        if (node?.Value is IImmutableChunk && offset - currentOffset + length < node.Value.Length &&
            offset - currentOffset is not 0)
        {
            DeleteInMiddleOfChunk(scope.Changes, node, offset - currentOffset, length);
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
            RemoveFromChunk(scope.Changes, node, relativeOffset, deleteLength);

            if (Chunks.First is null)
            {
                scope.Changes.Add(InsertChunk(null, new MemoryChunk(this, Array.Empty<byte>())));
            }

            bytesToDelete -= deleteLength;
            currentOffset += chunkLength;
            node = nextNode;
        }
    }

    public async Task UndoAsync(CancellationToken cancellationToken = default)
    {
        using var _ = await _lock.AcquireWriterLockAsync(cancellationToken);
        InternalUndo();
    }

    public void Undo()
    {
        using var _ = _lock.AcquireWriterLock();
        InternalUndo();
    }

    private void InternalUndo()
    {
        if (!_changeTracker.CanUndo)
        {
            return;
        }

        var oldLength = Length;
        var modifications = _changeTracker.Undo();
        OnModified(modifications, ModificationSource.Undo);

        if (oldLength != Length)
        {
            OnLengthChanged(oldLength, Length);
        }
    }

    public async Task RedoAsync(CancellationToken cancellationToken = default)
    {
        using var _ = await _lock.AcquireWriterLockAsync(cancellationToken);
        InternalRedo();
    }

    public void Redo()
    {
        using var _ = _lock.AcquireWriterLock();
        InternalRedo();
    }

    private void InternalRedo()
    {
        if (!_changeTracker.CanRedo)
        {
            return;
        }

        var oldLength = Length;
        var modifications = _changeTracker.Redo();
        OnModified(modifications, ModificationSource.Redo);
        
        if (oldLength != Length)
        {
            OnLengthChanged(oldLength, Length);
        }
    }

    public long Read(Span<byte> buffer,
        long offset,
        List<ModifiedRange>? modifications = null)
    {
        using var _ = _lock.AcquireReaderLock();
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
                modifications?.Add(new ModifiedRange(offset, offset + buffer.Length));
            }

            return node.Value.Read(buffer[..buffer.Length], offset - currentOffset);
        }

        var actualRead = 0L;
        var modificationStart = -1L;
        while (node is not null && buffer.Length - actualRead > 0)
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

    public async ValueTask<long> ReadAsync(Memory<byte> buffer,
    long offset,
    List<ModifiedRange>? modifications = null,
    CancellationToken cancellationToken = default)
    {
        using var _ = await _lock.AcquireReaderLockAsync(cancellationToken);
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
                modifications?.Add(new ModifiedRange(offset, offset + buffer.Length));
            }
            
            return await node.Value.ReadAsync(buffer, offset - currentOffset, cancellationToken);
        }

        var actualRead = 0L;
        var modificationStart = -1L;
        while (node is not null && buffer.Length - actualRead > 0)
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

    public long Find(byte[] pattern, long offset, CancellationToken cancellationToken = default)
    {
        return Find(pattern, offset, null, default, cancellationToken);
    }
    
    public long Find(byte[] pattern, long offset, FindOptions options, CancellationToken cancellationToken = default)
    {
        return Find(pattern, offset, null, options, cancellationToken);
    }

    public long Find(byte[] pattern, long offset, long? length, CancellationToken cancellationToken = default)
    {
        return Find(pattern, offset, length, default, cancellationToken);
    }

    public long Find(byte[] pattern, long offset, long? length, FindOptions options,
        CancellationToken cancellationToken = default)
    {
        using var _ = _lock.AcquireReaderLock(cancellationToken);
        Locked = true;

        try
        {
            var strategy = new KmpFindStrategy(pattern);
            var currentStartOffset = offset;
            var didWrap = false;
            var remainingMaxLength = length ?? long.MaxValue;
            while (true)
            {
                var findOffset = currentStartOffset;
                long findLength;
                if (options.Backward)
                {
                    findLength = didWrap
                        ? currentStartOffset - offset + (pattern.Length - 1)
                        : offset - (offset - currentStartOffset) + 1;
                }
                else
                {
                    findLength = didWrap
                        ? offset - findOffset + pattern.Length
                        : Length - findOffset;
                }

                if (length is not null)
                {
                    findLength = Math.Min(findLength, remainingMaxLength);
                    remainingMaxLength -= findLength;
                }

                // Find without overhead when the buffer is not modified.
                var foundOffset = IsModified
                    ? FindInMemory(strategy, findOffset, findLength, options, cancellationToken)
                    : FindInImmutable(strategy, findOffset, findLength, options, cancellationToken);
                if (foundOffset is not -1)
                {
                    return foundOffset;
                }

                if (didWrap)
                {
                    return -1;
                }

                didWrap = true;
                currentStartOffset = options.Backward ? Length : 0;
            }
        }
        finally
        {
            Locked = false;
        }
    }

    private long FindInMemory(IFindStrategy strategy, long offset, long length, FindOptions options,
        CancellationToken cancellationToken) =>
        strategy.FindInBuffer(this, offset, length, options, cancellationToken);

    protected abstract long FindInImmutable(IFindStrategy strategy, long offset, long length, FindOptions options,
        CancellationToken cancellationToken);

    protected abstract IChunk CreateDefaultChunk();

    private void GroupChanges(Action action)
    {
        var oldLength = Length;
        _changeTracker.Start();

        action();

        var collections = _changeTracker.End();
        var modifications = collections.Select(c => c.Modification).ToArray();
        OnModified(modifications, ModificationSource.User);

        if (oldLength != Length)
        {
            OnLengthChanged(oldLength, Length);
        }
    }
    
    protected virtual void OnLengthChanged(long oldLength, long newLength)
    {
        LengthChanged?.Invoke(this, new LengthChangedEventArgs(oldLength, newLength));
    }

    protected virtual void OnModified(IReadOnlyList<BufferModification> modifications, ModificationSource source)
    {
        Modified?.Invoke(this, new ModifiedEventArgs(modifications, source));
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
        _changeTracker.Clear();
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
        var targetNode = relativeOffset is 0 ? node.Previous ?? node : node;

        changes.SetStartAtPrevious();

        var newChunk = new MemoryChunk(this, bytes);
        changes.Add(InsertChunk(targetNode, newChunk));

        var newMemoryNode = targetNode.Next;
        if (newMemoryNode is null)
        {
            throw new InvalidOperationException("Next node is not supposed to be null.");
        }

        var newImmutableLength = -newMemoryNode.Value.Length;
        if (relativeOffset is not 0)
        {
            newImmutableLength = node.Value.Length - (relativeOffset + bytes.Length);
            RemoveBefore(changes, newMemoryNode, node.Value.Length - relativeOffset);
        }

        if (newImmutableLength <= 0)
        {
            RemoveAfter(changes, newMemoryNode, Math.Abs(newImmutableLength));
        }
        else
        {
            if (node.Value.Clone() is not IImmutableChunk newImmutableChunk)
            {
                throw new InvalidOperationException("Expected new chunk to be immutable.");
            }

            newImmutableChunk.SourceOffset += relativeOffset + bytes.Length;
            newImmutableChunk.Length = newImmutableLength;
            changes.Add(InsertChunk(newMemoryNode, newImmutableChunk));
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

        if (modification.Data.Direction is GrowDirection.Start or GrowDirection.Both && node.Previous is not null)
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
        if (node.Previous is null)
        {
            return;
        }

        changes.SetStartAtPrevious();

        var previous = node.Previous;
        RemoveFromChunk(changes, previous, previous.Value.Length - removeLength, removeLength, true);
    }

    private void RemoveAfter(ChangeCollection changes, LinkedListNode<IChunk> node, long removeLength)
    {
        if (node.Next is null)
        {
            return;
        }

        long removedBytes = 0;
        var removeNode = node.Next;
        while (removeNode is not null && removedBytes < removeLength)
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
            var removeChunkChange = new RemoveChunkChange(ReferenceEquals(node.Value, changes.FirstChunk)).Apply(this, node);
            if (prependChange)
            {
                changes.Prepend(removeChunkChange);
            }
            else
            {
                changes.Add(removeChunkChange);
            }
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
        while (node is not null)
        {
            var chunk = node.Value;
            if (offset <= findOffset && offset + chunk.Length > findOffset || node.Next is null)
            {
                return (node, offset);
            }

            offset += chunk.Length;
            node = node.Next;
        }

        return (null, 0);
    }

    public ByteBufferStream AsStream() => new(this);

    #region Saving
    public async Task<bool> SaveAsync(CancellationToken cancellationToken = default)
    {
        GuardAgainstInvalidState();

        await _lock.AcquireWriterLockAsync(cancellationToken);

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
        using var _ = await _lock.AcquireReaderLockAsync(cancellationToken);

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

    private async Task<bool> InternalSaveToFileAsync(FileStream fileStream, CancellationToken cancellationToken)
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

    #endregion

    /// <summary>
    ///     Check whether the structure of the buffer might have changed. This implies verifying if any insert of delete
    ///     modifications have been made.
    /// </summary>
    public bool HasStructureChanged()
    {
        return IsModified &&
               _changeTracker.UndoStack.Any(group =>
                   group.Collections.Any(collection =>
                       collection.Modification is InsertModification or DeleteModification));
    }

    [StackTraceHidden]
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

    [StackTraceHidden]
    private void GuardAgainstInvalidOffset(
        long offset,
        [CallerArgumentExpression("offset")] string? offsetExpression = null)
    {
        if (offset < 0 || offset > Length)
        {
            throw new ArgumentOutOfRangeException(offsetExpression, offset, "Offset is not between 0 and length of buffer.");
        }
    }

    [StackTraceHidden]
    private void GuardAgainstInvalidOffsetAndLength(
        long offset, 
        long length, 
        [CallerArgumentExpression("offset")] string? offsetExpression = null, 
        [CallerArgumentExpression("length")] string? lengthExpression = null)
    {
        if (offset < 0 || offset > Length - 1)
        {
            throw new ArgumentOutOfRangeException(offsetExpression, offset, "Offset is not between 0 and length of buffer.");
        }

        if (offset + length > Length)
        {
            throw new ArgumentOutOfRangeException(lengthExpression, length, "Combination of offset and length exceeds length of buffer.");
        }
    }

    private ref struct ChangeScope
    {
        private readonly ByteBuffer _buffer;
        private readonly long _oldLength;

        public ChangeScope(ByteBuffer buffer, ChangeCollection changes)
        {
            _buffer = buffer;
            Changes = changes;
            _oldLength = buffer.Length;
        }

        public ChangeCollection Changes { get; }

        public static ChangeScope Delete(ByteBuffer buffer, long offset, long length)
        {
            return new ChangeScope(buffer, new ChangeCollection(new DeleteModification(offset, length), offset, buffer.Chunks.First?.Value));
        }

        public static ChangeScope Insert(ByteBuffer buffer, long offset, byte[] bytes)
        {
            return new ChangeScope(buffer, new ChangeCollection(new InsertModification(offset, bytes), offset, buffer.Chunks.First?.Value));
        }

        public static ChangeScope Write(ByteBuffer buffer, long offset, byte[] bytes)
        {
            return new ChangeScope(buffer, new ChangeCollection(new WriteModification(offset, bytes), offset, buffer.Chunks.First?.Value));
        }

        public void Dispose()
        {
            _buffer.Version++;
            _buffer._changeTracker.Push(Changes);

            if (_buffer._changeTracker.IsGroup)
            {
                return;
            }

            _buffer.OnModified(new[] {Changes.Modification}, ModificationSource.User);

            if (_oldLength != _buffer.Length)
            {
                _buffer.OnLengthChanged(_oldLength, _buffer.Length);
            }
        }
    }
}