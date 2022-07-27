using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;

namespace HexControl.SharedControl.Characters;

[PublicAPI]
public sealed class BinaryCharacterSet : CharacterSet, IStringParsable, IStringConvertible
{
    private const char ZERO = '0';
    private const char ONE = '1';

    public BinaryCharacterSet()
    {
        Groupable = true;
        Width = 8;
    }

    public override int GetCharacters(ReadOnlySpan<byte> bytes, Span<char> destBuffer)
    {
        var @byte = bytes[0];
        destBuffer[0] = GetBit(@byte, 0);
        destBuffer[1] = GetBit(@byte, 1);
        destBuffer[2] = GetBit(@byte, 2);
        destBuffer[3] = GetBit(@byte, 3);
        destBuffer[4] = GetBit(@byte, 4);
        destBuffer[5] = GetBit(@byte, 5);
        destBuffer[6] = GetBit(@byte, 6);
        destBuffer[7] = GetBit(@byte, 7);
        return Width;
    }

    public override bool TryWrite(byte input, char @char, int nibble, out byte output)
    {
        if (@char != ZERO && @char != ONE)
        {
            output = default;
            return false;
        }

        output = SetBit(input, nibble, @char == ZERO ? 0 : 1);
        return true;
    }

    public bool TryParse(string value, Span<byte> buffer, out int length)
    {
        value = string.Join("", value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        length = 0;

        if (value.Length % 8 is not 0)
        {
            return false;
        }

        byte currentByte = 0;
        for (var i = 0; i < value.Length; i++)
        {
            var chr = value[i];
            if (chr != ZERO && chr != ONE)
            {
                return false;
            }

            currentByte = SetBit(currentByte, (i + 1) % 8, chr == ZERO ? 0 : 1);

            if ((i + 1) % 8 == 0)
            {
                buffer[length++] = currentByte;
            }
        }

        return true;
    }

    public string ToString(ReadOnlySpan<byte> buffer, FormatInfo info)
    {
        var sb = new StringBuilder();
        var currentOffset = info.Offset;

        foreach (var @byte in buffer)
        {
            sb.Append(GetBit(@byte, 0))
                .Append(GetBit(@byte, 1))
                .Append(GetBit(@byte, 2))
                .Append(GetBit(@byte, 3))
                .Append(GetBit(@byte, 4))
                .Append(GetBit(@byte, 5))
                .Append(GetBit(@byte, 6))
                .Append(GetBit(@byte, 7));

            if (currentOffset is not 0 && (currentOffset + 1) % info.Configuration.GroupSize is 0 || info.Configuration.GroupSize is 1)
            {
                sb.Append(' ');
            }

            currentOffset++;
        }

        return sb.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char GetBit(byte @byte, byte index)
    {
        return (@byte & (1 << index)) is not 0 ? ONE : ZERO;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte SetBit(byte @byte, int index, int value)
    {
        return (byte)(@byte & ~(1 << index) | (value << index));
    }
}