using HexControl.SharedControl.Framework.Drawing;
using HexControl.SharedControl.Framework.Host.EventArgs;

namespace HexControl.SharedControl.Framework.Host.Controls;

internal interface IHostControl
{
    double Width { get; set; }
    double Height { get; set; }
    event EventHandler<HostSizeChangedEventArgs>? SizeChanged;

    event EventHandler<HostMouseWheelEventArgs>? MouseWheel;
    event EventHandler<HostMouseButtonEventArgs>? MouseDown;
    event EventHandler<HostMouseButtonEventArgs>? MouseUp;
    event EventHandler<HostMouseEventArgs>? MouseMove;

    event EventHandler<HostKeyEventArgs>? KeyDown;
    event EventHandler<HostKeyEventArgs>? KeyUp;

    event EventHandler<IRenderContext>? Render;


    TControl? GetChild<TControl>(string name) where TControl : class, IHostControl;
    void AddChild(string name, IHostControl control);

    void Focus();
    void Invalidate();
}