using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using HexControl.Avalonia.Host;
using HexControl.Avalonia.Host.Controls;
using HexControl.Core;
using HexControl.SharedControl.Control;
using HexControl.SharedControl.Framework.Mapping;
using JetBrains.Annotations;

namespace HexControl.Avalonia;

[PublicAPI]
public class HexEditorControl : UserControl
{
    public static readonly StyledProperty<Document?> DocumentProperty =
        AvaloniaProperty.Register<HexEditorControl, Document?>(nameof(Document));

    public new static readonly StyledProperty<IBrush> BackgroundProperty =
        AvaloniaProperty.Register<HexEditorControl, IBrush>(nameof(Background));

    public static readonly StyledProperty<IBrush> HeaderForegroundProperty =
        AvaloniaProperty.Register<HexEditorControl, IBrush>(nameof(HeaderForeground));

    public static readonly StyledProperty<IBrush> OffsetForegroundProperty =
        AvaloniaProperty.Register<HexEditorControl, IBrush>(nameof(OffsetForeground));

    public new static readonly StyledProperty<IBrush> ForegroundProperty =
        AvaloniaProperty.Register<HexEditorControl, IBrush>(nameof(Foreground));

    public static readonly StyledProperty<IBrush> EvenForegroundProperty =
        AvaloniaProperty.Register<HexEditorControl, IBrush>(nameof(EvenForeground));

    private static readonly AvaloniaProperty[] Properties =
    {
        DocumentProperty, BackgroundProperty, HeaderForegroundProperty, OffsetForegroundProperty, ForegroundProperty, EvenForegroundProperty
    };

    private readonly SharedHexControl _control;
    private readonly IPropertyMapper _mapper;

    private TextBox? _fakeTextBox;
    private Grid? _grid;
    private ScrollBar? _horizontalScrollBar;
    private AvaloniaControl? _host;
    private ScrollBar? _verticalScrollBar;

    static HexEditorControl()
    {
        foreach (var property in Properties)
        {
            property.Changed.AddClassHandler<HexEditorControl>(OnPropertyChanged);
        }
    }

    public HexEditorControl()
    {
        _control = new SharedHexControl();
        _control.ScrollBarVisibilityChanged += OnScrollBarVisibilityChanged;

        var factory = new AvaloniaNativeFactory();
        _mapper = new HexControlPropertyMapper(_control, factory);

        InitializeComponent();
    }

    public Document? Document
    {
        get => _mapper.GetValueNullable<Document?>(GetValue(DocumentProperty));
        set => SetValue(DocumentProperty, value);
    }
    public new IBrush Background
    {
        get => _mapper.GetValue<IBrush>(GetValue(BackgroundProperty));
        set => SetValue(BackgroundProperty, value);
    }

    public IBrush HeaderForeground
    {
        get => _mapper.GetValue<IBrush>(GetValue(HeaderForegroundProperty));
        set => SetValue(HeaderForegroundProperty, value);
    }

    public IBrush OffsetForeground
    {
        get => _mapper.GetValue<IBrush>(GetValue(OffsetForegroundProperty));
        set => SetValue(OffsetForegroundProperty, value);
    }

    public new IBrush Foreground
    {
        get => _mapper.GetValue<IBrush>(GetValue(ForegroundProperty));
        set => SetValue(ForegroundProperty, value);
    }

    public IBrush EvenForeground
    {
        get => _mapper.GetValue<IBrush>(GetValue(EvenForegroundProperty));
        set => SetValue(EvenForegroundProperty, value);
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
        await element._mapper.SetValueAsync(e.Property.Name, value);
    }
}