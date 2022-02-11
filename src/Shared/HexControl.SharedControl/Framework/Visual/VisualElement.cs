using System.Diagnostics;
using HexControl.Core;
using HexControl.SharedControl.Framework.Drawing;
using HexControl.SharedControl.Framework.Host;
using HexControl.SharedControl.Framework.Host.Controls;
using HexControl.SharedControl.Framework.Host.EventArgs;

namespace HexControl.SharedControl.Framework.Visual;

internal abstract class VisualElement : ObservableObject
{
    private readonly List<VisualElement> _children;

    private Stopwatch? _sw;

    private VisualElementTree? _tree;

    protected VisualElement(bool isRoot) : this()
    {
        if (!isRoot)
        {
            return;
        }

        _tree = new VisualElementTree(this);
        _queue = new RenderQueue(this);
    }

    protected VisualElement()
    {
        _children = new List<VisualElement>();
    }

    public IHostControl? Host { get; private set; }

    public virtual double Top { get; set; }
    public virtual double Left { get; set; }
    public virtual double Width { get; set; }
    public virtual double Height { get; set; }

    public virtual bool Visible { get; set; } = true;

    public VisualElement? Parent { get; private set; }
    public IReadOnlyList<VisualElement> Children => _children;

    public event EventHandler<HostMouseButtonEventArgs>? MouseDown
    {
        add => _tree?.Events.AddHandler(this, value);
        remove => _tree?.Events.RemoveHandler(this, value);
    }

    public event EventHandler<HostMouseEventArgs>? MouseMove
    {
        add => _tree?.Events.AddHandler(this, value);
        remove => _tree?.Events.RemoveHandler(this, value);
    }

    public event EventHandler<HostMouseButtonEventArgs>? MouseUp
    {
        add => _tree?.Events.AddHandler(this, value);
        remove => _tree?.Events.RemoveHandler(this, value);
    }

    public event EventHandler<HostMouseWheelEventArgs>? MouseWheel
    {
        add => _tree?.Events.AddHandler(this, value);
        remove => _tree?.Events.RemoveHandler(this, value);
    }

    public event EventHandler<HandledEventArgs>? MouseLeave
    {
        add => _tree?.Events.AddHandler(this, value);
        remove => _tree?.Events.RemoveHandler(this, value);
    }

    public event EventHandler<HandledEventArgs>? MouseEnter
    {
        add => _tree?.Events.AddHandler(this, value);
        remove => _tree?.Events.RemoveHandler(this, value);
    }

    public event EventHandler<HostSizeChangedEventArgs>? SizeChanged
    {
        add => _tree?.Events.AddHandler(this, value);
        remove => _tree?.Events.RemoveHandler(this, value);
    }

    public event EventHandler<HostKeyEventArgs>? KeyDown
    {
        add => _tree?.Events.AddHandler(this, value);
        remove => _tree?.Events.RemoveHandler(this, value);
    }

    public event EventHandler<HostKeyEventArgs>? KeyUp
    {
        add => _tree?.Events.AddHandler(this, value);
        remove => _tree?.Events.RemoveHandler(this, value);
    }

    private VisualElement? _oldMouseOwner;

    public void AttachHost(IHostControl attachHost)
    {
        Host = attachHost;

        if (ReferenceEquals(_tree?.Root, this))
        {
            attachHost.MouseDown += HostOnMouseDown;
            attachHost.MouseMove += HostOnMouseMove;
            attachHost.MouseUp += HostOnMouseUp;
            attachHost.MouseWheel += HostOnMouseWheel;
            attachHost.SizeChanged += HostOnSizeChanged;

            attachHost.MouseEnter += HostOnMouseEnter;
            attachHost.MouseLeave += HostOnMouseLeave;
            attachHost.KeyDown += HostOnKeyDown;
            attachHost.KeyUp += HostOnKeyUp;

            attachHost.Render += HostOnRender;
        }

        // Notify children when they were added before a host was attached
        foreach (var child in Children)
        {
            AttachAllToHost(child, attachHost);
        }

        OnHostAttached(attachHost);
    }

