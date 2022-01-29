using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using HexControl.Core;
using HexControl.PatternLanguage;
using System.Collections.Generic;
using System.IO;

namespace HexControl.Samples.Avalonia;

public class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        DataContext = this;
    }

    public Document? Document { get; set; }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        //Document = Document.FromFile(@"..\..\..\..\..\files\sample-binary");

        var code = File.ReadAllText(@"C:\Users\joery\Downloads\pe.hexpat");
        Document = Document.FromFile(@"C:\Users\joery\Downloads\MemProfilerInstaller5_7_26.exe");
        var parsed = LanguageParser.Parse(code);
        var eval = new Evaluator();
        eval.EvaluationDepth = 9999;
        eval.ArrayLimit = 100000;
        var patterns = eval.Evaluate(Document.Buffer, parsed);

        var markers = new List<PatternMarker>();
        foreach (var pattern in patterns)
        {
            pattern.CreateMarkers(markers);
        }

        foreach (var marker in markers)
        {
            Document.AddMarker(marker);
        }
    }
}