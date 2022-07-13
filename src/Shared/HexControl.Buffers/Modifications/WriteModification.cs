namespace HexControl.Buffers.Modifications;

public record WriteModification(long Offset, byte[] Bytes) : BufferModification(Offset, Bytes.Length);