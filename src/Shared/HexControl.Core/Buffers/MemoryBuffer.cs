using HexControl.Core.Buffers.Chunks;
using JetBrains.Annotations;

namespace HexControl.Core.Buffers;

[PublicAPI]
public class MemoryBuffer : BaseBuffer
{
    private readonly byte[] _bytes;

    public MemoryBuffer(byte[] bytes)
    {
        _bytes = bytes;
        Initialize(new ImmutableMemoryChunk(this, _bytes));
    }

    protected override long FindInImmutable(IFindStrategy strategy, long offset, long length, FindOptions options,
        CancellationToken cancellationToken) => strategy.SearchInBuffer(_bytes, offset, length, options.Backward);
}