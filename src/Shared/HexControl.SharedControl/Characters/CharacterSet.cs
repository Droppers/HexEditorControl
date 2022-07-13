namespace HexControl.SharedControl.Characters;

public abstract class CharacterSet
{
    public bool Groupable { get; set; }

    public int Width { get; init; }

    public abstract int GetCharacters(byte @byte, Span<char> destBuffer);

    public abstract bool TryWrite(byte input, char @char, int nibble, out byte output);
}