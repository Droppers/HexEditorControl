using System.IO;
using System.Windows.Forms;
using HexControl.Core;

namespace HexControl.Samples.WinForms;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();

        var document = Document.FromFile(@"..\..\..\..\..\..\files\sample-binary");
        hexEditorControl2.Document = document;
    }
}