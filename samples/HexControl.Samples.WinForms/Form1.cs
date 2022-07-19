﻿using HexControl.Buffers;
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
            OffsetBase = NumberBase.Decimal
        };
        var document = Document.FromBytes(bytes, configuration: config, changeTracking: ChangeTracking.Undo);
        hexEditorControl1.Document = document;
    }
}