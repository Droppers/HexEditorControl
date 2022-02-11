using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using HexControl.Core;
using HexControl.SharedControl.Control;
using HexControl.Wpf.D2D;
using HexControl.Wpf.Host;
using HexControl.Wpf.Host.Controls;
using Microsoft.Win32;

namespace HexControl.Wpf;

public partial class HexEditorControl : UserControl
{
    public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register(nameof(Document),
        typeof(Document), typeof(HexEditorControl), new PropertyMetadata(null, OnPropertyChanged));

    public static readonly DependencyProperty EvenForegroundProperty = DependencyProperty.Register(
        nameof(EvenForeground),
        typeof(Brush), typeof(HexEditorControl), new PropertyMetadata(null, OnPropertyChanged));

    public static readonly DependencyProperty RowHeightProperty = DependencyProperty.Register(nameof(RowHeight),
        typeof(int), typeof(HexEditorControl), new PropertyMetadata(15, OnPropertyChanged));


    public static readonly DependencyProperty RenderApiProperty = DependencyProperty.Register(nameof(RenderApi),
        typeof(HexRenderApi), typeof(HexEditorControl), new PropertyMetadata(null, OnPropertyChanged));

    public HexEditorControl()
    {
        InitializeComponent();
        
        var host = new WpfD2DHost(HostContainer, new D2DControl(HostContainer))
        {
            { "VerticalScrollBar", new WpfScrollBar(VerticalScrollBar)},
            { "HorizontalScrollBar", new WpfScrollBar(HorizontalScrollBar)},
            { "FakeTextBox", new WpfTextBox(FakeTextBox)}
        };

        Control = new SharedHexControl();
        Control.ScrollBarVisibilityChanged += OnScrollBarVisibilityChanged;
        Control.AttachHost(host);

        var factory = new WpfNativeFactory();
        Mapper = new HexControlPropertyMapper(Control, factory);
    }

    private void OnScrollBarVisibilityChanged(object? sender, ScrollBarVisibilityChangedEventArgs e)
    {
        if (e.ScrollBar is SharedScrollBar.Horizontal)
        {
            GridRow.Height = e.Visible ? GridLength.Auto : new GridLength(0);
        }
        else
        {
            GridColumn.Width = e.Visible ? GridLength.Auto : new GridLength(0);
        }
    }
    
    private HexControlPropertyMapper Mapper { get; }

    private SharedHexControl Control { get; }

    public Document? Document
    {
        get => (Document)GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    public Brush? EvenForeground
    {
        get => Mapper.GetValue<Brush>(GetValue(EvenForegroundProperty));
        set => SetValue(EvenForegroundProperty, value);
    }

    public int RowHeight
    {
        get => Mapper.GetValue<int>(GetValue(RowHeightProperty));
        set => SetValue(RowHeightProperty, value);
    }

    public HexRenderApi? RenderApi
    {
        get => Mapper.GetValueNullable<HexRenderApi>(GetValue(RenderApiProperty));
        set => SetValue(RenderApiProperty, value);
    }

    private static async void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not HexEditorControl editor)
        {
            return;
        }

        var value = e.NewValue;
        await editor.Mapper.SetValue(e.Property.Name, value);
    }
}