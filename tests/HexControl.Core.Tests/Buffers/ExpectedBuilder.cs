using System.Collections.Generic;
using System.Linq;
using HexControl.Core.Buffers.Chunks;

namespace HexControl.Core.Tests.Buffers;

public class ExpectedBuilder
{
    private readonly ValidatingBuffer _buffer;

    public ExpectedBuilder(ValidatingBuffer buffer)
    {
        _buffer = buffer;
        Chunks = new List<IChunk>();
    }

    public long ExpectedLength { get; private set; } = -1;

    public List<IChunk> Chunks { get; }

    public ExpectedBuilder Immutable(long length, long sourceOffset)
    {
        Chunks.Add(new ImmutableMemoryChunk(_buffer, _buffer.Bytes)
        {
            Length = length,
            SourceOffset = sourceOffset
        });
        return this;
    }

    public ExpectedBuilder Memory(byte[] bytes)
    {
        Chunks.Add(new MemoryChunk(_buffer, bytes.ToArray()));
        return this;
    }

    public ExpectedBuilder Length(long expectedLength)
    {
        ExpectedLength = expectedLength;
        return this;
    }
}