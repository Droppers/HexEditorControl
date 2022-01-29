using System;
using System.Collections.Generic;
using System.Diagnostics;
using HexControl.PatternLanguage.Functions;
using HexControl.PatternLanguage.Literals;

namespace HexControl.PatternLanguage.Standard;

internal class StdNamespace : IFunctionNamespace
{
    public override FunctionNamespace Namespace => new("std");

    protected override void RegisterFunctions()
    {
        Register("print", FunctionParameterCount.AtLeast(1), Print);
        Register("assert", 2, Assert);
    }

    private static Literal? Print(Evaluator ctx, IReadOnlyList<Literal> parameters)
    {
        var message = Format(ctx, parameters);
#if DEBUG
        Debug.WriteLine(message);
#endif
        Console.WriteLine(message);
        return null;
    }

    private static Literal? Assert(Evaluator ctx, IReadOnlyList<Literal> parameters)
    {
        var condition = parameters[0].ToBool();
        var message = parameters[1].ToString(false);

        if (!condition)
        {
            throw new Exception($"assertion failed \"{message}\"");
        }

        return null;
    }

    private static string ReplaceFirst(string text, string search, string replace)
    {
        var pos = text.IndexOf(search, StringComparison.Ordinal);
        return pos < 0 ? text : $"{text[..pos]}{replace}{text[(pos + search.Length)..]}";
    }

    private static string Format(Evaluator ctx, IReadOnlyList<Literal> parameters)
    {
        var format = parameters[0].ToString()!;

        for (var i = 1; i < parameters.Count; i++)
        {
            var arg = parameters[i];
            var value = arg is PatternDataLiteral pattern
                ? pattern.Value.ToString(ctx)
                : arg.ToString() ?? "";

            format = ReplaceFirst(format, "{}", value);
        }

        return format;
    }
}