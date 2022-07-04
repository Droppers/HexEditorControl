using System.ComponentModel;
using System.Runtime.CompilerServices;
using HexControl.Core;
using HexControl.SharedControl.Control;
using HexControl.WinForms.Host;
using HexControl.WinForms.Host.Controls;

namespace HexControl.WinForms;

internal class PropertyChangedEventArgs : EventArgs
{
    public PropertyChangedEventArgs(string? propertyName, object? value)
    {
        PropertyName = propertyName;
        Value = value;
    }

    public string? PropertyName { get; }
    public object? Value { get; }
}

public partial class HexEditorControl : UserControl
{
    private readonly HexControlPropertyMapper _mapper;

    public HexEditorControl()
    {
        PropertyChanged += OnPropertyChanged;

        var control = new SharedHexControl();
        var factory = new WinFormsNativeFactory();
        _mapper = new HexControlPropertyMapper(control, factory);

        AutoScaleMode = AutoScaleMode.Dpi;
        InitializeComponent();

        var host = new WinFormsHost(d2dControl)
        {
            {SharedHexControl.VerticalScrollBarName, new WinFormsScrollBar(sbVertical)},
            {SharedHexControl.HorizontalScrollBarName, new WinFormsScrollBar(sbHorizontal)},
            {SharedHexControl.FakeTextBoxName, new WinFormsTextBox(txtFake)}
        };

        control.AttachHost(host);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Document? Document
    {
        get => _mapper.GetValueNullable<Document?>(null);
        set => OnPropertyChanged(value);
    }

    private event EventHandler<PropertyChangedEventArgs>? PropertyChanged;

    private async void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null)
        {
            return;
        }

        await _mapper.SetValueAsync(e.PropertyName, e.Value);
    }

    private void OnPropertyChanged(object? value, [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName, value));
    }
}