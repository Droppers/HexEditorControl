using HexControl.SharedControl.Documents;

namespace HexControl.Samples.WinUI;

public sealed partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();

        Title = "WinUI";
        Document = Document.FromFile(@"C:\_dev\vs\HexEditorControl\files\sample-binary");
    }

    public Document Document { get; set; }
}