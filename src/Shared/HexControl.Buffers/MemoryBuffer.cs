using HexControl.Buffers.Chunks;
using HexControl.Buffers.Find;
using JetBrains.Annotations;

namespace HexControl.Buffers;

[PublicAPI]
public class MemoryBuffer : ByteBuffer
{
    public MemoryBuffer(byte[] bytes, bool readOnly = false, ChangeTracking changeTracking = ChangeTracking.UndoRedo) : base(changeTracking)
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
        CancellationToken cancellationToken) =>
        strategy.FindInBuffer(Bytes, offset, length, options, cancellationToken);
}