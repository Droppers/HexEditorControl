using HexControl.Core;
using Microsoft.UI.Xaml;

namespace HexControl.Samples.WinUI;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Document = Document.FromFile(@"C:\_dev\vs\HexEditorControl\files\sample-binary");
    }

    public Document Document { get; set; }
}