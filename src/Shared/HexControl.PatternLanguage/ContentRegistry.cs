using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HexControl.Core.Numerics;
using HexControl.PatternLanguage.Literals;

namespace HexControl.PatternLanguage;

public delegate Literal? PatternFunctionBody(Evaluator ctx, IReadOnlyList<Literal> parameters);

// C++ remnant, completely refactor this (maybe a source generator that generates function definitions for classes and methods with attributes?)
// If so, important that this can also be used by third parties to implement their own functions!
// 
/*
 * [PatternNamespace("std", "mem")
 * internal static class Memory {
 *    [PatternFunction("find_sequence", PatternLanguage.MoreParametersThan | 1)
 *    public static long FindSequence(byte[] sequence) {
 *        return 123;
 *    }
 */
public static class ContentRegistry
{
    public static class FunctionRegistry
    {
        public static readonly uint UnlimitedParameters = 0xFFFFFFFF;
        public static readonly uint MoreParametersThan = 0x80000000;
        public static readonly uint LessParametersThan = 0x40000000;
        public static readonly uint NoParameters = 0x00000000;
        
        public static readonly Dictionary<string, Function> Functions = new();

        static FunctionRegistry()
        {
            // Crappy
            RegisterFunction("std", "print", MoreParametersThan | 0, (evaluator, args) =>
            {
                var message = Format(evaluator, args);
#if DEBUG
                Debug.WriteLine(message);
#endif
                Console.WriteLine(message);
                return null;
            });

            RegisterFunction("std", "assert", 2, (ctx, args) =>
            {
                var condition = args[0].ToBool();
                var message = args[1].ToString(false);

                if (!condition)
                {
                    throw new Exception($"assertion failed \"{message}\"");
                }

                return null;
            });


            RegisterFunction("std::mem", "find_sequence", MoreParametersThan | 1, (ctx, args) =>
            {
                var occurrenceIndex = (int)args[0].ToInt128();

                var sequence = new List<byte>();
                for (var i = 1; i < args.Count; i++)
                {
                    var @byte = args[i].ToUInt128();

                    if (@byte > 0xFF)
                    {
                        throw new Exception($"byte #{i} value out of range: {@byte} > 0xFF");
                    }

                    sequence.Add((byte)@byte);
                }

                var bytes = new byte[sequence.Count];
                var occurrences = 0;
                for (var offset = 0; offset < ctx.Buffer.Length - sequence.Count; offset++)
                {
                    ctx.Buffer.Read(offset, bytes);

                    if (bytes.SequenceEqual(sequence))
                    {
                        if (occurrences < occurrenceIndex)
                        {
                            occurrences++;
                            continue;
                        }

                        return Literal.Create((Int128)offset);
                    }
                }

                throw new Exception("failed to find sequence");
            });
        }


        private static string ReplaceFirst(string text, string search, string replace)
        {
            var pos = text.IndexOf(search, StringComparison.Ordinal);
            return pos < 0 ? text : $"{text[..pos]}{replace}{text[(pos + search.Length)..]}";
        }

        private static string Format(Evaluator ctx, IReadOnlyList<Literal> args)
        {
            var format = args[0].ToString()!;

            for (var i = 1; i < args.Count; i++)
            {
                var arg = args[i];
                var value = arg is PatternDataLiteral pattern
                    ? pattern.Value.ToString(ctx.Buffer)
                    : arg.ToString() ?? "";

                format = ReplaceFirst(format, "{}", value);
            }

            return format;
        }

        public static void RegisterFunction(string ns, string name, uint parameterCount, PatternFunctionBody body)
        {
            Functions.Add($"{ns}::{name}", new Function(parameterCount, body, false));
        }

        public static void AddDangerousFunction(string ns, string name, uint parameterCount, PatternFunctionBody body)
        {
            Functions.Add($"{ns}::{name}", new Function(parameterCount, body, true));
        }

        private static string GetFunctionName(IEnumerable<string> ns, string name)
        {
            var functionName = "";
            foreach (var scope in ns)
            {
                functionName += $"{scope}::";
            }

            functionName += name;

            return functionName;
        }

        public record Function(uint ParameterCount, PatternFunctionBody Body, bool Dangerous);
    }
}