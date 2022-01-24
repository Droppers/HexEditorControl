using System;
using System.Collections.Generic;
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

            var code = File.ReadAllText(@"Patterns\java_class.hexpat");
            var document = Document.FromFile(@"C:\Users\joery\Downloads\FieldNamingStrategy.class");

            //var code = File.ReadAllText(@"Patterns\pe.hexpat");
            //var document = Document.FromFile(@"C:\Users\joery\Downloads\gpg4win-4.0.0.exe");

            //var code = File.ReadAllText(@"Patterns\elf.hexpat");
            //var document = Document.FromFile(@"C:\Users\joery\Downloads\elf-Linux-ARM64-bash");

            //
            var buffer = document.Buffer;

            //for (var i = 0; i < 200; i++)
            //{
                var parsed = LanguageParser.Parse(code);
            //}

            //for (var i = 0; i < 200; i++)
            //{
                var eval = new Evaluator();
                eval.EvaluationDepth = 9999;
                eval.ArrayLimit = 100000;
                eval.DefaultEndian = Endianess.Big;
                var patterns = eval.Evaluate(buffer, parsed);

                var markers = new List<Marker>();
                foreach (var pattern in patterns)
                {
                    pattern.CreateMarkers(markers);
                }
            //}

            //new ConsoleVisualizer().Visualize(patterns);

            Console.WriteLine("Hello World!");
        }
    }
}
