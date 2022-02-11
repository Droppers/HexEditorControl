using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using HexControl.Avalonia.Host;
using HexControl.Avalonia.Host.Controls;
using HexControl.Core;
using HexControl.SharedControl.Control;

namespace HexControl.Avalonia;

public class HexEditorControl : UserControl
{
    public static readonly StyledProperty<Document?> DocumentProperty =
        AvaloniaProperty.Register<HexEditorControl, Document?>(nameof(Document));

    public new static readonly StyledProperty<IBrush> ForegroundProperty =
        AvaloniaProperty.Register<HexEditorControl, IBrush>(nameof(Foreground));

    public static readonly StyledProperty<IBrush> OddForegroundProperty =
        AvaloniaProperty.Register<HexEditorControl, IBrush>(nameof(OddForeground));

    private readonly SharedHexControl _control;
    private readonly HexControlPropertyMapper _mapper;
    private AvaloniaControl? _host;

    private TextBox? _fakeTextBox;
    private ScrollBar? _horizontalScrollBar;
    private ScrollBar? _verticalScrollBar;
    private Grid? _grid;

    static HexEditorControl()
    {
        DocumentProperty.Changed.AddClassHandler<HexEditorControl>(OnPropertyChanged);
        ForegroundProperty.Changed.AddClassHandler<HexEditorControl>(OnPropertyChanged);
        OddForegroundProperty.Changed.AddClassHandler<HexEditorControl>(OnPropertyChanged);
    }

    public HexEditorControl()
    {
        _control = new SharedHexControl();
        _control.ScrollBarVisibilityChanged += OnScrollBarVisibilityChanged;

        var factory = new AvaloniaNativeFactory();
        _mapper = new HexControlPropertyMapper(_control, factory);

        InitializeComponent();
    }

    private void OnScrollBarVisibilityChanged(object? sender, ScrollBarVisibilityChangedEventArgs e)
    {
        if (_grid is null)
        {
            return;
        }

        if (e.ScrollBar is SharedScrollBar.Horizontal)
        {
            var row = _grid.RowDefinitions[1];
            row.Height = e.Visible ? GridLength.Auto : new GridLength(0);
        }
        else
        {
            var column = _grid.ColumnDefinitions[1];
            column.Width = e.Visible ? GridLength.Auto : new GridLength(0);
        }
    }

    public Document? Document
    {
        get => _mapper.GetValueNullable<Document?>(GetValue(DocumentProperty));
        set => SetValue(DocumentProperty, value);
    }

    public IBrush OddForeground
    {
        get => _mapper.GetValue<IBrush>(GetValue(OddForegroundProperty));
        set => SetValue(OddForegroundProperty, value);
    }

    public new IBrush Foreground
    {
        get => _mapper.GetValue<IBrush>(GetValue(ForegroundProperty));
        set => SetValue(ForegroundProperty, value);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        _verticalScrollBar = this.FindControl<ScrollBar>(SharedHexControl.VerticalScrollBarName);
        _horizontalScrollBar = this.FindControl<ScrollBar>(SharedHexControl.HorizontalScrollBarName);
        _fakeTextBox = this.FindControl<TextBox>(SharedHexControl.FakeTextBoxName);

        _grid = this.FindControl<Grid>("Container");

        if (_verticalScrollBar is null || _horizontalScrollBar is null || _fakeTextBox is null)
        {
            return;
        }

        _host = new AvaloniaControl(this)
        {
            {SharedHexControl.VerticalScrollBarName, new AvaloniaScrollBar(_verticalScrollBar)},
            {SharedHexControl.HorizontalScrollBarName, new AvaloniaScrollBar(_horizontalScrollBar)},
            {SharedHexControl.FakeTextBoxName, new AvaloniaTextBox(_fakeTextBox)}
        };
        _control.AttachHost(_host);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        _host?.DoRender(context);
    }

    private static async void OnPropertyChanged(
        HexEditorControl element,
        AvaloniaPropertyChangedEventArgs e)
    {
        var value = e.NewValue;
        await element._mapper.SetValue(e.Property.Name, value);
    }
}