using Avalonia;
using Avalonia.Controls;
using HexControl.Framework.Host;
using HexControl.Framework.Host.Controls;

namespace HexControl.Avalonia.Host.Controls;

internal class AvaloniaTextBox : AvaloniaControl, IHostTextBox
{
    private readonly TextBox _textBox;
    private readonly IDisposable _caretIndexChangedDisposable;
    private readonly IDisposable _textChangedDisposable;

    public AvaloniaTextBox(TextBox textBox) : base(textBox)
    {
        _textBox = textBox;

        // TODO: Bug, see: https://github.com/AvaloniaUI/Avalonia/pull/8318, waiting for release
        // As a temporary workaround, always set the caret position to the end
        _caretIndexChangedDisposable = _textBox.GetObservable(TextBox.CaretIndexProperty).Subscribe(_ =>
        {
            _textBox.CaretIndex = int.MaxValue;
        });

        _textChangedDisposable = _textBox.GetObservable(TextBox.TextProperty).Subscribe(text =>
        {
            if (modifiers.HasFlag(HostKeyModifier.Control))
            {
                return;
            }

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            TextChanged?.Invoke(this, new HostTextChangedEventArgs(text));
        });
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

    public override void Dispose()
    {
        _caretIndexChangedDisposable.Dispose();
        _textChangedDisposable.Dispose();

        base.Dispose();
    }
}