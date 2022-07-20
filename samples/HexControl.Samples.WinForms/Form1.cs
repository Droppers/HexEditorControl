using HexControl.Buffers;
using HexControl.SharedControl.Characters;
using HexControl.SharedControl.Documents;

namespace HexControl.Samples.WinForms;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();

        var fileName = @"..\..\..\..\..\files\sample-binary";
        var bytes = File.ReadAllBytes(fileName);
        var config = new DocumentConfiguration
        {
            WriteMode = WriteMode.Insert,
            OffsetBase = NumberBase.Decimal,
            GroupSize = 1,
            DataCharacterSet = new OctalCharacterSet()
        };
        var document = Document.FromBytes(bytes, configuration: config, changeTracking: ChangeTracking.Undo);
        hexEditorControl1.Document = document;
    }
}