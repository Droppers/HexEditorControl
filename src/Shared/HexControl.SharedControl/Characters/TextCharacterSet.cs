namespace HexControl.SharedControl.Characters;

public sealed class TextCharacterSet : CharacterSet
{
    private readonly char[] _characters;

    public TextCharacterSet(char[] characters)
    {
        if (characters.Length != 256)
        {
            throw new ArgumentOutOfRangeException(nameof(characters), "Expects an char array with length of 256.");
        }

        _characters = characters;
        Width = 1;
    }

    public TextCharacterSet(CharacterEncoding encoding) : this(CharacterTable.Table[encoding]) { }

    public override int GetCharacters(byte @byte, char[] destBuffer)
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
}