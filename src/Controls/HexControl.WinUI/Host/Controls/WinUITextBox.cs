using HexControl.Framework.Host.Controls;
using Microsoft.UI.Xaml.Controls;

namespace HexControl.WinUI.Host.Controls;

internal class WinUITextBox : WinUIControl, IHostTextBox
{
    private readonly TextBox _element;

    public WinUITextBox(TextBox element) : base(element)
    {
        _element = element;
        _element.TextChanged += OnTextChanged;
    }

    public string Text
    {
        get => _element.Text;
        set => _element.Text = value;
    }

    public event EventHandler<HostTextChangedEventArgs>? TextChanged;

    public void Clear()
    {
        _element.Text = "";
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        TextChanged?.Invoke(this, new HostTextChangedEventArgs(_element.Text));
    }

    public override void Dispose()
    {
        base.Dispose();

        _element.TextChanged -= OnTextChanged;
    }
}