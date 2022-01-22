using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexControl.PatternLanguage.AST;

namespace HexControl.PatternLanguage
{
    public class ParsedLanguage
    {
        internal ParsedLanguage(IReadOnlyList<ASTNode> nodes)
        {
            Nodes = nodes;
        }

        internal IReadOnlyList<ASTNode> Nodes { get; }

        public int NodeCount => Nodes.Count;
    }
    public static class LanguageParser
    {
        public static ParsedLanguage Parse(string code)
        {
            var lexer = new Lexer();
            var result = lexer.Lex(Preprocess(code));

            var parser = new Parser();
            var ast = parser.Parse(result);
            if (ast is null)
            {
                throw new Exception("Parsing did not result in any nodes.");
            }

            return new ParsedLanguage(ast);
        }

        private static string Preprocess(ReadOnlySpan<char> code)
        {
            var lineNumber = 0;
            var startOfLine = false;
            var output = new StringBuilder();
            var offset = 0;
            while (offset < code.Length)
            {
                if (code.SafeSubString(offset, 2).SequenceEqual("//"))
                {
                    while (code[offset] != '\n' && offset < code.Length)
                    {
                        offset += 1;
                    }
                }
                else if (code.SafeSubString(offset, 2).SequenceEqual("/*"))
                {
                    while (!code.SafeSubString(offset, 2).SequenceEqual("*/") && offset < code.Length)
                    {
                        if (code[offset] == '\n')
                        {
                            output.Append('\n');
                            lineNumber++;
                        }

                        offset += 1;
                    }

                    offset += 2;
                    if (offset >= code.Length)
                    {
                        throw new Exception("unterminated comment");
                    }
                }

                if (code[offset] == '\n')
                {
                    lineNumber++;
                    startOfLine = true;
                }
                else if (!char.IsWhiteSpace(code[offset]))
                {
                    startOfLine = false;
                }

                output.Append(code[offset]);
                offset += 1;
            }

            return output.ToString();
        }
    }
}
