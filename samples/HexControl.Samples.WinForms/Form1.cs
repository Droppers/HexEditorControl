using HexControl.Buffers;
using HexControl.SharedControl.Documents;

namespace HexControl.Samples.WinForms;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();

        var fileName = @"..\..\..\..\..\files\sample-binary";
        var bytes = File.ReadAllBytes(fileName);
        var document = Document.FromBytes(bytes);

        var buffer = new FileBuffer(fileName, FileOpenMode.ReadWrite);
        document.ReplaceBuffer(buffer);
        //var document = Document.FromFile(fileName);
        hexEditorControl2.Document = document;

        AutoScaleMode = AutoScaleMode.Dpi;
    }
}