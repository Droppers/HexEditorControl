using System.ComponentModel;
using System.Runtime.CompilerServices;
using HexControl.SharedControl.Control;
using HexControl.SharedControl.Documents;
using HexControl.WinForms.Host;
using HexControl.WinForms.Host.Controls;
using JetBrains.Annotations;

namespace HexControl.WinForms;

[PublicAPI]
[ToolboxItem(true)]
public partial class HexEditorControl : UserControl
{
    private readonly HexControlPropertyMapper _mapper;

    public HexEditorControl()
    {
        PropertyChanged += OnPropertyChanged;

        var control = new SharedHexControl();
        control.ScrollBarVisibilityChanged += OnScrollBarVisibilityChanged;
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

    private void OnScrollBarVisibilityChanged(object? sender, ScrollBarVisibilityChangedEventArgs e)
    {
        switch (e.ScrollBar)
        {
            case SharedScrollBar.Horizontal:
                tlpGrid.RowStyles[1].Height = 0;
                tlpGrid.RowStyles[1].SizeType = e.Visible ? SizeType.AutoSize : SizeType.Absolute;
                break;
            case SharedScrollBar.Vertical:
                tlpGrid.ColumnStyles[1].Width = 0;
                tlpGrid.ColumnStyles[1].SizeType = e.Visible ? SizeType.AutoSize : SizeType.Absolute;
                break;
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Document? Document
    {
        get => _mapper.GetValueNullable<Document?>();
        set => OnPropertyChanged(value);
    }

    public new Color BackColor
    {
        get => _mapper.GetValue<Color>(nameof(SharedHexControl.Background));
        set
        {
            base.BackColor = value;
            OnPropertyChanged(value, nameof(SharedHexControl.Background));
        }
    }

    public Color HeaderForeColor
    {
        get => _mapper.GetValue<Color>(nameof(SharedHexControl.HeaderForeground));
        set => OnPropertyChanged(value, nameof(SharedHexControl.HeaderForeground));
    }

    public Color OffsetForeColor
    {
        get => _mapper.GetValue<Color>(nameof(SharedHexControl.OffsetForeground));
        set => OnPropertyChanged(value, nameof(SharedHexControl.OffsetForeground));
    }

    public new Color ForeColor
    {
        get => _mapper.GetValue<Color>(nameof(SharedHexControl.Foreground));
        set
        {
            base.ForeColor = value;
            OnPropertyChanged(value, nameof(SharedHexControl.Foreground));
        }
    }

    public Color EvenForeColor
    {
        get => _mapper.GetValue<Color>(nameof(SharedHexControl.EvenForeground));
        set => OnPropertyChanged(value, nameof(SharedHexControl.EvenForeground));
    }

    public string OffsetHeader
    {
        get => _mapper.GetValue<string>();
        set => OnPropertyChanged(value);
    }

    public string TextHeader
    {
        get => _mapper.GetValue<string>();
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
}