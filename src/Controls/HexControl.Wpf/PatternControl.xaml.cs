using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using HexControl.PatternLanguage.Patterns;
using HexControl.SharedControl.Control;
using HexControl.SharedControl.PatternControl;
using HexControl.Wpf.D2D;
using HexControl.Wpf.Host;
using HexControl.Wpf.Host.Controls;

namespace HexControl.Wpf;

public partial class PatternControl : UserControl
{
    public static readonly DependencyProperty PatternsProperty = DependencyProperty.Register(nameof(Patterns),
        typeof(List<PatternData>), typeof(PatternControl), new PropertyMetadata(null, OnPropertyChanged));

    private readonly PatternControlPropertyMapper _mapper;

    public PatternControl()
    {
        InitializeComponent();

        var control = new SharedPatternControl();
        var factory = new WpfNativeFactory();
        _mapper = new PatternControlPropertyMapper(control, factory);

        var host = new WpfD2DHost(HostContainer, new D2DControl(HostContainer))
        {
            {SharedPatternControl.VerticalScrollBarName, new WpfScrollBar(VerticalScrollBar)}
        };
        control.AttachHost(host);
    }

    public List<PatternData>? Patterns
    {
        get => (List<PatternData>?)GetValue(PatternsProperty);
        set => SetValue(PatternsProperty, value);
    }

    private static async void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PatternControl control)
        {
            return;
        }

        var value = e.NewValue;
        await control._mapper.SetValue(e.Property.Name, value);
    }
}