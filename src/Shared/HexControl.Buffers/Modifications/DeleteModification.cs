namespace HexControl.Buffers.Modifications;

public record DeleteModification(long Offset, long Length) : BufferModification(Offset, Length);