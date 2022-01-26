using System;

namespace HexControl.PatternLanguage.Exceptions;

public class LexerException : Exception
{
    internal LexerException(Lexer lexer, string message) : base(FormatMessage(lexer, message)) { }

    private static string FormatMessage(Lexer lexer, string message) =>
        $"{message} at line {lexer.LineNumber + 1}, column {lexer.Column + 1}.";
}