namespace HexControl.Core.Characters;

public abstract class CharacterSet
{
    public bool Groupable { get; set; }

    public int Width { get; init; }

    public abstract int GetCharacters(byte @byte, char[] destBuffer);

    public abstract bool TryWrite(byte input, char @char, int nibble, out byte output);
}