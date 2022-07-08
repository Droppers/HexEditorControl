using System.IO.MemoryMappedFiles;

namespace HexControl.Buffers.Find;

public interface IFindStrategy
{
    long FindInFile(
        MemoryMappedViewAccessor accessor,
        long startOffset,
        long maxSearchLength,
        bool backward,
        CancellationToken cancellationToken);

    long FindInBuffer(byte[] buffer, long startOffset, long maxSearchLength, bool backward, CancellationToken cancellationToken);
    long FindInBuffer(ByteBuffer buffer, long startOffset, long maxSearchLength, bool backward, CancellationToken cancellationToken);
}