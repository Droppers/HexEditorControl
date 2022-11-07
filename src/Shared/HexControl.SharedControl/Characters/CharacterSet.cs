using JetBrains.Annotations;

namespace HexControl.SharedControl.Characters;

[PublicAPI]
public abstract class CharacterSet
{
    public CharacterSetType Type { get; protected init; }

    public bool Groupable { get; protected init; }

    public int VisualWidth { get; protected init; } = 1;

    public int ByteWidth { get; protected init; } = 1;
    
    public abstract int GetCharacters(ReadOnlySpan<byte> bytes, Span<char> destBuffer);

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
    string? ToString(ReadOnlySpan<byte> buffer, FormatInfo info);
}
