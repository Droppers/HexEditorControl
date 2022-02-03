using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using HexControl.PatternLanguage.Patterns;
using HexControl.SharedControl.Control;
using HexControl.SharedControl.PatternControl;
using HexControl.Wpf.Host;
using HexControl.Wpf.Host.Controls;
using Microsoft.Win32;

namespace HexControl.Wpf;

public partial class PatternControl : UserControl
{
    public static readonly DependencyProperty PatternsProperty = DependencyProperty.Register(nameof(Patterns),
        typeof(List<PatternData>), typeof(PatternControl), new PropertyMetadata(null, OnPropertyChanged));

    private readonly SharedPatternControl _control;

    private readonly PatternControlPropertyMapper _mapper;

    private WpfD2DInteropHost _host;

    public PatternControl()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;

        _control = new SharedPatternControl();

        var factory = new WpfNativeFactory();
        _mapper = new PatternControlPropertyMapper(_control, factory);
    }

    public List<PatternData>? Patterns
    {
        get => (List<PatternData>?)GetValue(PatternsProperty);
        set => SetValue(PatternsProperty, value);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SystemEvents.PowerModeChanged += OnPowerModeChanged;

        var window = Window.GetWindow(this);
        if (window is null)
        {
            return;
            throw new Exception("Could not obtain window.");
        }

        window.DpiChanged += WindowOnDpiChanged;
        var owner = new WindowInteropHelper(window).Handle;
        InteropImage.WindowOwner = owner;
        _host = new WpfD2DInteropHost(ImageContainer, MainImage, InteropImage, true)
        {
            { SharedPatternControl.VerticalScrollBarName, new WpfScrollBar(VerticalScrollBar) }
        };
        _control.AttachHost(_host);

        _host.Resize();
    }

    private void WindowOnDpiChanged(object sender, DpiChangedEventArgs e)
    {
        _host.Invalidate();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;

        var window = Window.GetWindow(this);
        if (window is null)
        {
            return;
            throw new Exception("Could not obtain window.");
        }

        window.DpiChanged -= WindowOnDpiChanged;
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode is PowerModes.Resume)
        {
            _host.Invalidate();
        }
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