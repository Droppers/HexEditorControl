using HexControl.Core.Events;

namespace HexControl.Core.Buffers;

public interface IBuffer
{
    long Length { get; }
    long OriginalLength { get; }


    event EventHandler<LengthChangedEventArgs>? LengthChanged;

    // Modifying methods
    void Write(long writeOffset, byte value);
    void Write(long writeOffset, byte[] writeBytes);
    void Delete(long deleteOffset, long deleteLength);
    void Insert(long insertOffset, byte[] insertBytes);

    public void Undo();

    public void Redo();

    // Reading methods
    Task<long> ReadAsync(
        long readOffset,
        byte[] readBuffer,
        List<ModifiedRange>? modifiedRanges = null,
        CancellationToken? cancellationToken = null);

    Task<byte[]> ReadAsync(long readOffset, long readLength, List<ModifiedRange>? modifiedRanges = null,
        CancellationToken? cancellationToken = null);
}