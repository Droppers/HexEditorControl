namespace HexControl.Core.Buffers.Modifications;

public record InsertModification(long Offset, byte[] Bytes) : BufferModification(Offset);