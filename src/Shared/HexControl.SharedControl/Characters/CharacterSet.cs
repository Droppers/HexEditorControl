namespace HexControl.SharedControl.Characters;

public abstract class CharacterSet
{
    public bool Groupable { get; init; }

    public int Width { get; init; }
    
    public abstract int GetCharacters(byte @byte, Span<char> destBuffer);

    public abstract bool TryWrite(byte input, char @char, int nibble, out byte output);
}

public interface IStringParsable
{
    bool TryParse(string value, Span<byte> buffer, out int length);
}

public interface IStringConvertible
{
    string ToString(ReadOnlySpan<byte> buffer, FormatInfo info);
}
