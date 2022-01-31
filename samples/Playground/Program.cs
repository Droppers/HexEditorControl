using System;
using System.Diagnostics;
using System.IO;
using HexControl.Core;
using HexControl.PatternLanguage;

//using IntervalTree;

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

            var code = File.ReadAllText(@"Patterns\memory_test.hexpat");
            var document = Document.FromFile(@"C:\Users\joery\Downloads\pad000001.meta");

            //var code = File.ReadAllText(@"Patterns\pe.hexpat");
            //var document = Document.FromFile(@"C:\Users\joery\Downloads\windirstat1_1_2_setup.exe");

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
            var sw = new Stopwatch();
            sw.Start();

            var eval = new Evaluator();
            eval.EvaluationDepth = 9999;
            eval.ArrayLimit = 1000000000;
            eval.PatternLimit = 99999999999999;
            //eval.DefaultEndian = Endianess.Big;
            var patterns = eval.Evaluate(buffer, parsed);

            sw.Stop();
            Console.WriteLine($"Time: {sw.ElapsedMilliseconds}");

            var markers = new StaticMarkerProvider();
            foreach (var pattern in patterns)
            {
                pattern.CreateMarkers(markers);
            }

            markers.Complete();

            Console.Read();
        }
    }
}