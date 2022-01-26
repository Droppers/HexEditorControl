using System;
using System.Collections.Generic;
using System.Linq;
using HexControl.Core.Numerics;
using HexControl.PatternLanguage.Functions;
using HexControl.PatternLanguage.Literals;

namespace HexControl.PatternLanguage.Standard;

internal class StdMemNamespace : IFunctionNamespace
{
    public override FunctionNamespace Namespace => new("std", "mem");

    protected override void RegisterFunctions()
    {
        Register("find_sequence", FunctionParameterCount.AtLeast(2), FindSequence);
    }

    private static Literal? FindSequence(Evaluator ctx, IReadOnlyList<Literal> parameters)
    {
        {
            var occurrenceIndex = parameters[0].ToInt128();

            var sequence = new List<byte>();
            for (var i = 1; i < parameters.Count; i++)
            {
                var @byte = parameters[i].ToUInt128();

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

                if (!bytes.SequenceEqual(sequence))
                {
                    continue;
                }

                if (occurrences < occurrenceIndex)
                {
                    occurrences++;
                    continue;
                }

                return (Int128)offset;
            }

            throw new Exception("failed to find sequence");
        }
    }
}