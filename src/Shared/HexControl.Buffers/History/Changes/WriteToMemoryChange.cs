using HexControl.Buffers.Chunks;
using HexControl.Buffers.Helpers;

namespace HexControl.Buffers.History.Changes;

internal class WriteToMemoryChange : IChunkChange<MemoryChunk>
{
    private readonly long _offset;
    private readonly byte[] _bytes;

    public WriteToMemoryChange(long offset, byte[] bytes)
    {
        _offset = offset;
        _bytes = bytes;
    }

    public RevertData Data { get; private set; } = null!;

    public IChunkChange<MemoryChunk> Apply(ByteBuffer buffer, LinkedListNode<IChunk> contextNode, MemoryChunk chunk)
    {
        var growStart = _offset < 0 ? Math.Abs(_offset) : 0;
        var growEnd = Math.Max(0, _offset + _bytes.Length - chunk.Length);

        if (growStart > 0 || growEnd > 0)
        {
            var newBuffer = new byte[chunk.Bytes.Length + growStart + growEnd];
            Array.Copy(chunk.Bytes, 0, newBuffer, growStart, chunk.Bytes.Length);

            var overwritten = Array.Empty<byte>();
            var direction = GrowDirection.Both;
            if (growStart > 0 && growEnd > 0)
            {
                overwritten = chunk.Bytes.Copy(0, chunk.Bytes.Length); // all overwritten
                newBuffer.Write(0, _bytes);
            }
            else if (growStart > 0)
            {
                direction = GrowDirection.Start;
                overwritten = chunk.Bytes.Copy(0, _bytes.Length - growStart);
                newBuffer.Write(0, _bytes);
            }
            else if (growEnd > 0)
            {
                direction = GrowDirection.End;
                overwritten = chunk.Bytes.Copy(_offset, _bytes.Length - growEnd);
                newBuffer.Write(_offset, _bytes);
            }

            chunk.Bytes = newBuffer;
            chunk.Length = newBuffer.Length;
            Data = new RevertData(direction, growStart, growEnd, overwritten);
        }
        else
        {
            var overwritten = chunk.Bytes.Copy(_offset, _bytes.Length);
            chunk.Bytes.Write(_offset, _bytes);
            Data = new RevertData(GrowDirection.None, 0, 0, overwritten);
        }

        buffer.Length += growStart + growEnd;
        return this;
    }

    public IChunkChange<MemoryChunk> Revert(ByteBuffer buffer, LinkedListNode<IChunk> contextNode, MemoryChunk chunk)
    {
        if (Data == null)
        {
            throw new InvalidOperationException("Apply a modification before reverting.");
        }

        if (Data.Direction is GrowDirection.None)
        {
            chunk.Bytes.Write(_offset, Data.OverwrittenBuffer);
            return this;
        }

        var newBuffer = chunk.Bytes.Copy(Data.GrowStart, chunk.Bytes.Length - Data.GrowStart - Data.GrowEnd);
        var writeOffset = Data.Direction switch
        {
            GrowDirection.Both => 0,
            GrowDirection.Start => 0,
            GrowDirection.End => newBuffer.Length - Data.OverwrittenBuffer.Length,
            _ => 0
        };
        newBuffer.Write(writeOffset, Data.OverwrittenBuffer);

        chunk.Bytes = newBuffer;
        chunk.Length = newBuffer.Length;

        buffer.Length -= Data.GrowStart + Data.GrowEnd;
        return this;
    }

    public class RevertData
    {
        public RevertData(GrowDirection direction, long growStart, long growEnd, byte[] overwrittenBuffer)
        {
            Direction = direction;
            GrowStart = growStart;
            GrowEnd = growEnd;
            OverwrittenBuffer = overwrittenBuffer;
        }

        public GrowDirection Direction { get; }
        public long GrowStart { get; }
        public long GrowEnd { get; }
        public byte[] OverwrittenBuffer { get; }
    }
}