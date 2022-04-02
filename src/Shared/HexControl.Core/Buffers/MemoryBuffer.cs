using HexControl.Core.Buffers.Chunks;
using JetBrains.Annotations;

namespace HexControl.Core.Buffers;

[PublicAPI]
public class MemoryBuffer : BaseBuffer
{
    public MemoryBuffer(byte[] bytes, bool readOnly = false)
    {
        IsReadOnly = readOnly;
        Bytes = bytes;
        Initialize(CreateDefaultChunk());
    }

    public byte[] Bytes { get; private set; }

    protected sealed override IChunk CreateDefaultChunk() => new ImmutableMemoryChunk(this, Bytes);

    protected override async Task<bool> SaveInternalAsync(CancellationToken cancellationToken)
    {
        var targetBuffer = Length == OriginalLength ? Bytes : new byte[Length];
        var memoryStream = new MemoryStream(targetBuffer);
        await AsStream().CopyToAsync(memoryStream, cancellationToken);

        Bytes = targetBuffer;

        return true;
    }

    protected override long FindInImmutable(IFindStrategy strategy, long offset, long length, FindOptions options,
        CancellationToken cancellationToken) => strategy.SearchInBuffer(Bytes, offset, length, options.Backward);
}