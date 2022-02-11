using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Input;

namespace HexControl.WinUI.Host.Controls
{
    internal interface ICursorChangeable
    {
        public InputCursor Cursor { get; set; }
    }
}
