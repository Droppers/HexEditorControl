using HexControl.Core;
using HexControl.SharedControl.Control;
using HexControl.WinUI.Host;
using HexControl.WinUI.Host.Controls;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HexControl.WinUI;

public sealed partial class HexEditorControl : UserControl, ICursorChangeable
{
    private static readonly DependencyProperty DocumentProperty =
        DependencyProperty.Register(nameof(Document),
            typeof(Document),
            typeof(HexEditorControl), new PropertyMetadata(null, PropertyChanged));

    private readonly HexControlPropertyMapper _mapper;

    public HexEditorControl()
    {
        InitializeComponent();

        var host = new WinUIHost(this, RenderPanel)
        {
            {SharedHexControl.VerticalScrollBarName, new WinUIScrollBar(VerticalScrollBar)},
            {SharedHexControl.HorizontalScrollBarName, new WinUIScrollBar(HorizontalScrollBar)},
            {SharedHexControl.FakeTextBoxName, new WinUITextBox(FakeTextBox)}
        };
        var control = new SharedHexControl();
        control.AttachHost(host);

        var factory = new WinUINativeFactory();
        _mapper = new HexControlPropertyMapper(control, factory);
    }

    public Document? Document
    {
        get => _mapper.GetValue<Document>(GetValue(DocumentProperty));
        set => SetValue(DocumentProperty, value);
    }

    // To allow us to change the cursor from a public property
    public InputCursor Cursor
    {
        get => ProtectedCursor;
        set => ProtectedCursor = value;
    }

    private static async void PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not HexEditorControl control)
        {
            return;
        }

        var property = e.Property;
        var propertyName = 0 switch
        {
            _ when property == DocumentProperty => nameof(Document),
            _ => null
        };

        if (propertyName is not null)
        {
            await control._mapper.SetValueAsync(propertyName, e.NewValue);
        }
    }
}