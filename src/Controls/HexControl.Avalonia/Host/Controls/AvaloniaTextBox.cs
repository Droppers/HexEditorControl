using System;
using Avalonia;
using Avalonia.Controls;
using HexControl.SharedControl.Framework.Host;
using HexControl.SharedControl.Framework.Host.Controls;

namespace HexControl.Avalonia.Host.Controls;

internal class AvaloniaTextBox : AvaloniaControl, IHostTextBox
{
    private readonly TextBox _textBox;
    private readonly IDisposable _textChangedDisposable;

    public AvaloniaTextBox(TextBox textBox) : base(textBox)
    {
        _textBox = textBox;
        _textChangedDisposable = _textBox.GetObservable(TextBox.TextProperty).Subscribe(text =>
        {
            if (modifiers.HasFlag(HostKeyModifier.Control))
            {
                return;
            }

            TextChanged?.Invoke(this, new HostTextChangedEventArgs(text ?? ""));
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
        _textChangedDisposable.Dispose();

        base.Dispose();
    }
}