    private void HostOnMouseEnter(object? sender, HandledEventArgs e)
    {
        RaiseTunnelEvent(nameof(MouseEnter), e);
    }

    private void HostOnMouseLeave(object? sender, HandledEventArgs e)
    {
        RaiseTunnelEvent(nameof(MouseLeave), e);
    }

    private void HostOnKeyDown(object? sender, HostKeyEventArgs e)
    {
        RaiseFocusDependentEvent(nameof(KeyDown), e);
    }

    private void HostOnKeyUp(object? sender, HostKeyEventArgs e)
    {
        RaiseFocusDependentEvent(nameof(KeyUp), e);
    }

    protected RenderQueue _queue;

    private async void HostOnRender(object? sender, IRenderContext e)
    {
        await _queue.Render(e);
        //InvokeRender(e);
    }

    public void DetachHost(IHostControl detachHost)
    {
        detachHost.MouseDown -= HostOnMouseDown;
        detachHost.MouseMove -= HostOnMouseMove;
        detachHost.MouseUp -= HostOnMouseUp;
        detachHost.MouseWheel -= HostOnMouseWheel;
        detachHost.SizeChanged -= HostOnSizeChanged;

        Host = null;
    }

    private void HostOnSizeChanged(object? sender, HostSizeChangedEventArgs e)
    {
        RaiseTunnelEvent(nameof(SizeChanged), e);
    }

    private void RaiseTunnelEvent(string name, HandledEventArgs e)
    {
        _tree?.Events.Raise(_tree.Root, name, e, EventStrategy.Tunnel);
    }

    private void RaiseDirectEvent(VisualElement target, string name, HandledEventArgs e)
    {
        _tree?.Events.Raise(target, name, e, EventStrategy.Direct);
    }

    private void HostOnMouseDown(object? sender, HostMouseButtonEventArgs e)
    {
        RaiseMouseEvent(nameof(MouseDown), e);
    }

    private void HostOnMouseMove(object? sender, HostMouseEventArgs e)
    {
        var newMouseOwner = RaiseMouseEvent(nameof(MouseMove), e);

        // Our own mouse leave / enter implementation to handle mouse owner changing between visual elements
        if (_oldMouseOwner != newMouseOwner)
        {
            if (_oldMouseOwner is not null)
            {
                RaiseDirectEvent(_oldMouseOwner, nameof(MouseLeave), new HandledEventArgs());
            }

            if (newMouseOwner is not null)
            {
                RaiseDirectEvent(newMouseOwner, nameof(MouseEnter), new HandledEventArgs());
            }
        }

        _oldMouseOwner = newMouseOwner;
    }

    private void HostOnMouseUp(object? sender, HostMouseButtonEventArgs e)
    {
        RaiseMouseEvent(nameof(MouseUp), e);
    }

    private void HostOnMouseWheel(object? sender, HostMouseWheelEventArgs e)
    {
        RaiseMouseEvent(nameof(MouseWheel), e);
    }

    private VisualElement? RaiseMouseEvent(string name, PointerEventArgs args)
    {
        static bool PointInBounds(SharedPoint point, VisualElement element) =>
            point.X >= element.Left && point.Y >= element.Top && point.X <= element.Left + element.Width &&
            point.Y <= element.Top + element.Height;

        if (_tree is null)
        {
            return null;
        }

        return _tree.State.CapturedElement is not null
            ? _tree.Events.Raise(_tree.State.CapturedElement, name, args)
            : _tree.Events.Raise(_tree.Root, name, args, element => PointInBounds(args.Point, element));
    }

    private void RaiseFocusDependentEvent(string name, HandledEventArgs args)
    {
        if (_tree?.State.CapturedElement != null)
        {
            _tree.Events.Raise(_tree.State.CapturedElement, name, args);
        }
    }

