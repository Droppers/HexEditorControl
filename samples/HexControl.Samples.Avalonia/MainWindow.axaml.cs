using System.Drawing;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using HexControl.SharedControl.Characters;
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
            OffsetBase = NumberBase.Decimal,
            ColumnsVisible = VisibleColumns.DataText,
            WriteMode = WriteMode.Insert,
            DataCharacterSet = new NumberCharacterSet(NumberType.Int32),
            GroupSize = 1
        };
        var bytes = File.ReadAllBytes(@"C:\Users\joery\Downloads\MemProfilerInstaller5_7_26.exe");
        Document = Document.FromBytes(bytes, configuration: config);

        Document.AddMarker(new Marker(2, 23)
        {
            Background = Color.FromArgb(255, 0, 0, 0),
            Column = MarkerColumn.DataText,
            BehindText = true,
            Foreground = Color.White
        });

        //for (var i = 0; i < 10_000; i++)
        //{
        //    Document.AddMarker(new Marker(i * 200, 100)
        //    {
        //        Background = Color.FromArgb(255, 0, 0, 0),
        //        Column = MarkerColumn.Text,
        //        BehindText = true,
        //        Foreground = Color.White
        //    });
        //}
        //Document = Document.FromFile(@"C:\Users\joery\Downloads\MemProfilerInstaller5_7_26.exe", FileOpenMode.ReadWrite, ChangeTracking.None);
    }
}