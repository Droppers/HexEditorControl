namespace HexControl.PatternLanguage.Helpers;

// A wrapper around a byte to represent a single byte character, since this does not exist in C#.
public readonly struct AsciiChar
{
    private byte Value { get; }

    private AsciiChar(byte value)
    {
        Value = value;
    }

    public static implicit operator byte(AsciiChar d) => d.Value;
    public static explicit operator AsciiChar(byte b) => new(b);
}