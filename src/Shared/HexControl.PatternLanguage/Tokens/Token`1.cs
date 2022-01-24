using System;

namespace HexControl.PatternLanguage.Tokens;

internal class Token<TValue> : Token where TValue : IEquatable<TValue>
{
    public Token(TokenType type, TValue value, int lineNumber) : base(type, lineNumber)
    {
        Value = value;
    }

    public TValue Value { get; }

    protected override bool CompareValue(Token other) =>
        other is Token<TValue> otherValue && Value.Equals(otherValue.Value);

    public override int GetHashCode() => HashCode.Combine(Type, Value);
}