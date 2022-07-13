using JetBrains.Annotations;

namespace HexControl.Buffers.Chunks;

[PublicAPI]
public interface IImmutableChunk : IChunk
{
    public long SourceOffset { get; set; }
}