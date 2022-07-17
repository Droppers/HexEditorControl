using HexControl.Buffers.Chunks;

namespace HexControl.Buffers.History.Changes;

internal class InsertToMemoryChange : IChunkChange<MemoryChunk>
{
    private readonly byte[] _bytes;
    private readonly long _offset;

    public InsertToMemoryChange(long offset, byte[] bytes)
    {
        _offset = offset;
        _bytes = bytes;
    }

    public IChunkChange<MemoryChunk> Apply(
        ByteBuffer buffer,
        LinkedListNode<IChunk> contextNode,
        MemoryChunk chunk)
    {
        var newBytes = new byte[chunk.Bytes.Length + _bytes.Length];
        if (_offset > 0)
        {
            Array.Copy(chunk.Bytes, 0, newBytes, 0, _offset);
        }

        Array.Copy(_bytes, 0, newBytes, _offset, _bytes.Length);
        if (_offset < chunk.Bytes.Length)
        {
            Array.Copy(chunk.Bytes, _offset, newBytes, _offset + _bytes.Length,
                chunk.Bytes.Length - _offset);
        }

        chunk.Bytes = newBytes;
        chunk.Length += _bytes.Length;

        buffer.Length += _bytes.Length;
        return this;
    }

    public IChunkChange<MemoryChunk> Revert(
        ByteBuffer buffer,
        LinkedListNode<IChunk> contextNode,
        MemoryChunk chunk)
    {
        var newBytes = new byte[chunk.Bytes.Length - _bytes.Length];
        if (_offset > 0)
        {
            Array.Copy(chunk.Bytes, 0, newBytes, 0, _offset);
        }

        if (_offset < newBytes.Length)
        {
            Array.Copy(chunk.Bytes, _offset + _bytes.Length, newBytes, _offset,
                chunk.Bytes.Length - (_offset + _bytes.Length));
        }

        chunk.Bytes = newBytes;
        chunk.Length -= _bytes.Length;

        buffer.Length -= _bytes.Length;
        return this;
    }
}