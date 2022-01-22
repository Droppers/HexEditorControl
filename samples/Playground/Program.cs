using System;
using System.IO;
using HexControl.Core;
using HexControl.Core.Buffers;
using HexControl.Core.Buffers.Extensions;
using HexControl.PatternLanguage;
using HexControl.PatternLanguage.Visualization;

namespace Playground
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            //var code = File.ReadAllText(@"Patterns\zip.hexpat");
            //var document = Document.FromFile(@"C:\Users\joery\Downloads\heapview-2021.3.0.zip");

            //var code = File.ReadAllText(@"Patterns\java_class.hexpat");
            //var document = Document.FromFile(@"C:\Users\joery\Downloads\FieldNamingStrategy.class");

            //var code = File.ReadAllText(@"Patterns\pe.hexpat");
            //var document = Document.FromFile(@"C:\Users\joery\Downloads\python-3.9.0-amd64.exe");


            var code = File.ReadAllText(@"Patterns\elf.hexpat");
            var document = Document.FromFile(@"C:\Users\joery\Downloads\elf-Linux-ARM64-bash");

            //
            var buffer = document.Buffer;

            //for (var i = 0; i < 100; i++)
            //{
                var parsed = LanguageParser.Parse(code);
            //}

            for (var i = 0; i < 200; i++)
            {
                var eval = new Evaluator();
                eval.SetBuffer(buffer);
                eval.SetEvaluationDepth(9999);
                eval.SetArrayLimit(100000);
                //eval.SetDefaultEndian(Endianess.Big);
                var patterns = eval.Evaluate(parsed);
            }

            //new ConsoleVisualizer().Visualize(patterns);

            Console.WriteLine("Hello World!");
        }
    }
}
