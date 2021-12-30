using HexControl.SharedControl.Framework.Host.Controls;

namespace HexControl.WinForms.Host.Controls;

internal class WinFormsTextBox : WinFormsControl, IHostTextBox
{
    private readonly TextBox _textBox;

    public WinFormsTextBox(TextBox textBox) : base(textBox)
    {
        _textBox = textBox;
        _textBox.TextChanged += TextBoxOnTextChanged;
    }

    public event EventHandler<ProxyTextChangedEventArgs>? TextChanged;

    public string Text
    {
        get => _textBox.Text;
        set => _textBox.Text = value;
    }

    public void Clear()
    {
        _textBox.Clear();
    }

    private void TextBoxOnTextChanged(object? sender, EventArgs e)
    {
        TextChanged?.Invoke(this, new ProxyTextChangedEventArgs(_textBox.Text));
    }

    public override void Dispose()
    {
        _textBox.TextChanged -= TextBoxOnTextChanged;
        base.Dispose();
    }
}