using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using HexControl.Framework.Drawing;
using HexControl.Framework.Host;
using HexControl.Framework.Host.Controls;

namespace HexControl.Avalonia.Host.Controls;

internal class AvaloniaControl : HostControl
{
    private readonly Control _control;
    private AvaloniaRenderContext? _renderContext;
    protected HostKeyModifier modifiers = HostKeyModifier.Default;

    public AvaloniaControl(Control control)
    {
        _control = control;
        _control.PointerPressed += OnPointerPressed;
        _control.PointerMoved += OnPointerMoved;
        _control.PointerReleased += OnPointerReleased;
        _control.PointerWheelChanged += ControlOnPointerWheelChanged;

        _control.PointerLeave += OnPointerLeave;
        _control.PointerEnter += OnPointerEnter;

        _control.EffectiveViewportChanged += OnEffectiveViewportChanged;

        _control.AddHandler(InputElement.KeyDownEvent, ControlOnKeyDown, RoutingStrategies.Tunnel);
        _control.AddHandler(InputElement.KeyUpEvent, ControlOnKeyUp, RoutingStrategies.Tunnel);
    }

    public override double Width => _control.Bounds.Width;
    public override double Height => _control.Bounds.Height;

    public override HostCursor? Cursor
    {
        get => currentCursor;
        set
        {
            if (currentCursor == value)
            {
                return;
            }

            var oldCursor = _control.Cursor;
            _control.Cursor = value is null ? null : new Cursor(MapCursor(value.Value));
            oldCursor?.Dispose();
            currentCursor = value;
        }
    }

    public override bool Visible
    {
        get => _control.IsVisible;
        set => _control.IsVisible = value;
    }

    private void OnPointerLeave(object? sender, PointerEventArgs e)
    {
        RaiseMouseLeave();
    }

    private void OnPointerEnter(object? sender, PointerEventArgs e)
    {
        RaiseMouseEnter();
    }

    private static StandardCursorType MapCursor(HostCursor cursor)
    {
        return cursor switch
        {
            HostCursor.Arrow => StandardCursorType.Arrow,
            HostCursor.Hand => StandardCursorType.Hand,
            HostCursor.Text => StandardCursorType.Ibeam,
            HostCursor.SizeNs => StandardCursorType.SizeNorthSouth,
            HostCursor.SizeNesw => StandardCursorType.BottomRightCorner,
            HostCursor.SizeWe => StandardCursorType.SizeWestEast,
            HostCursor.SizeNwse => StandardCursorType.BottomLeftCorner,
            _ => throw new ArgumentOutOfRangeException(nameof(cursor), cursor, null)
        };
    }

    private void ControlOnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var position = e.GetCurrentPoint(_control).Position;
        RaiseMouseWheel(new SharedPoint(position.X, position.Y), (int)e.Delta.Y);
    }

    private void ControlOnKeyDown(object? sender, KeyEventArgs e)
    {
        modifiers |= MapKeyModifier(e.Key);

        var key = MapKey(e.Key);
        RaiseKeyDown(modifiers, key);
    }

    private void ControlOnKeyUp(object? sender, KeyEventArgs e)
    {
        modifiers &= ~MapKeyModifier(e.Key);

        var key = MapKey(e.Key);
        RaiseKeyUp(modifiers, key);
    }

    private static HostKey MapKey(Key key)
    {
        return key switch
        {
            Key.Left => HostKey.Left,
            Key.Up => HostKey.Up,
            Key.Right => HostKey.Right,
            Key.Down => HostKey.Down,
            Key.LeftShift => HostKey.Shift,
            Key.RightShift => HostKey.Shift,
            Key.LeftCtrl => HostKey.Control,
            Key.RightCtrl => HostKey.Control,
            Key.Back => HostKey.Back,
            Key.Delete => HostKey.Delete,
            Key.Z => HostKey.Z,
            Key.Y => HostKey.Y,
            Key.A => HostKey.A,
            Key.C => HostKey.C,
            Key.V => HostKey.V,
            _ => HostKey.Unknown
        };
    }

    private static HostKeyModifier MapKeyModifier(Key key)
    {
        return key switch
        {
            Key.LeftCtrl => HostKeyModifier.Control,
            Key.RightCtrl => HostKeyModifier.Control,
            Key.LeftShift => HostKeyModifier.Shift,
            Key.RightShift => HostKeyModifier.Shift,
            _ => HostKeyModifier.Default
        };
    }

    private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
    {
        var newSize = new SharedSize(e.EffectiveViewport.Width, e.EffectiveViewport.Height);
        RaiseSizeChanged(newSize, newSize);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var (x, y) = e.GetPosition(_control);
        var properties = e.GetCurrentPoint(_control).Properties;
        RaiseMouseUp(MapPointerToMouseButton(properties), new SharedPoint(x, y));
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var (x, y) = e.GetPosition(_control);
        RaiseMouseMove(new SharedPoint(x, y));
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var (x, y) = e.GetPosition(_control);
        var properties = e.GetCurrentPoint(_control).Properties;
        RaiseMouseDown(MapPointerToMouseButton(properties), new SharedPoint(x, y));
    }

    private static HostMouseButton MapPointerToMouseButton(PointerPointProperties properties)
    {
        if (properties.IsLeftButtonPressed)
        {
            return HostMouseButton.Left;
        }

        if (properties.IsMiddleButtonPressed)
        {
            return HostMouseButton.Middle;
        }

        if (properties.IsRightButtonPressed)
        {
            return HostMouseButton.Right;
        }

        return HostMouseButton.Unknown;
    }

    public override void Focus()
    {
        _control.Focus();
    }

    public override void Invalidate()
    {
        _control.InvalidateVisual();
    }

    public void DoRender(DrawingContext context)
    {
        _renderContext ??= new AvaloniaRenderContext(context);
        _renderContext.Context = context;

        // TODO: currently does not use throttling internally, this (value false) will result in an invalid state inside Avalonia
        // TODO: for new we accept the higher CPU usage
        RaiseRender(_renderContext, true);
    }

    public override void Dispose()
    {
        base.Dispose();

        _control.PointerPressed -= OnPointerPressed;
        _control.PointerMoved -= OnPointerMoved;
        _control.PointerReleased -= OnPointerReleased;
        _control.EffectiveViewportChanged -= OnEffectiveViewportChanged;

        _control.RemoveHandler(InputElement.KeyDownEvent, ControlOnKeyDown);
        _control.KeyDown -= ControlOnKeyDown;
        _control.KeyUp -= ControlOnKeyUp;
        _control.PointerWheelChanged -= ControlOnPointerWheelChanged;
    }
}