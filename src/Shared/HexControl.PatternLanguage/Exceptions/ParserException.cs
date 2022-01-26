using System;

namespace HexControl.PatternLanguage.Exceptions;

public class ParserException : Exception
{
    internal ParserException(Parser parser, string message, int tokenOffset = 0) : base(FormatMessage(parser, message,
        tokenOffset)) { }

    private static string FormatMessage(Parser parser, string message, int tokenOffset)
    {
        var token = parser.GetToken(tokenOffset);
        return $"{message} at line {token.LineNumber + 1}, column {token.Column + 1}.";
    }
}