using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexControl.Core.Helpers;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Types;

namespace HexControl.PatternLanguage
{
    internal static class StringExtensions
    {
        public static ReadOnlySpan<char> RemoveSuffix(this ReadOnlySpan<char> target, int length)
        {
            return target[0..^length];
        }
        
        public static ReadOnlySpan<char> SafeSubString(this ReadOnlySpan<char> value, int startIndex)
        {
            return value[startIndex..];
        }

        public static ReadOnlySpan<char> SafeSubString(this ReadOnlySpan<char> value, int startIndex, int length)
        {
            return value[startIndex..Math.Min(startIndex+length, value.Length)];
        }
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

        private static int? FindFirstNotOf(ReadOnlySpan<char> source, string chars)
        {
            for (var i = 0; i < source.Length; i++)
            {
                if (!chars.Contains(source[i]))
                {
                    return i;
                }
            }

            return null;
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

        private static int? GetIntegerLiteralLength(ReadOnlySpan<char> source)
        {
            const string chars = "0123456789ABCDEFabcdef.xUL";
            return FindFirstNotOf(source, chars);
        }

        private static Literal? ParseIntegerLiteral(ReadOnlySpan<char> value)
        {
            var type = Token.ValueType.Any;

            ushort @base;

            var endPos = GetIntegerLiteralLength(value);
            var numberData = endPos is not null ? value[0..endPos.Value] : value;
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

                if (FindFirstNotOf(numberData, "0123456789ABCDEFabcdef") is not null)
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

                if (FindFirstNotOf(numberData, "01") is not null)
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

                if (CountOccurrences(numberData.ToString(), '.') > 1 || FindFirstNotOf(numberData, "0123456789.") is not null)
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

                if (FindFirstNotOf(numberData, "0123456789") is not null)
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
                // TODO: use BigInteger?
                long integer = 0;
                foreach (var c in numberData)
                {
                    integer *= @base;

                    if (char.IsDigit(c))
                    {
                        integer += (c - '0');
                    }
                    else if (c >= 'A' && c <= 'F')
                    {
                        integer += 10 + (c - 'A');
                    }
                    else if (c >= 'a' && c <= 'f')
                    {
                        integer += 10 + (c - 'a');
                    }
                    else
                    {
                        return null;
                    }
                }

                return type switch
                {
                    Token.ValueType.Unsigned128Bit => integer,
                    Token.ValueType.Signed128Bit => integer,
                    _ => null
                };
            }
            else if (Token.IsFloatingPoint(type))
            {
                var floatingPoint = Convert.ToDouble(numberData.ToString());

                return type switch
                {
                    Token.ValueType.Float => floatingPoint,
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

                    if (value[2] < '0' || value[2] > '7' || value[3] < '0' || value[3] > '7' || value[4] < '0' || value[4] > '7')
                    {
                        return null;
                    }

                    // TODO: WTF -> IMPLEMENT THIS!
                    //return { { std::strtoul(&string[2], nullptr, 8), 5 } };
                    return null;
                }

                return null;
            }
            else
            {
                return (value[0], 1);
            }
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

        private Token CreateToken(Token.TokenType type, Token.Separator separator)
        {
            return CreateToken(type, new Token.EnumValue<Token.Separator>(separator));
        }

        private Token CreateToken(Token.TokenType type, Token.Operator @operator)
        {
            return CreateToken(type, new Token.EnumValue<Token.Operator>(@operator));
        }

        private Token CreateToken(Token.TokenType type, Token.Keyword keyword)
        {
            return CreateToken(type, new Token.EnumValue<Token.Keyword>(keyword));
        }

        private Token CreateToken(Token.TokenType type, Token.ValueType valueType)
        {
            return CreateToken(type, new Token.EnumValue<Token.ValueType>(valueType));
        }

        private Token CreateToken(Token.TokenType type, Token.ITokenValue value)
        {
            return new Token(type, value, _lineNumber);
        }

        private Token CreateToken(Token.TokenType type, Literal literal)
        {
            return CreateToken(type, new Token.LiteralValue(literal));
        }

        private Token CreateToken(Token.TokenType type, Token.Identifier identifier)
        {
            return CreateToken(type, (Token.ITokenValue)identifier);
        }

        private int _lineNumber;

        // TODO: convert to span usage
        public List<Token> Lex(ReadOnlySpan<char> code) {
            var tokens = new List<Token>();
            var offset = 0;
            
                while (offset < code.Length)
                {
                    var c = code[offset];

                    if (c == 0x00)
                    {
                        break;
                    }

                    if (char.IsWhiteSpace(c))
                    {
                        if (code[offset] == '\n')
                        {
                            _lineNumber++;
                        }

                        offset += 1;
                    }
                    else if (c == ';')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Separator, Token.Separator.EndOfExpression));
                        offset += 1;
                    }
                    else if (c == '(')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Separator, Token.Separator.RoundBracketOpen));
                        offset += 1;
                    }
                    else if (c == ')')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Separator, Token.Separator.RoundBracketClose));
                        offset += 1;
                    }
                    else if (c == '{')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Separator, Token.Separator.CurlyBracketOpen));
                        offset += 1;
                    }
                    else if (c == '}')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Separator, Token.Separator.CurlyBracketClose));
                        offset += 1;
                    }
                    else if (c == '[')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Separator, Token.Separator.SquareBracketOpen));
                        offset += 1;
                    }
                    else if (c == ']')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Separator, Token.Separator.SquareBracketClose));
                        offset += 1;
                    }
                    else if (c == ',')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Separator, Token.Separator.Comma));
                        offset += 1;
                    }
                    else if (c == '.')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Separator, Token.Separator.Dot));
                        offset += 1;
                    }
                    else if (code.SafeSubString(offset, 2).SequenceEqual("::"))
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.ScopeResolution));
                        offset += 2;
                    }
                    else if (c == '@')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.AtDeclaration));
                        offset += 1;
                    }
                    else if (code.SafeSubString(offset, 2).SequenceEqual("=="))
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.BoolEquals));
                        offset += 2;
                    }
                    else if (code.SafeSubString(offset, 2).SequenceEqual("!="))
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.BoolNotEquals));
                        offset += 2;
                    }
                    else if (code.SafeSubString(offset, 2).SequenceEqual(">="))
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.BoolGreaterThanOrEquals));
                        offset += 2;
                    }
                    else if (code.SafeSubString(offset, 2).SequenceEqual("<="))
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.BoolLessThanOrEquals));
                        offset += 2;
                    }
                    else if (code.SafeSubString(offset, 2).SequenceEqual("&&"))
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.BoolAnd));
                        offset += 2;
                    }
                    else if (code.SafeSubString(offset, 2).SequenceEqual("||"))
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.BoolOr));
                        offset += 2;
                    }
                    else if (code.SafeSubString(offset, 2).SequenceEqual("^^"))
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.BoolXor));
                        offset += 2;
                    }
                    else if (c == '=')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.Assignment));
                        offset += 1;
                    }
                    else if (c == ':')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.Inherit));
                        offset += 1;
                    }
                    else if (c == '+')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.Plus));
                        offset += 1;
                    }
                    else if (c == '-')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.Minus));
                        offset += 1;
                    }
                    else if (c == '*')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.Star));
                        offset += 1;
                    }
                    else if (c == '/')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.Slash));
                        offset += 1;
                    }
                    else if (c == '%')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.Percent));
                        offset += 1;
                    }
                    else if (code.SafeSubString(offset, 2).SequenceEqual("<<"))
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.ShiftLeft));
                        offset += 2;
                    }
                    else if (code.SafeSubString(offset, 2).SequenceEqual(">>"))
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.ShiftRight));
                        offset += 2;
                    }
                    else if (c == '>')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.BoolGreaterThan));
                        offset += 1;
                    }
                    else if (c == '<')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.BoolLessThan));
                        offset += 1;
                    }
                    else if (c == '!')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.BoolNot));
                        offset += 1;
                    }
                    else if (c == '|')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.BitOr));
                        offset += 1;
                    }
                    else if (c == '&')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.BitAnd));
                        offset += 1;
                    }
                    else if (c == '^')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.BitXor));
                        offset += 1;
                    }
                    else if (c == '~')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.BitNot));
                        offset += 1;
                    }
                    else if (c == '?')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.TernaryConditional));
                        offset += 1;
                    }
                    else if (c == '$')
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.Dollar));
                        offset += 1;
                    }
                    else if (code.SafeSubString(offset, 9).SequenceEqual("addressof"))
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.AddressOf));
                        offset += 9;
                    }
                    else if (code.SafeSubString(offset, 6).SequenceEqual("sizeof"))
                    {
                        tokens.Add(CreateToken(Token.TokenType.Operator, Token.Operator.SizeOf));
                        offset += 6;
                    }
                    else if (c == '\'')
                    {
                        var character = GetCharacterLiteral(code.SafeSubString(offset));

                        if (character is null)
                        {
                            throw new Exception($"invalid character literal: {_lineNumber}");
                        }

                        var (c2, charSize) = character.Value;
                    
                        // TODO: char16 or char?
                        tokens.Add(CreateToken(Token.TokenType.Integer, Literal.Create((AsciiChar)(byte)c2)));
                        offset += charSize;
                    }
                    else if (c == '\"')
                    {
                        var @string = GetStringLiteral(code.SafeSubString(offset));

                        if (@string is null)
                        {
                            throw new Exception($"invalid string literal: {_lineNumber}");
                        }

                        var (s, stringSize) = @string.Value;

                        tokens.Add(CreateToken(Token.TokenType.String, s));
                        offset += stringSize;
                    }
                    else if (char.IsLetter(c) || c == '_')
                    {
                        var identifier = MatchTillInvalid(code.SafeSubString(offset), (char newChar) => char.IsLetterOrDigit(newChar) || newChar == '_');

                        // Check for reserved keywords

                        // TODO: Convert to switch expression
                        switch (identifier)
                        {
                            case "struct":
                                tokens.Add(CreateToken(Token.TokenType.Keyword, Token.Keyword.Struct));
                                break;
                            case "union":
                                tokens.Add(CreateToken(Token.TokenType.Keyword, Token.Keyword.Union));
                                break;
                            case "using":
                                tokens.Add(CreateToken(Token.TokenType.Keyword, Token.Keyword.Using));
                                break;
                            case "enum":
                                tokens.Add(CreateToken(Token.TokenType.Keyword, Token.Keyword.Enum));
                                break;
                            case "bitfield":
                                tokens.Add(CreateToken(Token.TokenType.Keyword, Token.Keyword.Bitfield));
                                break;
                            case "be":
                                tokens.Add(CreateToken(Token.TokenType.Keyword, Token.Keyword.BigEndian));
                                break;
                            case "le":
                                tokens.Add(CreateToken(Token.TokenType.Keyword, Token.Keyword.LittleEndian));
                                break;
                            case "if":
                                tokens.Add(CreateToken(Token.TokenType.Keyword, Token.Keyword.If));
                                break;
                            case "else":
                                tokens.Add(CreateToken(Token.TokenType.Keyword, Token.Keyword.Else));
                                break;
                            case "false":
                                tokens.Add(CreateToken(Token.TokenType.Integer, false));
                                break;
                            case "true":
                                tokens.Add(CreateToken(Token.TokenType.Integer, true));
                                break;
                            case "parent":
                                tokens.Add(CreateToken(Token.TokenType.Keyword, Token.Keyword.Parent));
                                break;
                            case "this":
                                tokens.Add(CreateToken(Token.TokenType.Keyword, Token.Keyword.This));
                                break;
                            case "while":
                                tokens.Add(CreateToken(Token.TokenType.Keyword, Token.Keyword.While));
                                break;
                            case "for":
                                tokens.Add(CreateToken(Token.TokenType.Keyword, Token.Keyword.For));
                                break;
                            case "fn":
                                tokens.Add(CreateToken(Token.TokenType.Keyword, Token.Keyword.Function));
                                break;
                            case "return":
                                tokens.Add(CreateToken(Token.TokenType.Keyword, Token.Keyword.Return));
                                break;
                            case "namespace":
                                tokens.Add(CreateToken(Token.TokenType.Keyword, Token.Keyword.Namespace));
                                break;
                            case "in":
                                tokens.Add(CreateToken(Token.TokenType.Keyword, Token.Keyword.In));
                                break;
                            case "out":
                                tokens.Add(CreateToken(Token.TokenType.Keyword, Token.Keyword.Out));
                                break;
                            case "break":
                                tokens.Add(CreateToken(Token.TokenType.Keyword, Token.Keyword.Break));
                                break;
                            case "continue":
                                tokens.Add(CreateToken(Token.TokenType.Keyword, Token.Keyword.Continue));
                                break;
                            // Check for built-in types
                            case "u8":
                                tokens.Add(CreateToken(Token.TokenType.ValueType, Token.ValueType.Unsigned8Bit));
                                break;
                            case "s8":
                                tokens.Add(CreateToken(Token.TokenType.ValueType, Token.ValueType.Signed8Bit));
                                break;
                            case "u16":
                                tokens.Add(CreateToken(Token.TokenType.ValueType, Token.ValueType.Unsigned16Bit));
                                break;
                            case "s16":
                                tokens.Add(CreateToken(Token.TokenType.ValueType, Token.ValueType.Signed16Bit));
                                break;
                            case "u32":
                                tokens.Add(CreateToken(Token.TokenType.ValueType, Token.ValueType.Unsigned32Bit));
                                break;
                            case "s32":
                                tokens.Add(CreateToken(Token.TokenType.ValueType, Token.ValueType.Signed32Bit));
                                break;
                            case "u64":
                                tokens.Add(CreateToken(Token.TokenType.ValueType, Token.ValueType.Unsigned64Bit));
                                break;
                            case "s64":
                                tokens.Add(CreateToken(Token.TokenType.ValueType, Token.ValueType.Signed64Bit));
                                break;
                            case "u128":
                                tokens.Add(CreateToken(Token.TokenType.ValueType, Token.ValueType.Unsigned128Bit));
                                break;
                            case "s128":
                                tokens.Add(CreateToken(Token.TokenType.ValueType, Token.ValueType.Signed128Bit));
                                break;
                            case "float":
                                tokens.Add(CreateToken(Token.TokenType.ValueType, Token.ValueType.Float));
                                break;
                            case "double":
                                tokens.Add(CreateToken(Token.TokenType.ValueType, Token.ValueType.Double));
                                break;
                            case "char":
                                tokens.Add(CreateToken(Token.TokenType.ValueType, Token.ValueType.Character));
                                break;
                            case "char16":
                                tokens.Add(CreateToken(Token.TokenType.ValueType, Token.ValueType.Character16));
                                break;
                            case "bool":
                                tokens.Add(CreateToken(Token.TokenType.ValueType, Token.ValueType.Boolean));
                                break;
                            case "str":
                                tokens.Add(CreateToken(Token.TokenType.ValueType, Token.ValueType.String));
                                break;
                            case "padding":
                                tokens.Add(CreateToken(Token.TokenType.ValueType, Token.ValueType.Padding));
                                break;
                            case "auto":
                                tokens.Add(CreateToken(Token.TokenType.ValueType, Token.ValueType.Auto));
                                break;
                            // If it's not a keyword and a builtin type, it has to be an identifier
                            default:
                                tokens.Add(CreateToken(Token.TokenType.Identifier, new Token.Identifier(identifier)));
                                break;
                        }

                        offset += identifier.Length;
                    }
                    else if (char.IsDigit(c))
                    {
                        var integer = ParseIntegerLiteral(code.SafeSubString(offset));

                        if (integer is null)
                        {
                            throw new Exception($"invalid integer literal: {_lineNumber}");
                        }


                        tokens.Add(CreateToken(Token.TokenType.Integer, integer));
                        offset += GetIntegerLiteralLength(code.SafeSubString(offset)) ?? 1;
                    }
                    else
                    {
                        throw new Exception($"unknown token: {c} at {_lineNumber}");

                    }

                }

                tokens.Add(CreateToken(Token.TokenType.Separator, Token.Separator.EndOfProgram));

            return tokens;
        }
    }
}