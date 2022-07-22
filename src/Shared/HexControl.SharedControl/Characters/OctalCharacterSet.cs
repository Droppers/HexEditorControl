using JetBrains.Annotations;
using System.Text;

namespace HexControl.SharedControl.Characters;

[PublicAPI]
public sealed class OctalCharacterSet : CharacterSet, IStringParsable, IStringConvertible
{
    private const int BASE = 8;
    private const int NUMERIC_OFFSET = 48;

    public OctalCharacterSet()
    {
        Groupable = true;
        Width = 3;
    }

    public override int GetCharacters(ReadOnlySpan<byte> bytes, Span<char> destBuffer)
    {
        var @byte = bytes[0];
        destBuffer[2] = (char)(@byte % BASE + NUMERIC_OFFSET);
        @byte /= BASE;
        destBuffer[1] = (char)(@byte % BASE + NUMERIC_OFFSET);
        @byte /= BASE;
        destBuffer[0] = (char)(@byte % BASE + NUMERIC_OFFSET);

        return Width;
    }

    public override bool TryWrite(byte input, char @char, int nibble, out byte output)
    {
        var c = nibble is 2 ? @char : (char)(input % BASE + NUMERIC_OFFSET);
        input /= BASE;
        var b = nibble is 1 ? @char : (char)(input % BASE + NUMERIC_OFFSET);
        input /= BASE;
        var a = nibble is 0 ? @char : (char)(input % BASE + NUMERIC_OFFSET);

        try
        {
            var @byte = Convert.ToUInt32($"{a}{b}{c}", 8);
            output = (byte)@byte;
            return @byte is >= 0 and <= 255;
        }
        catch
        {
            output = default;
            return false;
        }
    }

    public bool TryParse(string value, Span<byte> buffer, out int length)
    {
        value = string.Join("", value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        length = 0;

        if (value.Length % 3 is not 0)
        {
            return false;
        }

        for (var i = 0; i < value.Length; i += 3)
        {
            var @byte = Convert.ToUInt32(value.Substring(i, 3), 8);
            if(@byte is < 0 or > 255)
            {
                return false;
            }

            buffer[length++] = (byte)@byte;
        }

        return true;
    }

    public string ToString(ReadOnlySpan<byte> buffer, FormatInfo info)
    {
        var sb = new StringBuilder();
        var currentOffset = info.Offset;

        foreach (var @byte in buffer)
        {
            var number = @byte;
            var c = (char)(number % BASE + NUMERIC_OFFSET);
            number /= BASE;
            var b = (char)(number % BASE + NUMERIC_OFFSET);
            number /= BASE;
            var a = (char)(number % BASE + NUMERIC_OFFSET);

            sb.Append(a).Append(b).Append(c);

            if (currentOffset is not 0 && (currentOffset + 1) % info.Configuration.GroupSize is 0 || info.Configuration.GroupSize is 1)
            {
                sb.Append(' ');
            }

            currentOffset++;
        }

        return sb.ToString();
    }
}