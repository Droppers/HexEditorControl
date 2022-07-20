using JetBrains.Annotations;

namespace HexControl.SharedControl.Characters;

[PublicAPI]
public abstract class CharacterSet
{
    public bool Groupable { get; protected init; }

    public int Width { get; protected init; } = 1;

    public int DataWidth { get; protected init; } = 1;
    
    public abstract int GetCharacters(byte @byte, Span<char> destBuffer);

    public abstract bool TryWrite(byte input, char @char, int nibble, out byte output);
}

[PublicAPI]
public interface IStringParsable
{
    bool TryParse(string value, Span<byte> buffer, out int length);
}

[PublicAPI]
public interface IStringConvertible
{
    string ToString(ReadOnlySpan<byte> buffer, FormatInfo info);
}
