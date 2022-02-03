using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using HexControl.Avalonia.Host;
using HexControl.Avalonia.Host.Controls;
using HexControl.Core;
using HexControl.PatternLanguage.Patterns;
using HexControl.SharedControl.Control;
using HexControl.SharedControl.PatternControl;

namespace HexControl.Avalonia;

public class PatternControl : UserControl
{
    private readonly SharedPatternControl _control;
    private readonly PatternControlPropertyMapper _mapper;
    private TextBox? _fakeTextBox;
    private ScrollBar? _horizontalScrollBar;

    public static readonly StyledProperty<List<PatternData>?> PatternsProperty =
        AvaloniaProperty.Register<PatternControl, List<PatternData>?>(nameof(Patterns));

    private AvaloniaControl? _host;

    private ScrollBar? _verticalScrollBar;

    static PatternControl()
    {
        PatternsProperty.Changed.AddClassHandler<PatternControl>(OnPropertyChanged);
    }

    public PatternControl()
    {
        _control = new SharedPatternControl();

        var factory = new AvaloniaNativeFactory();
        _mapper = new PatternControlPropertyMapper(_control, factory);

        InitializeComponent();
    }

    public List<PatternData>? Patterns
    {
        get => _mapper.GetValueNullable<List<PatternData>>(GetValue(PatternsProperty));
        set => SetValue(PatternsProperty, value);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        //_verticalScrollBar = this.FindControl<ScrollBar>(SharedHexControl.VerticalScrollBarName);
        //_horizontalScrollBar = this.FindControl<ScrollBar>(SharedHexControl.HorizontalScrollBarName);
        //_fakeTextBox = this.FindControl<TextBox>(SharedHexControl.FakeTextBoxName);

        //if (_verticalScrollBar is null || _horizontalScrollBar is null || _fakeTextBox is null)
        //{
        //    return;
        //}

        _host = new AvaloniaControl(this)
        {
            //{SharedHexControl.VerticalScrollBarName, new AvaloniaScrollBar(_verticalScrollBar)},
            //{SharedHexControl.HorizontalScrollBarName, new AvaloniaScrollBar(_horizontalScrollBar)},
            //{SharedHexControl.FakeTextBoxName, new AvaloniaTextBox(_fakeTextBox)}
        };
        _control.AttachHost(_host);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        _host?.DoRender(context);
    }

    private static async void OnPropertyChanged(
        PatternControl element,
        AvaloniaPropertyChangedEventArgs e)
    {
        var value = e.NewValue;
        await element._mapper.SetValue(e.Property.Name, value);
    }
}