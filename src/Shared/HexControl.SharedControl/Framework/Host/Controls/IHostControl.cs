using HexControl.SharedControl.Framework.Drawing;
using HexControl.SharedControl.Framework.Host.EventArgs;
using HexControl.SharedControl.Framework.Visual;

namespace HexControl.SharedControl.Framework.Host.Controls;

internal delegate void HostRenderEvent(IRenderContext context, bool newSurface);

internal interface IHostControl
{
    double Width { get; set; }
    double Height { get; set; }

    bool Visible { get; set; }
    HostCursor? Cursor { get; set; }

    event EventHandler<HostSizeChangedEventArgs>? SizeChanged;

    event EventHandler<HostMouseWheelEventArgs>? MouseWheel;
    event EventHandler<HostMouseButtonEventArgs>? MouseDown;
    event EventHandler<HostMouseButtonEventArgs>? MouseUp;
    event EventHandler<HostMouseEventArgs>? MouseMove;

    event EventHandler<HandledEventArgs>? MouseEnter;
    event EventHandler<HandledEventArgs>? MouseLeave;

    event EventHandler<HostKeyEventArgs>? KeyDown;
    event EventHandler<HostKeyEventArgs>? KeyUp;

    event HostRenderEvent? Render;


    TControl? GetChild<TControl>(string name) where TControl : class, IHostControl;
    void AddChild(string name, IHostControl control);

    void Focus();
    void Invalidate();
}