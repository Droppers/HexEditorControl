namespace HexControl.Buffers.Helpers;

internal static class ByteArrayExtensions
{
    public static byte[] Copy(this byte[] buffer, long offset, long length)
    {
        if (length <= 0)
        {
            return Array.Empty<byte>();
        }

        var copy = new byte[length];
        Array.Copy(buffer, offset, copy, 0, length);
        return copy;
    }

    public static void Write(this byte[] buffer, long offset, byte[] writeBuffer)
    {
        Array.Copy(writeBuffer, 0, buffer, offset, writeBuffer.Length);
    }
}