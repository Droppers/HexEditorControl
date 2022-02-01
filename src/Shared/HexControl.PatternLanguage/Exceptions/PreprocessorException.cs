namespace HexControl.PatternLanguage.Exceptions;

internal class PreprocessorException : PatternException
{
    internal PreprocessorException(string message, int lineNumber) : base(FormatMessage(message, lineNumber)) { }

    private static string FormatMessage(string message, int lineNumber) => $"{message} at line {lineNumber}.";
}