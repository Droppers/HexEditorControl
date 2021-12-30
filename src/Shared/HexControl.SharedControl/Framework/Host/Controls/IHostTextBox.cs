namespace HexControl.SharedControl.Framework.Host.Controls;

internal class ProxyTextChangedEventArgs : System.EventArgs
{
    public ProxyTextChangedEventArgs(string newText)
    {
        NewText = newText;
    }

    public string NewText { get; }
}

internal interface IHostTextBox : IHostControl
{
    string Text { get; set; }
    event EventHandler<ProxyTextChangedEventArgs>? TextChanged;
    void Clear();
}