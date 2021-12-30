using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using HexControl.Core;

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
        Document = Document.FromFile(@"..\..\..\..\..\..\files\sample-binary");
    }
}