using Microsoft.UI.Input;

namespace HexControl.WinUI.Host.Controls;

internal interface ICursorChangeable
{
    public InputCursor Cursor { get; set; }
}