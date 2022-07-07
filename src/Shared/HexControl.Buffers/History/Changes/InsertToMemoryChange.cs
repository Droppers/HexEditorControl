using HexControl.Buffers.Chunks;

namespace HexControl.Buffers.History.Changes;

internal class InsertToMemoryChange : IChunkChange<MemoryChunk>
{
    private readonly byte[] _insertBytes;
    private readonly long _insertOffset;

    public InsertToMemoryChange(long insertOffset, byte[] insertBytes)
    {
        _insertOffset = insertOffset;
        _insertBytes = insertBytes;
    }

    public IChunkChange<MemoryChunk> Apply(
        BaseBuffer buffer,
        LinkedListNode<IChunk> contextNode,
        MemoryChunk chunk)
    {
        var newBytes = new byte[chunk.Bytes.Length + _insertBytes.Length];
        if (_insertOffset > 0)
        {
            Array.Copy(chunk.Bytes, 0, newBytes, 0, _insertOffset);
        }

        Array.Copy(_insertBytes, 0, newBytes, _insertOffset, _insertBytes.Length);
        if (_insertOffset < chunk.Bytes.Length)
        {
            Array.Copy(chunk.Bytes, _insertOffset, newBytes, _insertOffset + _insertBytes.Length,
                chunk.Bytes.Length - _insertOffset);
        }

        chunk.Bytes = newBytes;
        chunk.Length += _insertBytes.Length;

        buffer.Length += _insertBytes.Length;
        return this;
    }

    public IChunkChange<MemoryChunk> Revert(
        BaseBuffer buffer,
        LinkedListNode<IChunk> contextNode,
        MemoryChunk chunk)
    {
        var newBytes = new byte[chunk.Bytes.Length - _insertBytes.Length];
        if (_insertOffset > 0)
        {
            Array.Copy(chunk.Bytes, 0, newBytes, 0, _insertOffset);
        }

        if (_insertOffset < newBytes.Length)
        {
            Array.Copy(chunk.Bytes, _insertOffset + _insertBytes.Length, newBytes, _insertOffset,
                chunk.Bytes.Length - (_insertOffset + _insertBytes.Length));
        }

        chunk.Bytes = newBytes;
        chunk.Length -= _insertBytes.Length;

        buffer.Length -= _insertBytes.Length;
        return this;
    }
}