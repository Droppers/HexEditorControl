namespace HexControl.Core.Buffers.Modifications;

public record WriteModification(long Offset, byte[] Bytes) : BufferModification(Offset, Bytes.Length);