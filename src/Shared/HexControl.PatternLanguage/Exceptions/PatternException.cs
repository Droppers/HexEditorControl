using System;

namespace HexControl.PatternLanguage.Exceptions;

public class PatternException : Exception
{
    internal PatternException(string message) : base(message) { }
}