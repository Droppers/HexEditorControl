using HexControl.Core.Buffers.Chunks;

namespace HexControl.Core.Buffers.History.Changes;

internal class RemoveFromMemoryChange : IChunkChange<MemoryChunk>
{
    private readonly long _removeLength;
    private readonly long _removeOffset;

    private RevertData? _revertData;

    public RemoveFromMemoryChange(long removeOffset, long removeLength)
    {
        _removeOffset = removeOffset;
        _removeLength = removeLength;
    }

    public IChunkChange<MemoryChunk> Apply(
        BaseBuffer buffer,
        LinkedListNode<IChunk> contextNode,
        MemoryChunk chunk)
    {
        if (chunk.Bytes.Length - _removeLength <= 0)
        {
            throw new InvalidOperationException("Tried remove too great of a length from chunk.");
        }

        // Set data for reverting
        var removedBuffer = new byte[_removeLength];
        Array.Copy(chunk.Bytes, _removeOffset, removedBuffer, 0, _removeLength);
        _revertData = new RevertData(removedBuffer);

        var newBuffer = new byte[chunk.Bytes.Length - _removeLength];
        if (_removeOffset > 0)
        {
            Array.Copy(chunk.Bytes, 0, newBuffer, 0, _removeOffset);
        }

        if (_removeOffset + _removeLength < chunk.Bytes.Length)
        {
            Array.Copy(chunk.Bytes, _removeOffset + _removeLength, newBuffer, _removeOffset,
                newBuffer.Length - _removeOffset);
        }

        chunk.Bytes = newBuffer;
        chunk.Length -= _removeLength;

        buffer.Length -= _removeLength;
        return this;
    }

    public IChunkChange<MemoryChunk> Revert(
        BaseBuffer buffer,
        LinkedListNode<IChunk> contextNode,
        MemoryChunk chunk)
    {
        if (_revertData == null)
        {
            throw new InvalidOperationException("Apply a modification before reverting.");
        }

        var data = _revertData.Value;
        var insertOffset = _removeOffset;
        var insertBuffer = data.RemovedBuffer;

        var newData = new byte[chunk.Bytes.Length + insertBuffer.Length];
        if (insertOffset > 0)
        {
            Array.Copy(chunk.Bytes, 0, newData, 0, insertOffset);
        }

        Array.Copy(insertBuffer, 0, newData, insertOffset, insertBuffer.Length);
        if (insertOffset < chunk.Bytes.Length)
        {
            Array.Copy(chunk.Bytes, insertOffset, newData, insertOffset + insertBuffer.Length,
                chunk.Bytes.Length - insertOffset);
        }

        chunk.Bytes = newData;
        chunk.Length += insertBuffer.Length;

        buffer.Length += insertBuffer.Length;
        return this;
    }

    private struct RevertData
    {
        public RevertData(byte[] removedBuffer)
        {
            RemovedBuffer = removedBuffer;
        }

        public byte[] RemovedBuffer { get; }
    }
}