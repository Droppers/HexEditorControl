using System.Collections;
using HexControl.SharedControl.Framework.Drawing;
using HexControl.SharedControl.Framework.Host.EventArgs;
using HexControl.SharedControl.Framework.Visual;

namespace HexControl.SharedControl.Framework.Host.Controls;

internal abstract class HostControl : IHostControl, IEnumerable, IDisposable
{
    private readonly Dictionary<string, IHostControl> _children;

    protected HostControl()
    {
        _children = new Dictionary<string, IHostControl>();
    }

    public virtual void Dispose() { }

    public IEnumerator GetEnumerator() => _children.GetEnumerator();

    public event EventHandler<HostSizeChangedEventArgs>? SizeChanged;

    public event EventHandler<HostMouseWheelEventArgs>? MouseWheel;
    public event EventHandler<HostMouseButtonEventArgs>? MouseDown;
    public event EventHandler<HostMouseButtonEventArgs>? MouseUp;
    public event EventHandler<HostMouseEventArgs>? MouseMove;

    public event EventHandler<HandledEventArgs>? MouseLeave;
    public event EventHandler<HandledEventArgs>? MouseEnter;

    public event EventHandler<HostKeyEventArgs>? KeyDown;
    public event EventHandler<HostKeyEventArgs>? KeyUp;

    public event EventHandler<IRenderContext>? Render;

    public TControl? GetChild<TControl>(string name) where TControl : class, IHostControl
    {
        if (_children.TryGetValue(name, out var control) && control is TControl cast)
        {
            return cast;
        }

        return null;
    }

    public void AddChild(string name, IHostControl control)
    {
        _children[name] = control;
    }

    public virtual double Width { get; set; }
    public virtual double Height { get; set; }

    public abstract bool Visible { get; set; }
    public abstract HostCursor? Cursor { get; set; }
    
    public abstract void Focus();
    public abstract void Invalidate();

    protected void RaiseMouseWheel(SharedPoint point, int delta)
    {
        MouseWheel?.Invoke(this, new HostMouseWheelEventArgs(point, delta));
    }

    protected void RaiseMouseDown(HostMouseButton button, SharedPoint point)
    {
        MouseDown?.Invoke(this, new HostMouseButtonEventArgs(button, point));
    }

    protected void RaiseMouseUp(HostMouseButton button, SharedPoint point)
    {
        MouseUp?.Invoke(this, new HostMouseButtonEventArgs(button, point));
    }

    protected void RaiseMouseMove(SharedPoint point)
    {
        MouseMove?.Invoke(this, new HostMouseEventArgs(point));
    }

    protected void RaiseMouseEnter()
    {
        MouseEnter?.Invoke(this, new HandledEventArgs());
    }

    protected void RaiseMouseLeave()
    {
        MouseLeave?.Invoke(this, new HandledEventArgs());
    }


    protected void RaiseSizeChanged(SharedSize oldSize, SharedSize newSize)
    {
        SizeChanged?.Invoke(this, new HostSizeChangedEventArgs(oldSize, newSize));
    }

    protected void RaiseKeyDown(bool isRepeat, bool isUp, bool isDown, HostKeyModifier modifiers, HostKey key)
    {
        KeyDown?.Invoke(this, new HostKeyEventArgs(isRepeat, isUp, isDown, modifiers, key));
    }

    protected void RaiseKeyUp(bool isRepeat, bool isUp, bool isDown, HostKeyModifier modifiers, HostKey key)
    {
        KeyUp?.Invoke(this, new HostKeyEventArgs(isRepeat, isUp, isDown, modifiers, key));
    }


    protected void RaiseRender(IRenderContext context)
    {
        Render?.Invoke(this, context);
    }


    // Do not rename, part of object initializer
    public void Add(string name, IHostControl control)
    {
        AddChild(name, control);
    }
}