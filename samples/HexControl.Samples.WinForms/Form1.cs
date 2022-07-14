﻿using HexControl.SharedControl.Documents;

namespace HexControl.Samples.WinForms;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();

        var fileName = @"..\..\..\..\..\files\sample-binary";
        var bytes = File.ReadAllBytes(fileName);
        var config = new DocumentConfiguration();
        config.WriteInsert = true;
        var document = Document.FromBytes(bytes, configuration: config);
        hexEditorControl1.Document = document;
    }
}