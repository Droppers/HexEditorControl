using System.Text;

namespace HexControl.SharedControl.Characters;

public sealed class TextCharacterSet : CharacterSet, IStringConvertible, IStringParsable
{
    private const char NULL_CHARACTER_REPLACEMENT = '�';

    private readonly char[] _characters;

    public TextCharacterSet(char[] characters)
    {
        if (characters.Length != 256)
        {
            throw new ArgumentOutOfRangeException(nameof(characters), "Expects an char array with length of 256.");
        }

        _characters = characters;

        Groupable = false;
        Width = 1;
    }

    public TextCharacterSet(CharacterEncoding encoding) : this(CharacterTable.Table[encoding]) { }

    public override int GetCharacters(byte @byte, Span<char> destBuffer)
    {
        var @char = _characters[@byte];
        destBuffer[0] = @char == '\0' ? '.' : @char;
        return Width;
    }

    public override bool TryWrite(byte input, char @char, int nibble, out byte output)
    {
        for (var i = 0; i < _characters.Length; i++)
        {
            if (_characters[i] != @char)
            {
                continue;
            }

            output = (byte)i;
            return true;
        }

        output = input;
        return false;
    }

    public bool TryParse(string value, Span<byte> parsedBuffer, out int length)
    {
        length = 0;

        foreach(var chr in value)
        {
            if(!TryConvertCharacterToByte(chr, out var @byte))
            {
                parsedBuffer[length] = 0;
                length++;
                continue;
            }

            parsedBuffer[length] = @byte;
            length++;
        }

        return true;
    }

    public string ToString(ReadOnlySpan<byte> buffer, FormatInfo info)
    {
        var sb = new StringBuilder();
        foreach (var @byte in buffer)
        {
            var chr = _characters[@byte];
            sb.Append(chr is '\0' ? NULL_CHARACTER_REPLACEMENT : chr);
        }

        return sb.ToString();
    }

    private bool TryConvertCharacterToByte(char value, out byte @byte)
    {
        for (var i = 0; i < Math.Min(256, _characters.Length); i++)
        {
            var @char = _characters[i];
            if(@char == value)
            {
                @byte = (byte)i;
                return true;
            }
        }

        @byte = default;
        return false;
    }
}