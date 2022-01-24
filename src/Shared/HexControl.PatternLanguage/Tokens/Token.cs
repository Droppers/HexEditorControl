using System;
using System.Collections.Generic;
using HexControl.PatternLanguage.Literals;

namespace HexControl.PatternLanguage.Tokens;

internal abstract class Token
{
    public enum Keyword
    {
        Struct,
        Union,
        Using,
        Enum,
        Bitfield,
        LittleEndian,
        BigEndian,
        If,
        Else,
        Parent,
        This,
        While,
        For,
        Function,
        Return,
        Namespace,
        In,
        Out,
        Break,
        Continue
    }

    public enum Operator
    {
        AtDeclaration,
        Assignment,
        Inherit,
        Plus,
        Minus,
        Star,
        Slash,
        Percent,
        ShiftLeft,
        ShiftRight,
        BitOr,
        BitAnd,
        BitXor,
        BitNot,
        BoolEquals,
        BoolNotEquals,
        BoolGreaterThan,
        BoolLessThan,
        BoolGreaterThanOrEquals,
        BoolLessThanOrEquals,
        BoolAnd,
        BoolOr,
        BoolXor,
        BoolNot,
        TernaryConditional,
        Dollar,
        AddressOf,
        SizeOf,
        ScopeResolution
    }

    public enum Separator
    {
        RoundBracketOpen,
        RoundBracketClose,
        CurlyBracketOpen,
        CurlyBracketClose,
        SquareBracketOpen,
        SquareBracketClose,
        Comma,
        Dot,
        EndOfExpression,
        EndOfProgram
    }

    public enum TokenType : long
    {
        Keyword,
        ValueType,
        Operator,
        Integer,
        String,
        Identifier,
        Separator
    }

    public enum ValueType
    {
        Unsigned8Bit = 0x10,
        Signed8Bit = 0x11,
        Unsigned16Bit = 0x20,
        Signed16Bit = 0x21,
        Unsigned32Bit = 0x40,
        Signed32Bit = 0x41,
        Unsigned64Bit = 0x80,
        Signed64Bit = 0x81,
        Unsigned128Bit = 0x100,
        Signed128Bit = 0x101,
        Character = 0x13,
        Character16 = 0x23,
        Boolean = 0x14,
        Float = 0x42,
        Double = 0x82,
        String = 0x15,
        Auto = 0x16,
        CustomType = 0x00,
        Padding = 0x1F,

        Unsigned = 0xFF00,
        Signed = 0xFF01,
        FloatingPoint = 0xFF02,
        Integer = 0xFF03,
        Any = 0xFFFF
    }

    protected Token(TokenType type, int lineNumber)
    {
        Type = type;
        //Value = value;
        LineNumber = lineNumber;
    }

    public TokenType Type { get; }

    //public ITokenValue? Value { get; }
    public int LineNumber { get; }

    public static bool IsUnsigned(ValueType type) => ((int)type & 0x0F) == 0x00;

    public static bool IsSigned(ValueType type) => ((int)type & 0x0F) == 0x01;

    public static bool IsFloatingPoint(ValueType type) => ((int)type & 0x0F) == 0x02;

    public static int GetTypeSize(ValueType type) => (int)type >> 4;

    public static string GetTypeName(ValueType type)
    {
        return type switch
        {
            ValueType.Signed8Bit => "s8",
            ValueType.Signed16Bit => "s16",
            ValueType.Signed32Bit => "s32",
            ValueType.Signed64Bit => "s64",
            ValueType.Signed128Bit => "s128",
            ValueType.Unsigned8Bit => "u8",
            ValueType.Unsigned16Bit => "u16",
            ValueType.Unsigned32Bit => "u32",
            ValueType.Unsigned64Bit => "u64",
            ValueType.Unsigned128Bit => "u128",
            ValueType.Float => "float",
            ValueType.Double => "double",
            ValueType.Character => "char",
            ValueType.Character16 => "char16",
            ValueType.Padding => "padding",
            ValueType.String => "str",
            _ => "< ??? >"
        };
    }

    public bool TokenValueEquals(Token otherTokenValue)
    {
        switch (Type)
        {
            case TokenType.Integer or TokenType.Identifier or TokenType.String:
                return true;
            case TokenType.ValueType:
                if (this is not Token<EnumValue<ValueType>> valueType ||
                    otherTokenValue is not Token<EnumValue<ValueType>> otherValueType)
                {
                    return false;
                }

                var value = valueType.Value.Value;
                var otherValue = otherValueType.Value.Value;

                if (otherValue == value)
                {
                    return true;
                }

                return otherValue switch
                {
                    ValueType.Any => value != ValueType.CustomType && value != ValueType.Padding,
                    ValueType.Unsigned => IsUnsigned(value),
                    ValueType.Signed => IsSigned(value),
                    ValueType.FloatingPoint => IsFloatingPoint(value),
                    ValueType.Integer => IsUnsigned(value) || IsSigned(value),
                    _ => false
                };
            default:
                return CompareValue(otherTokenValue);
        }
    }

    protected abstract bool CompareValue(Token token);

    public override int GetHashCode() => Type.GetHashCode();

    public interface ITokenValue { }

    public readonly struct IdentifierValue : ITokenValue, IEquatable<IdentifierValue>
    {
        public string Value { get; }

        public IdentifierValue(string value)
        {
            Value = value;
        }

        public bool Equals(IdentifierValue other) => Value == other.Value;

        public override bool Equals(object? obj) => obj is IdentifierValue other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();
    }

    public readonly struct EnumValue<T> : ITokenValue, IEquatable<EnumValue<T>> where T : notnull
    {
        public T Value { get; }

        public EnumValue(T value)
        {
            Value = value;
        }

        public override bool Equals(object? other) => other is EnumValue<T> otherEnum && Equals(otherEnum);

        public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode(Value);

        public bool Equals(EnumValue<T> other) => EqualityComparer<T>.Default.Equals(Value, other.Value);
    }

    public readonly struct LiteralValue : ITokenValue, IEquatable<LiteralValue>
    {
        public Literal Literal { get; }

        public LiteralValue(Literal literal)
        {
            Literal = literal;
        }

        public override bool Equals(object? other) => other is LiteralValue otherLiteral && Equals(otherLiteral);

        public override int GetHashCode() => Literal.GetHashCode();

        public bool Equals(LiteralValue other) => Literal.Equals(other.Literal);
    }
}