using HexControl.SharedControl.Documents;
using HexControl.WinForms;

namespace HexControl.Samples.WinForms;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();

        var fileName = @"..\..\..\..\..\files\sample-binary";
        var bytes = File.ReadAllBytes(fileName);
        var document = Document.FromBytes(bytes);
        hexEditorControl1.Document = document;
    }
}