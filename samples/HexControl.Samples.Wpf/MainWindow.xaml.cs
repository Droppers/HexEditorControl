using System.Windows;
using HexControl.Core;
using HexControl.Core.Buffers;

namespace HexControl.Samples.Wpf;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        
        Document = Document.FromFile(@"C:\Users\joery\Downloads\MemProfilerInstaller5_7_26.exe", FileOpenMode.ReadOnly);
    }

    public Document Document { get; set; }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Document.Buffer.Undo();
        }
        catch
        {
            // ignore
        }
    }

    private void Clear_OnClick(object sender, RoutedEventArgs e)
    {
        EditorControl.Document = null!;
    }

    private void Restore_OnClick(object sender, RoutedEventArgs e)
    {
        EditorControl.Document = Document;
    }

    private void Insert_OnClick(object sender, RoutedEventArgs e)
    {
        Document.Buffer.Insert(Document.Caret.Offset, new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10});
    }

    private async void Save_OnClick(object sender, RoutedEventArgs e)
    {
        await Document.Buffer.SaveAsync();
    }
}