    protected virtual void OnHostAttached(IHostControl attachHost) { }

    protected virtual void OnHostDetached(IHostControl detachHost) { }

    protected virtual void CaptureMouse()
    {
        _tree?.State.Capture(this);
    }

    protected virtual void ReleaseMouse()
    {
        _tree?.State.ReleaseCapture();
    }

    protected virtual void Focus()
    {
        _tree?.State.Focus(this);
    }

    protected virtual void ReleaseFocus()
    {
        _tree?.State.ReleaseFocus();
    }

    protected void AddDirtyRect(SharedRectangle rectangle, int grow = 0)
    {
        if (Left is not 0 || Top is not 0)
        {
            // Relative to current control location
            rectangle = new SharedRectangle(Left + rectangle.X, Top + rectangle.Y, rectangle.Width, rectangle.Height);
        }

        if (grow > 0)
        {
            rectangle = new SharedRectangle(
                Math.Max(0, rectangle.X - grow),
                Math.Max(0, rectangle.Y - grow),
                rectangle.Width + 2 * grow,
                rectangle.Height + 2 * grow
            );
        }

        _tree?.AddDirtyRect(rectangle);
    }

    public HostCursor? Cursor
    {
        get => Host?.Cursor;
        set
        {
            if (Host is not null)
            {
                Host.Cursor = value;
            }
        }
    }

    internal void AttachToTree(VisualElementTree tree)
    {
        _tree = tree;
        OnTreeAttached();
    }

    internal void DetachFromTree()
    {
        ClearEventHandlers();
        ClearState();
        _tree = null;

        OnTreeDetached();
    }

    protected void ClearEventHandlers()
    {
        _tree?.Events.ClearHandlers(this);
    }

    protected void ClearState()
    {
        _tree?.State.ClearState(this);
    }

    protected virtual void OnTreeAttached() { }

    protected virtual void OnTreeDetached() { }

    public void AddChild(VisualElement element)
    {
        element.Parent = this;
        if (_tree is not null)
        {
            AttachAllToTree(element, _tree);
        }

        if (Host is not null)
        {
            AttachAllToHost(element, Host);
        }

        _children.Add(element);
    }

    private static void AttachAllToTree(VisualElement element, VisualElementTree attachTree)
    {
        element.AttachToTree(attachTree);

        foreach (var child in element.Children)
        {
            AttachAllToTree(child, attachTree);
        }
    }

    private static void AttachAllToHost(VisualElement element, IHostControl host)
    {
        element.AttachHost(host);

        foreach (var child in element.Children)
        {
            AttachAllToHost(child, host);
        }
    }

    public void RemoveChild(VisualElement element)
    {
        element.DetachAllFromTree(element);
        _children.Remove(element);
    }

    private void DetachAllFromTree(VisualElement element)
    {
        DetachFromTree();

        foreach (var child in element.Children)
        {
            DetachAllFromTree(child);
        }
    }
    
    public void InvokeRender(IRenderContext context)
    {
        if (Parent is null)
        {
            _sw ??= new Stopwatch();
            _sw.Restart();

            context.Begin();
        }

        Render(context);

        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < _children.Count; i++)
        {
            var child = _children[i];
            if (!child.Visible)
            {
                continue;
            }

            var shouldTranslate = child.Left is not 0 || child.Top is not 0;
            if (shouldTranslate)
            {
                context.PushTranslate(child.Left, child.Top);
            }

            child.InvokeRender(context);

            if (shouldTranslate)
            {
                context.Pop();
            }
        }

        RenderAfter(context);

        if (_tree?.Root == this && _sw is not null)
        {
            context.End(_tree.DirtyRect);
            _tree?.ClearDirtyRect();

            Debug.WriteLine($"Render: {_sw.ElapsedMilliseconds}");
        }
    }

    protected virtual void RenderAfter(IRenderContext context) { }

    protected virtual void Render(IRenderContext context) { }

    public void Dispose()
    {
        DetachFromTree();
    }
}