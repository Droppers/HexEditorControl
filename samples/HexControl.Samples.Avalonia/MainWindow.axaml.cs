using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using HexControl.Core;
using HexControl.Core.Buffers;
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

        Document = Document.FromFile(@"C:\Users\joery\Downloads\MemProfilerInstaller5_7_26.exe", FileOpenMode.ReadOnly);
    }
}