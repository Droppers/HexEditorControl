using System.Windows.Controls;
using HexControl.Framework.Host.Controls;

namespace HexControl.Wpf.Host.Controls;

internal class WpfTextBox : WpfControl, IHostTextBox
{
    private readonly TextBox _textBox;

    public WpfTextBox(TextBox textBox) : base(textBox)
    {
        _textBox = textBox;
        _textBox.TextChanged += TextBoxOnTextChanged;
    }

    public event EventHandler<HostTextChangedEventArgs>? TextChanged;

    public string Text
    {
        get => _textBox.Text;
        set => _textBox.Text = value;
    }

    public void Clear()
    {
        _textBox.Clear();
    }

    private void TextBoxOnTextChanged(object sender, TextChangedEventArgs e)
    {
        TextChanged?.Invoke(this, new HostTextChangedEventArgs(_textBox.Text));
    }

    public override void Dispose()
    {
        base.Dispose();

        _textBox.TextChanged -= TextBoxOnTextChanged;
    }
}