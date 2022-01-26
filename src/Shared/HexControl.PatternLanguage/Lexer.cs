using System;
using System.Collections.Generic;
using System.Text;
using HexControl.Core.Helpers;
using HexControl.Core.Numerics;
using HexControl.PatternLanguage.Exceptions;
using HexControl.PatternLanguage.Extensions;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Tokens;
using HexControl.PatternLanguage.Types;

namespace HexControl.PatternLanguage;

internal static class StringExtensions
{
    public static ReadOnlySpan<char> RemoveSuffix(this ReadOnlySpan<char> target, int length) => target[..^length];

    public static ReadOnlySpan<char> SafeSubString(this ReadOnlySpan<char> value, int startIndex) =>
        value[startIndex..];

    public static ReadOnlySpan<char> SafeSubString(this ReadOnlySpan<char> value, int startIndex, int length) =>
        value[startIndex..Math.Min(startIndex + length, value.Length)];
}

internal static class CharExtensions
{
    public static bool IsXDigit(this char c)
    {
        return c switch
        {
            >= '0' and <= '9' => true,
            >= 'a' and <= 'f' => true,
            >= 'A' and <= 'F' => true,
            _ => false
        };
    }
}

internal class Lexer
{
    private int _lineNumber;
    private int _offset;

    private List<Token> _tokens;

    public Lexer()
    {
        _tokens = new List<Token>();
    }

    internal int Column { get; private set; }

    internal int Offset
    {
        get => _offset;
        private set
        {
            Column += value - _offset;
            _offset = value;
        }
    }

    internal int LineNumber
    {
        get => _lineNumber;
        private set
        {
            Column = 0;
            _lineNumber = value;
        }
    }

    private static string MatchTillInvalid(ReadOnlySpan<char> characters, Func<char, bool> predicate)
    {
        var builder = ObjectPool<StringBuilder>.Shared.Rent();
        try
        {
            builder.Clear();

            var idx = 0;
            while (idx < characters.Length && characters[idx] != 0x00)
            {
                builder.Append(characters[idx]);
                idx++;

                if (!predicate(characters[idx]))
                {
                    break;
                }
            }

            return builder.ToString();
        }
        finally
        {
            ObjectPool<StringBuilder>.Shared.Return(builder);
        }
    }

