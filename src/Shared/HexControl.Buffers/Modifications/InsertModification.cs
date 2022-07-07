namespace HexControl.Buffers.Modifications;

public record InsertModification(long Offset, byte[] Bytes) : BufferModification(Offset, Bytes.Length);