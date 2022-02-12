namespace HexControl.SharedControl.Framework.Host.Controls;

internal class HostTextChangedEventArgs : System.EventArgs
{
    public HostTextChangedEventArgs(string newText)
    {
        NewText = newText;
    }

    public string NewText { get; }
}

internal interface IHostTextBox : IHostControl
{
    string Text { get; set; }
    event EventHandler<HostTextChangedEventArgs>? TextChanged;
    void Clear();
}