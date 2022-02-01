using System;
using System.Collections.Generic;
using System.Threading;
using HexControl.Core;
using HexControl.Core.Buffers;
using HexControl.Core.Buffers.Extensions;
using HexControl.PatternLanguage.Helpers;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage;

public class PatternRunner
{
    private readonly BaseBuffer _buffer;
    private readonly Document? _document;

    public PatternRunner(Document document)
    {
        _document = document;
        _buffer = _document.Buffer;
    }

    public PatternRunner(BaseBuffer buffer)
    {
        _buffer = buffer;
    }

    public IReadOnlyList<PatternData> Run(ReadOnlySpan<char> code, CancellationToken cancellationToken = default)
    {
        var evaluator = new Evaluator();
        var preprocessor = new Preprocessor();
        RegisterPreprocessorPragmas(preprocessor, evaluator);
        code = preprocessor.Preprocess(code);

        var lexer = new Lexer();
        var tokens = lexer.Lex(code);

        var parser = new Parser();
        var nodes = parser.Parse(tokens);

        var patterns = evaluator.Evaluate(_buffer, nodes);

        if (_document is not null)
        {
            ApplyPatterns(_document, patterns);
        }

        return patterns;
    }

    private static void RegisterPreprocessorPragmas(Preprocessor preprocessor, Evaluator evaluator)
    {
        preprocessor.AddPragmaHandler("endian", value =>
        {
            switch (value)
            {
                case "big":
                    evaluator.DefaultEndian = Endianess.Big;
                    return true;
                case "little":
                    evaluator.DefaultEndian = Endianess.Little;
                    return true;
                case "native":
                    evaluator.DefaultEndian = Endianess.Native;
                    return true;
                default:
                    return false;
            }
        });

        preprocessor.AddPragmaHandler("eval_depth", value =>
        {
            var limit = ConversionHelper.ParseIntAgnostic(value);

            if (limit <= 0)
            {
                return false;
            }

            evaluator.EvaluationDepth = limit;
            return true;
        });

        preprocessor.AddPragmaHandler("array_limit", value =>
        {
            var limit = ConversionHelper.ParseIntAgnostic(value);

            if (limit <= 0)
            {
                return false;
            }

            evaluator.ArrayLimit = limit;
            return true;
        });

        preprocessor.AddPragmaHandler("pattern_limit", value =>
        {
            var limit = ConversionHelper.ParseIntAgnostic(value);

            if (limit <= 0)
            {
                return false;
            }

            evaluator.PatternLimit = limit;
            return true;
        });

        preprocessor.AddPragmaHandler("loop_limit", value =>
        {
            var limit = ConversionHelper.ParseIntAgnostic(value);

            if (limit <= 0)
            {
                return false;
            }

            evaluator.LoopLimit = limit;
            return true;
        });

        preprocessor.AddPragmaHandler("base_address", value =>
        {
            var baseAddress = ConversionHelper.ParseIntAgnostic(value);
            throw new NotSupportedException("Buffer does not yet support base address.");
            return true;
        });
    }

    private static void ApplyPatterns(Document document, IReadOnlyList<PatternData> patterns)
    {
        var markers = new StaticMarkerProvider();
        for (var i = 0; i < patterns.Count; i++)
        {
            patterns[i].CreateMarkers(markers);
        }

        markers.Complete();

        document.StaticMarkerProvider = markers;
    }
}