    private static int FindFirstNotOf(ReadOnlySpan<char> source, string chars)
    {
        for (var i = 0; i < source.Length; i++)
        {
            if (!chars.Contains(source[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static int CountOccurrences(string source, char needle)
    {
        var n = 0;
        var count = 0;

        while ((n = source.IndexOf(needle, n)) != -1)
        {
            n += 1;
            ++count;
        }

        return count;
    }

    private static int GetIntegerLiteralLength(ReadOnlySpan<char> source)
    {
        const string chars = "0123456789ABCDEFabcdef.xUL";
        return FindFirstNotOf(source, chars);
    }

    private static Literal? ParseIntegerLiteral(ReadOnlySpan<char> value)
    {
        var type = Token.ValueType.Any;

        ushort @base;

        var endPos = GetIntegerLiteralLength(value);
        var numberData = endPos is not -1 ? value[..endPos] : value;
        //auto numberData = /*std::string_view(literal)*/.substr(0, endPos);

        if (numberData.EndsWith("U"))
        {
            type = Token.ValueType.Unsigned128Bit;
            numberData.RemoveSuffix(1);
        }
        else if (!numberData.StartsWith("0x") && !numberData.StartsWith("0b"))
        {
            if (numberData.EndsWith("F"))
            {
                type = Token.ValueType.Float;
                numberData.RemoveSuffix(1);
            }
            else if (numberData.EndsWith("D"))
            {
                type = Token.ValueType.Double;
                numberData.RemoveSuffix(1);
            }
        }

        if (numberData.StartsWith("0x"))
        {
            numberData = numberData[2..];
            @base = 16;

            if (Token.IsFloatingPoint(type))
            {
                return null;
            }

            if (FindFirstNotOf(numberData, "0123456789ABCDEFabcdef") is not -1)
            {
                return null;
            }
        }
        else if (numberData.StartsWith("0b"))
        {
            numberData = numberData[2..];
            @base = 2;

            if (Token.IsFloatingPoint(type))
            {
                return null;
            }

            if (FindFirstNotOf(numberData, "01") is not -1)
            {
                return null;
            }
        }
        else if (numberData.IndexOf('.') != -1 || Token.IsFloatingPoint(type))
        {
            @base = 10;
            if (type == Token.ValueType.Any)
            {
                type = Token.ValueType.Double;
            }

            if (CountOccurrences(numberData.ToString(), '.') > 1 ||
                FindFirstNotOf(numberData, "0123456789.") is not -1)
            {
                return null;
            }

            if (numberData.EndsWith(","))
            {
                return null;
            }
        }
        else if (char.IsDigit(numberData[0]))
        {
            @base = 10;

            if (FindFirstNotOf(numberData, "0123456789") is not -1)
            {
                return null;
            }
        }
        else
        {
            return null;
        }

        if (type == Token.ValueType.Any)
        {
            type = Token.ValueType.Signed128Bit;
        }


        if (numberData.Length == 0)
        {
            return null;
        }

        if (Token.IsUnsigned(type) || Token.IsSigned(type))
        {
            UInt128 integer = 0;
            foreach (var c in numberData)
            {
                integer *= @base;

                if (c.IsNumeric())
                {
                    integer += (UInt128)(c - '0');
                }
                else if (c is >= 'A' and <= 'F')
                {
                    integer += (UInt128)(10 + (c - 'A'));
                }
                else if (c is >= 'a' and <= 'f')
                {
                    integer += (UInt128)(10 + (c - 'a'));
                }
                else
                {
                    return null;
                }
            }

            return type switch
            {
                Token.ValueType.Unsigned128Bit => integer,
                Token.ValueType.Signed128Bit => (Int128)integer,
                _ => null
            };
        }

        if (Token.IsFloatingPoint(type))
        {
            var floatingPoint = Convert.ToDouble(numberData.ToString());

            return type switch
            {
                Token.ValueType.Float => (double)(float)floatingPoint,
                Token.ValueType.Double => floatingPoint,
                _ => null
            };
        }


        return null;
    }

    private static (char, int)? GetCharacter(ReadOnlySpan<char> value)
    {
        if (value.Length < 1)
        {
            return null;
        }

        // Escape sequences
        if (value[0] == '\\')
        {
            if (value.Length < 2)
            {
                return null;
            }

            // Handle simple escape sequences
            switch (value[1])
            {
                case 'a': return ('\a', 2);
                case 'b': return ('\b', 2);
                case 'f': return ('\f', 2);
                case 'n': return ('\n', 2);
                case 'r': return ('\r', 2);
                case 't': return ('\t', 2);
                case 'v': return ('\v', 2);
                case '\\': return ('\\', 2);
                case '\'': return ('\'', 2);
                case '\"': return ('\"', 2);
            }

            // Hexadecimal number
            if (value[1] == 'x')
            {
                if (value.Length != 4)
                {
                    return null;
                }

                if (!value[2].IsXDigit() || !value[3].IsXDigit())
                {
                    return null;
                }

                // TODO: WTF -> IMPLEMENT THIS!
                return null;
                //return (Convert.ToInt64(literal[2..].ToString(), 16), 4);
                //return (std::strtoul(&string[2], nullptr, 16), 4 } };
            }

            // Octal number
            if (value[1] == 'o')
            {
                if (value.Length != 5)
                {
                    return null;
                }

                if (value[2] < '0' || value[2] > '7' || value[3] < '0' || value[3] > '7' || value[4] < '0' ||
                    value[4] > '7')
                {
                    return null;
                }

                // TODO: WTF -> IMPLEMENT THIS!
                //return { { std::strtoul(&string[2], nullptr, 8), 5 } };
                return null;
            }

            return null;
        }

        return (value[0], 1);
    }

    private static (string, int)? GetStringLiteral(ReadOnlySpan<char> value)
    {
        if (!value.StartsWith(@""""))
        {
            return null;
        }

        var builder = ObjectPool<StringBuilder>.Shared.Rent();

        try
        {
            builder.Clear();
            var size = 1;
            while (value[size] != '\"')
            {
                var character = GetCharacter(value[size..]);

                if (character is null)
                {
                    return null;
                }

                var (c, charSize) = character.Value;

                builder.Append(c);
                size += charSize;

                if (size >= value.Length)
                {
                    return null;
                }
            }

            return (builder.ToString(), size + 1);
        }
        finally
        {
            ObjectPool<StringBuilder>.Shared.Return(builder);
        }
    }

    private static (char, int)? GetCharacterLiteral(ReadOnlySpan<char> value)
    {
        if (value.Length == 0)
        {
            return null;
        }

        if (value[0] != '\'')
        {
            return null;
        }


        var character = GetCharacter(value[1..]);

        if (character is null)
        {
            return null;
        }

        var (c, charSize) = character.Value;

        if (value.Length >= charSize + 2 && value[charSize + 1] != '\'')
        {
            return null;
        }

        return (c, charSize + 2);
    }

    private void Add(Token.TokenType type, Token.Separator separator, int length) =>
        Add(type, new Token.EnumValue<Token.Separator>(separator), length);

    private void Add(Token.TokenType type, Token.Operator @operator, int length) =>
        Add(type, new Token.EnumValue<Token.Operator>(@operator), length);

    private void Add(Token.TokenType type, Token.Keyword keyword, int length) =>
        Add(type, new Token.EnumValue<Token.Keyword>(keyword), length);

    private void Add(Token.TokenType type, Token.ValueType valueType, int length) =>
        Add(type, new Token.EnumValue<Token.ValueType>(valueType), length);

    private void Add<TType>(Token.TokenType type, TType value, int length)
        where TType : Token.ITokenValue, IEquatable<TType>
    {
        var token = new Token<TType>(type, value, length, _lineNumber, Column);
        _tokens.Add(token);

        Offset += length;
    }

    private void Add(Token.TokenType type, Literal literal, int length) =>
        Add(type, new Token.LiteralValue(literal), length);

    public List<Token> Lex(ReadOnlySpan<char> code)
    {
        var tokens = new List<Token>();
        _tokens = tokens;

        while (Offset < code.Length)
        {
            var c = code[Offset];

            if (c == 0x00)
            {
                break;
            }

            if (char.IsWhiteSpace(c))
            {
                if (code[Offset] == '\n')
                {
                    Offset += 1;
                    LineNumber++;
                }
                else
                {
                    Offset += 1;
                }
            }
            else if (c == ';')
            {
                Add(Token.TokenType.Separator, Token.Separator.EndOfExpression, 1);
            }
            else if (c == '(')
            {
                Add(Token.TokenType.Separator, Token.Separator.RoundBracketOpen, 1);
            }
            else if (c == ')')
            {
                Add(Token.TokenType.Separator, Token.Separator.RoundBracketClose, 1);
            }
            else if (c == '{')
            {
                Add(Token.TokenType.Separator, Token.Separator.CurlyBracketOpen, 1);
            }
            else if (c == '}')
            {
                Add(Token.TokenType.Separator, Token.Separator.CurlyBracketClose, 1);
            }
            else if (c == '[')
            {
                Add(Token.TokenType.Separator, Token.Separator.SquareBracketOpen, 1);
            }
            else if (c == ']')
            {
                Add(Token.TokenType.Separator, Token.Separator.SquareBracketClose, 1);
            }
            else if (c == ',')
            {
                Add(Token.TokenType.Separator, Token.Separator.Comma, 1);
            }
            else if (c == '.')
            {
                Add(Token.TokenType.Separator, Token.Separator.Dot, 1);
            }
            else if (code.SafeSubString(Offset, 2).SequenceEqual("::"))
            {
                Add(Token.TokenType.Operator, Token.Operator.ScopeResolution, 2);
            }
            else if (c == '@')
            {
                Add(Token.TokenType.Operator, Token.Operator.AtDeclaration, 1);
            }
            else if (code.SafeSubString(Offset, 2).SequenceEqual("=="))
            {
                Add(Token.TokenType.Operator, Token.Operator.BoolEquals, 2);
            }
            else if (code.SafeSubString(Offset, 2).SequenceEqual("!="))
            {
                Add(Token.TokenType.Operator, Token.Operator.BoolNotEquals, 2);
            }
            else if (code.SafeSubString(Offset, 2).SequenceEqual(">="))
            {
                Add(Token.TokenType.Operator, Token.Operator.BoolGreaterThanOrEquals, 2);
            }
            else if (code.SafeSubString(Offset, 2).SequenceEqual("<="))
            {
                Add(Token.TokenType.Operator, Token.Operator.BoolLessThanOrEquals, 2);
            }
            else if (code.SafeSubString(Offset, 2).SequenceEqual("&&"))
            {
                Add(Token.TokenType.Operator, Token.Operator.BoolAnd, 2);
            }
            else if (code.SafeSubString(Offset, 2).SequenceEqual("||"))
            {
                Add(Token.TokenType.Operator, Token.Operator.BoolOr, 2);
            }
            else if (code.SafeSubString(Offset, 2).SequenceEqual("^^"))
            {
                Add(Token.TokenType.Operator, Token.Operator.BoolXor, 2);
            }
            else if (c == '=')
            {
                Add(Token.TokenType.Operator, Token.Operator.Assignment, 1);
            }
            else if (c == ':')
            {
                Add(Token.TokenType.Operator, Token.Operator.Inherit, 1);
            }
            else if (c == '+')
            {
                Add(Token.TokenType.Operator, Token.Operator.Plus, 1);
            }
            else if (c == '-')
            {
                Add(Token.TokenType.Operator, Token.Operator.Minus, 1);
            }
            else if (c == '*')
            {
                Add(Token.TokenType.Operator, Token.Operator.Star, 1);
            }
            else if (c == '/')
            {
                Add(Token.TokenType.Operator, Token.Operator.Slash, 1);
            }
            else if (c == '%')
            {
                Add(Token.TokenType.Operator, Token.Operator.Percent, 1);
            }
            else if (code.SafeSubString(Offset, 2).SequenceEqual("<<"))
            {
                Add(Token.TokenType.Operator, Token.Operator.ShiftLeft, 1);
            }
            else if (code.SafeSubString(Offset, 2).SequenceEqual(">>"))
            {
                Add(Token.TokenType.Operator, Token.Operator.ShiftRight, 1);
            }
            else if (c == '>')
            {
                Add(Token.TokenType.Operator, Token.Operator.BoolGreaterThan, 1);
            }
            else if (c == '<')
            {
                Add(Token.TokenType.Operator, Token.Operator.BoolLessThan, 1);
            }
            else if (c == '!')
            {
                Add(Token.TokenType.Operator, Token.Operator.BoolNot, 1);
            }
            else if (c == '|')
            {
                Add(Token.TokenType.Operator, Token.Operator.BitOr, 1);
            }
            else if (c == '&')
            {
                Add(Token.TokenType.Operator, Token.Operator.BitAnd, 1);
            }
            else if (c == '^')
            {
                Add(Token.TokenType.Operator, Token.Operator.BitXor, 1);
            }
            else if (c == '~')
            {
                Add(Token.TokenType.Operator, Token.Operator.BitNot, 1);
            }
            else if (c == '?')
            {
                Add(Token.TokenType.Operator, Token.Operator.TernaryConditional, 1);
            }
            else if (c == '$')
            {
                Add(Token.TokenType.Operator, Token.Operator.Dollar, 1);
            }
            else if (code.SafeSubString(Offset, 9).SequenceEqual("addressof"))
            {
                Add(Token.TokenType.Operator, Token.Operator.AddressOf, 9);
            }
            else if (code.SafeSubString(Offset, 6).SequenceEqual("sizeof"))
            {
                Add(Token.TokenType.Operator, Token.Operator.SizeOf, 6);
            }
            else if (c == '\'')
            {
                var character = GetCharacterLiteral(code.SafeSubString(Offset));

                if (character is null)
                {
                    throw new LexerException(this, "Invalid character literal");
                }

                var (c2, charSize) = character.Value;

                // TODO: char16 or char?
                Add(Token.TokenType.Integer, Literal.Create((AsciiChar)(byte)c2), charSize);
            }
            else if (c == '\"')
            {
                var @string = GetStringLiteral(code.SafeSubString(Offset));

                if (@string is null)
                {
                    throw new LexerException(this, "Invalid string literal");
                }

                var (s, stringSize) = @string.Value;

                Add(Token.TokenType.String, s, stringSize);
            }
            else if (c.IsAlpha() || c is '_')
            {
                var identifier = MatchTillInvalid(code.SafeSubString(Offset),
                    newChar => newChar.IsAlphaNumeric() || newChar == '_');
                var length = identifier.Length;

                // Check for reserved keywords
                switch (identifier)
                {
                    case "struct":
                        Add(Token.TokenType.Keyword, Token.Keyword.Struct, length);
                        break;
                    case "union":
                        Add(Token.TokenType.Keyword, Token.Keyword.Union, length);
                        break;
                    case "using":
                        Add(Token.TokenType.Keyword, Token.Keyword.Using, length);
                        break;
                    case "enum":
                        Add(Token.TokenType.Keyword, Token.Keyword.Enum, length);
                        break;
                    case "bitfield":
                        Add(Token.TokenType.Keyword, Token.Keyword.Bitfield, length);
                        break;
                    case "be":
                        Add(Token.TokenType.Keyword, Token.Keyword.BigEndian, length);
                        break;
                    case "le":
                        Add(Token.TokenType.Keyword, Token.Keyword.LittleEndian, length);
                        break;
                    case "if":
                        Add(Token.TokenType.Keyword, Token.Keyword.If, length);
                        break;
                    case "else":
                        Add(Token.TokenType.Keyword, Token.Keyword.Else, length);
                        break;
                    case "false":
                        Add(Token.TokenType.Integer, false, length);
                        break;
                    case "true":
                        Add(Token.TokenType.Integer, true, length);
                        break;
                    case "parent":
                        Add(Token.TokenType.Keyword, Token.Keyword.Parent, length);
                        break;
                    case "this":
                        Add(Token.TokenType.Keyword, Token.Keyword.This, length);
                        break;
                    case "while":
                        Add(Token.TokenType.Keyword, Token.Keyword.While, length);
                        break;
                    case "for":
                        Add(Token.TokenType.Keyword, Token.Keyword.For, length);
                        break;
                    case "fn":
                        Add(Token.TokenType.Keyword, Token.Keyword.Function, length);
                        break;
                    case "return":
                        Add(Token.TokenType.Keyword, Token.Keyword.Return, length);
                        break;
                    case "namespace":
                        Add(Token.TokenType.Keyword, Token.Keyword.Namespace, length);
                        break;
                    case "in":
                        Add(Token.TokenType.Keyword, Token.Keyword.In, length);
                        break;
                    case "out":
                        Add(Token.TokenType.Keyword, Token.Keyword.Out, length);
                        break;
                    case "break":
                        Add(Token.TokenType.Keyword, Token.Keyword.Break, length);
                        break;
                    case "continue":
                        Add(Token.TokenType.Keyword, Token.Keyword.Continue, length);
                        break;
                    // Check for built-in types
                    case "u8":
                        Add(Token.TokenType.ValueType, Token.ValueType.Unsigned8Bit, length);
                        break;
                    case "s8":
                        Add(Token.TokenType.ValueType, Token.ValueType.Signed8Bit, length);
                        break;
                    case "u16":
                        Add(Token.TokenType.ValueType, Token.ValueType.Unsigned16Bit, length);
                        break;
                    case "s16":
                        Add(Token.TokenType.ValueType, Token.ValueType.Signed16Bit, length);
                        break;
                    case "u32":
                        Add(Token.TokenType.ValueType, Token.ValueType.Unsigned32Bit, length);
                        break;
                    case "s32":
                        Add(Token.TokenType.ValueType, Token.ValueType.Signed32Bit, length);
                        break;
                    case "u64":
                        Add(Token.TokenType.ValueType, Token.ValueType.Unsigned64Bit, length);
                        break;
                    case "s64":
                        Add(Token.TokenType.ValueType, Token.ValueType.Signed64Bit, length);
                        break;
                    case "u128":
                        Add(Token.TokenType.ValueType, Token.ValueType.Unsigned128Bit, length);
                        break;
                    case "s128":
                        Add(Token.TokenType.ValueType, Token.ValueType.Signed128Bit, length);
                        break;
                    case "float":
                        Add(Token.TokenType.ValueType, Token.ValueType.Float, length);
                        break;
                    case "double":
                        Add(Token.TokenType.ValueType, Token.ValueType.Double, length);
                        break;
                    case "char":
                        Add(Token.TokenType.ValueType, Token.ValueType.Character, length);
                        break;
                    case "char16":
                        Add(Token.TokenType.ValueType, Token.ValueType.Character16, length);
                        break;
                    case "bool":
                        Add(Token.TokenType.ValueType, Token.ValueType.Boolean, length);
                        break;
                    case "str":
                        Add(Token.TokenType.ValueType, Token.ValueType.String, length);
                        break;
                    case "padding":
                        Add(Token.TokenType.ValueType, Token.ValueType.Padding, length);
                        break;
                    case "auto":
                        Add(Token.TokenType.ValueType, Token.ValueType.Auto, length);
                        break;
                    // If it's not a keyword and a builtin type, it has to be an identifier
                    default:
                        Add(Token.TokenType.Identifier, new Token.IdentifierValue(identifier), length);
                        break;
                }
            }
            else if (char.IsDigit(c))
            {
                var integer = ParseIntegerLiteral(code.SafeSubString(Offset));

                if (integer is null)
                {
                    throw new LexerException(this, "Invalid integer literal");
                }

                var length = GetIntegerLiteralLength(code.SafeSubString(Offset));
                Add(Token.TokenType.Integer, integer, length);
            }
            else
            {
                throw new LexerException(this, $"Unknown token '{c}'");
            }
        }

        Add(Token.TokenType.Separator, Token.Separator.EndOfProgram, 0);

        return tokens;
    }
}