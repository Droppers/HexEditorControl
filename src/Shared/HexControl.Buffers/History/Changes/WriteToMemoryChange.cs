using HexControl.Buffers.Chunks;

namespace HexControl.Buffers.History.Changes;

internal class WriteToMemoryChange : IChunkChange<MemoryChunk>
{
    private readonly byte[] _writeBuffer;
    private readonly long _writeOffset;

    public WriteToMemoryChange(long writeOffset, byte[] writeBuffer)
    {
        _writeOffset = writeOffset;
        _writeBuffer = writeBuffer;
    }

    public RevertData Data { get; private set; } = null!;

    public IChunkChange<MemoryChunk> Apply(BaseBuffer buffer, LinkedListNode<IChunk> contextNode, MemoryChunk chunk)
    {
        var growStart = _writeOffset < 0 ? Math.Abs(_writeOffset) : 0;
        var growEnd = Math.Max(0, _writeOffset + _writeBuffer.Length - chunk.Length);

        if (growStart > 0 || growEnd > 0)
        {
            var newBuffer = new byte[chunk.Bytes.Length + growStart + growEnd];
            Array.Copy(chunk.Bytes, 0, newBuffer, growStart, chunk.Bytes.Length);

            var overwritten = Array.Empty<byte>();
            var direction = GrowDirection.Both;
            if (growStart > 0 && growEnd > 0)
            {
                overwritten = chunk.Bytes.Copy(0, chunk.Bytes.Length); // all overwritten
                newBuffer.Write(0, _writeBuffer);
            }
            else if (growStart > 0)
            {
                direction = GrowDirection.Start;
                overwritten = chunk.Bytes.Copy(0, _writeBuffer.Length - growStart);
                newBuffer.Write(0, _writeBuffer);
            }
            else if (growEnd > 0)
            {
                direction = GrowDirection.End;
                overwritten = chunk.Bytes.Copy(_writeOffset, _writeBuffer.Length - growEnd);
                newBuffer.Write(_writeOffset, _writeBuffer);
            }

            chunk.Bytes = newBuffer;
            chunk.Length = newBuffer.Length;
            Data = new RevertData(direction, growStart, growEnd, overwritten);
        }
        else
        {
            var overwritten = chunk.Bytes.Copy(_writeOffset, _writeBuffer.Length);
            chunk.Bytes.Write(_writeOffset, _writeBuffer);
            Data = new RevertData(GrowDirection.None, 0, 0, overwritten);
        }

        buffer.Length += growStart + growEnd;
        return this;
    }

    public IChunkChange<MemoryChunk> Revert(BaseBuffer buffer, LinkedListNode<IChunk> contextNode, MemoryChunk chunk)
    {
        if (Data == null)
        {
            throw new InvalidOperationException("Apply a modification before reverting.");
        }

        if (Data.Direction is GrowDirection.None)
        {
            chunk.Bytes.Write(_writeOffset, Data.OverwrittenBuffer);
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