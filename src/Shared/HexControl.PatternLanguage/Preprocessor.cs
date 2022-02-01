using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HexControl.PatternLanguage.Exceptions;

namespace HexControl.PatternLanguage;

internal class Preprocessor
{
    private readonly List<(string name, string value, int lineNumber)> _defines;
    private readonly StringBuilder _output;
    private readonly Dictionary<string, Func<string, bool>> _pragmaHandlers;
    private readonly List<(string name, string value, int lineNumber)> _pragmas;

    private PreprocessorContext? _context;
    private bool _isInString;
    private int _lineNumber = 1;
    private int _offset;
    private bool _startOfLine = true;

    public Preprocessor()
    {
        IncludePaths = new List<string>();
        _output = new StringBuilder();

        _defines = new List<(string, string, int)>();
        _pragmas = new List<(string, string, int)>();
        _pragmaHandlers = new Dictionary<string, Func<string, bool>>();
    }

    // ReSharper disable once CollectionNeverUpdated.Global
    public List<string> IncludePaths { get; set; }

    public void AddPragmaHandler(string name, Func<string, bool> handler)
    {
        _pragmaHandlers[name] = handler;
    }

    public string Preprocess(ReadOnlySpan<char> code) => Preprocess(code, new PreprocessorContext(), true);

    private string Preprocess(ReadOnlySpan<char> code, PreprocessorContext context, bool root = false)
    {
        _context = context;

        Reset();

        while (_offset < code.Length)
        {
            if (_offset > 0 && code[_offset - 1] != '\\' && code[_offset] == '\"')
            {
                _isInString = !_isInString;
            }
            else if (_isInString)
            {
                _output.Append(code[_offset]);
                _offset += 1;
                continue;
            }

            if (code[_offset] == '#' && _startOfLine)
            {
                _offset += 1;

                if (code.SafeSubString(_offset, 7).SequenceEqual("include"))
                {
                    _offset += 7;
                    HandleInclude(code);
                }
                else if (code.SafeSubString(_offset, 6).SequenceEqual("define"))
                {
                    _offset += 6;
                    HandleDefine(code);
                }
                else if (code.SafeSubString(_offset, 6).SequenceEqual("pragma"))
                {
                    _offset += 6;
                    HandlePragma(code);
                }
                else
                {
                    throw new PreprocessorException("Unknown preprocessor directive", _lineNumber);
                }
            }
            else if (code.SafeSubString(_offset, 2).SequenceEqual("//"))
            {
                while (code[_offset] != '\n' && _offset < code.Length)
                {
                    _offset += 1;
                }
            }
            else if (code.SafeSubString(_offset, 2).SequenceEqual("/*"))
            {
                while (!code.SafeSubString(_offset, 2).SequenceEqual("*/") && _offset < code.Length)
                {
                    if (code[_offset] == '\n')
                    {
                        _output.Append('\n');
                        _lineNumber++;
                    }

                    _offset += 1;
                }

                _offset += 2;
                if (_offset >= code.Length)
                {
                    throw new PreprocessorException("Unterminated comment", _lineNumber);
                }
            }

            if (code[_offset] == '\n')
            {
                _lineNumber++;
                _startOfLine = true;
            }
            else if (!char.IsWhiteSpace(code[_offset]))
            {
                _startOfLine = false;
            }

            _output.Append(code[_offset]);
            _offset += 1;
        }

        if (root)
        {
            return HandleRoot();
        }

        return _output.ToString();
    }

    private void Reset()
    {
        _lineNumber = 1;
        _startOfLine = true;
        _offset = 0;
        _defines.Clear();
        _pragmas.Clear();
        _output.Clear();
    }

    private string HandleRoot()
    {
        _defines.Sort((a, b) => a.name.Length.CompareTo(b.name.Length));
        var outputString = _output.ToString();

        foreach (var (define, value, _) in _defines)
        {
            outputString = outputString.Replace(define, value);
        }

        // Handle pragmas
        foreach (var (type, value, _) in _pragmas)
        {
            if (!_pragmaHandlers.TryGetValue(type, out var handler))
            {
                throw new PreprocessorException($"No #pragma handler registered for type {type}", _lineNumber);
            }


            if (!handler.Invoke(value))
            {
                throw new PreprocessorException($"Invalid value provided to '{type}' #pragma directive", _lineNumber);
            }
        }

        return outputString;
    }

