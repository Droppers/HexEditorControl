using System.Drawing;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using HexControl.SharedControl.Documents;
#if DEBUG
using Avalonia;
#endif

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

        var config = new DocumentConfiguration
        {
            OffsetBase = NumberBase.Decimal
        };
        var bytes = File.ReadAllBytes(@"C:\Users\joery\Downloads\MemProfilerInstaller5_7_26.exe");

        Document = Document.FromBytes(bytes, configuration: config);

        for (var i = 0; i < 100_000; i++)
        {
            Document.AddMarker(new Marker(i * 12_000, 10_000)
            {
                Background = Color.FromArgb(120, 200, 0, 123),
                Column = ColumnSide.Both,
                BehindText = true,
                Foreground = Color.Green
            });
        }
        //Document = Document.FromFile(@"C:\Users\joery\Downloads\MemProfilerInstaller5_7_26.exe", FileOpenMode.ReadWrite, ChangeTracking.None);
    }
}