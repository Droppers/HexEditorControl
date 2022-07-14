using System.ComponentModel;
using HexControl.SharedControl.Control;
using HexControl.WinForms.Host.Controls;
using HexControl.WinForms.Host;
using System.Runtime.CompilerServices;
using HexControl.SharedControl.Documents;
using JetBrains.Annotations;

namespace HexControl.WinForms;

[PublicAPI]
[ToolboxItem(true)]
public class HexEditorControl : UserControl
{
    private readonly float _startupDpi;
    private readonly HexControlPropertyMapper _mapper;
    private readonly TableLayoutPanel _tableLayoutPanel;
    private readonly VScrollBar _verticalScrollBar;
    private readonly HScrollBar _horizontalScrollBar;

    public HexEditorControl()
    {
        AutoScaleMode = AutoScaleMode.Dpi;

        _tableLayoutPanel = new TableLayoutPanel
        {
            RowCount = 2,
            ColumnCount = 2,
            RowStyles = { new RowStyle(SizeType.Percent, 100), new RowStyle(SizeType.AutoSize) },
            ColumnStyles = { new ColumnStyle(SizeType.Percent, 100), new ColumnStyle(SizeType.AutoSize) },
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };
        Controls.Add(_tableLayoutPanel);

        var d2dControl = new D2DControl
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        _tableLayoutPanel.Controls.Add(d2dControl, 0, 0);

        _verticalScrollBar = new VScrollBar
        {
            Dock = DockStyle.Fill,
            Width = 17
        };
        _tableLayoutPanel.Controls.Add(_verticalScrollBar, 1, 0);

        _horizontalScrollBar = new HScrollBar
        {
            Dock = DockStyle.Fill,
            Height = 17
        };
        _tableLayoutPanel.Controls.Add(_horizontalScrollBar, 0, 1);

        var textBox = new TextBox();
        Controls.Add(textBox);

        PropertyChanged += OnPropertyChanged;
        var control = new SharedHexControl();
        control.ScrollBarVisibilityChanged += OnScrollBarVisibilityChanged;
        var factory = new WinFormsNativeFactory();
        _mapper = new HexControlPropertyMapper(control, factory);

        var host = new WinFormsHost(d2dControl)
        {
            {SharedHexControl.VerticalScrollBarName, new WinFormsScrollBar(_verticalScrollBar)},
            {SharedHexControl.HorizontalScrollBarName, new WinFormsScrollBar(_horizontalScrollBar)},
            {SharedHexControl.FakeTextBoxName, new WinFormsTextBox(textBox)}
        };

        control.AttachHost(host);

        _startupDpi = DeviceDpi / 96f;
        ResizeScrollBars();
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        ResizeScrollBars();
    }

    private void ResizeScrollBars()
    {
        var currentDpi = DeviceDpi / 96f;
        _horizontalScrollBar.Height = (int)(SystemInformation.HorizontalScrollBarHeight / _startupDpi * currentDpi);
        _verticalScrollBar.Width = (int)(SystemInformation.VerticalScrollBarWidth / _startupDpi * currentDpi);
    }

    private void OnScrollBarVisibilityChanged(object? sender, ScrollBarVisibilityChangedEventArgs e)
    {
        switch (e.ScrollBar)
        {
            case SharedScrollBar.Horizontal:
                _tableLayoutPanel.RowStyles[1].Height = 0;
                _tableLayoutPanel.RowStyles[1].SizeType = e.Visible ? SizeType.AutoSize : SizeType.Absolute;
                break;
            case SharedScrollBar.Vertical:
                _tableLayoutPanel.ColumnStyles[1].Width = 0;
                _tableLayoutPanel.ColumnStyles[1].SizeType = e.Visible ? SizeType.AutoSize : SizeType.Absolute;
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