    private void SkipWhitespace(ReadOnlySpan<char> code)
    {
        while (char.IsWhiteSpace(code[_offset]))
        {
            _offset += 1;
        }
    }

    private void HandleInclude(ReadOnlySpan<char> code)
    {
        SkipWhitespace(code);

        if (code[_offset] != '<' && code[_offset] != '"')
        {
            throw new PreprocessorException("Expected '<' or '\"' before file name", _lineNumber);
        }

        var endChar = code[_offset];
        if (endChar == '<')
        {
            endChar = '>';
        }

        _offset += 1;

        var includeFile = "";
        while (code[_offset] != endChar)
        {
            includeFile += code[_offset];
            _offset += 1;

            if (_offset >= code.Length)
            {
                throw new PreprocessorException($"Missing terminating '{endChar}' character", _lineNumber);
            }
        }

        _offset += 1;

        var includePath = includeFile;
        if (includeFile[0] != '/')
        {
            foreach (var tempPath in IncludePaths.Select(dir => Path.Join(dir, includePath)).Where(File.Exists))
            {
                includePath = tempPath;
                break;
            }
        }

        // Already included by a previous file
        if (_context!.IncludedPaths.Contains(includePath))
        {
            return;
        }

        try
        {
            var preprocessedInclude = new Preprocessor().Preprocess(File.ReadAllText(includePath), _context!);
            _output.Append(preprocessedInclude.Replace("\n", "").Replace("\r", ""));
            _context.IncludedPaths.Add(includePath);
        }
        catch (IOException)
        {
            throw new PreprocessorException($"{includeFile}: no such file or directory", _lineNumber);
        }
    }

    private void HandleDefine(ReadOnlySpan<char> code)
    {
        SkipWhitespace(code);

        var defineName = new StringBuilder();
        while (!char.IsWhiteSpace(code[_offset]))
        {
            defineName.Append(code[_offset]);

            if (_offset >= code.Length || code[_offset] == '\n' || code[_offset] == '\r')
            {
                throw new PreprocessorException("No value given in #define directive", _lineNumber);
            }

            _offset += 1;
        }

        while (char.IsWhiteSpace(code[_offset]))
        {
            _offset += 1;
            if (_offset >= code.Length)
            {
                throw new PreprocessorException("No value given in #define directive", _lineNumber);
            }
        }

        var replaceValue = new StringBuilder();
        while (code[_offset] is not '\n' and not '\r')
        {
            if (_offset >= code.Length)
            {
                throw new PreprocessorException("Missing new line after #define directive", _lineNumber);
            }

            replaceValue.Append(code[_offset]);
            _offset += 1;
        }

        if (replaceValue.Length is 0)
        {
            throw new PreprocessorException("No value given in #define directive", _lineNumber);
        }

        _defines.Add((defineName.ToString(), replaceValue.ToString(), _lineNumber));
    }

    private void HandlePragma(ReadOnlySpan<char> code)
    {
        while (char.IsWhiteSpace(code[_offset]))
        {
            _offset += 1;

            if (code[_offset] == '\n' || code[_offset] == '\r')
            {
                throw new PreprocessorException("no instruction given in #pragma directive", _lineNumber);
            }
        }

        var pragmaKey = new StringBuilder();
        while (!char.IsWhiteSpace(code[_offset]) && code[_offset] != '\n' && code[_offset] != '\r')
        {
            pragmaKey.Append(code[_offset]);

            if (_offset >= code.Length)
            {
                throw new PreprocessorException("no instruction given in #pragma directive", _lineNumber);
            }

            _offset += 1;
        }

        SkipWhitespace(code);

        var pragmaValue = new StringBuilder();
        while (code[_offset] != '\n' && code[_offset] != '\r')
        {
            if (_offset >= code.Length)
            {
                throw new PreprocessorException("missing new line after #pragma directive", _lineNumber);
            }

            pragmaValue.Append(code[_offset]);
            _offset += 1;
        }

        _pragmas.Add((pragmaKey.ToString(), pragmaValue.ToString(), _lineNumber));
    }

    private class PreprocessorContext
    {
        public PreprocessorContext()
        {
            IncludedPaths = new HashSet<string>();
        }

        public HashSet<string> IncludedPaths { get; }
    }
}