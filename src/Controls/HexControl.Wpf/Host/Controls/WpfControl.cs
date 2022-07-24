using System.Windows;
using System.Windows.Input;
using HexControl.Framework.Drawing;
using HexControl.Framework.Host;
using HexControl.Framework.Host.Controls;

namespace HexControl.Wpf.Host.Controls;

internal class WpfControl : HostControl
{
    private readonly FrameworkElement _element;
    protected HostKeyModifier modifiers = HostKeyModifier.Default;

    protected WpfControl(FrameworkElement element)
    {
        _element = element;
        element.Focusable = true;
        element.MouseDown += ElementOnMouseDown;
        element.MouseUp += ElementOnMouseUp;
        element.MouseMove += ElementOnMouseMove;
        element.MouseWheel += ElementOnMouseWheel;

        element.MouseEnter += ElementOnMouseEnter;
        element.MouseLeave += ElementOnMouseLeave;

        element.SizeChanged += ElementOnSizeChanged;

        element.PreviewKeyDown += ElementOnKeyDown;
        element.PreviewKeyUp += ElementOnKeyUp;
    }

    public override double Width => _element.ActualWidth;
    public override double Height => _element.ActualHeight;

    public override bool Visible
    {
        get => _element.Visibility is Visibility.Visible;
        set => _element.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }

    public override HostCursor? Cursor
    {
        get => currentCursor;
        set
        {
            if (currentCursor == value)
            {
                return;
            }

            Mouse.OverrideCursor = MapCursor(value);
            currentCursor = value;
        }
    }

    private void ElementOnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _element.CaptureMouse();

        var point = e.GetPosition(_element);
        RaiseMouseDown(MapMouseButton(e.ChangedButton), new SharedPoint(point.X, point.Y));
    }

    private void ElementOnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _element.ReleaseMouseCapture();

        var point = e.GetPosition(_element);
        RaiseMouseUp(MapMouseButton(e.ChangedButton), new SharedPoint(point.X, point.Y));
    }

    private void ElementOnMouseMove(object sender, MouseEventArgs e)
    {
        var point = e.GetPosition(_element);
        RaiseMouseMove(new SharedPoint(point.X, point.Y));
    }

    private void ElementOnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var point = e.GetPosition(_element);

        var delta = e.Delta / 120d;
        var (deltaX, deltaY) = modifiers.HasFlag(HostKeyModifier.Shift) ? (delta, 0d) : (0d, delta);
        RaiseMouseWheel(new SharedPoint(point.X, point.Y), new SharedPoint(deltaX, deltaY));
    }

    private void ElementOnKeyDown(object sender, KeyEventArgs e)
    {
        modifiers |= MapKeyModifier(e.Key);
        RaiseKeyDown(modifiers, MapKey(e.Key));
    }

    private void ElementOnKeyUp(object sender, KeyEventArgs e)
    {
        modifiers &= ~MapKeyModifier(e.Key);
        RaiseKeyUp(modifiers, MapKey(e.Key));
    }

    private void ElementOnMouseEnter(object sender, MouseEventArgs e)
    {
        RaiseMouseEnter();
    }

    private void ElementOnMouseLeave(object sender, MouseEventArgs e)
    {
        RaiseMouseLeave();
    }

    private void ElementOnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        RaiseSizeChanged(new SharedSize(e.PreviousSize.Width, e.PreviousSize.Height),
            new SharedSize(e.NewSize.Width, e.NewSize.Height));
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
            Key.A => HostKey.A,
            Key.Z => HostKey.Z,
            Key.Y => HostKey.Y,
            Key.C => HostKey.C,
            Key.V => HostKey.V,
            _ => HostKey.Unknown
        };
    }

    private static HostMouseButton MapMouseButton(MouseButton button)
    {
        return button switch
        {
            MouseButton.Left => HostMouseButton.Left,
            MouseButton.Middle => HostMouseButton.Middle,
            MouseButton.Right => HostMouseButton.Right,
            _ => HostMouseButton.Unknown
        };
    }

    private static Cursor? MapCursor(HostCursor? cursor)
    {
        if (cursor is null)
        {
            return null;
        }

        return cursor switch
        {
            HostCursor.Arrow => Cursors.Arrow,
            HostCursor.Hand => Cursors.Hand,
            HostCursor.Text => Cursors.IBeam,
            HostCursor.SizeNs => Cursors.SizeNS,
            HostCursor.SizeNesw => Cursors.SizeNESW,
            HostCursor.SizeWe => Cursors.SizeWE,
            HostCursor.SizeNwse => Cursors.SizeNWSE,
            _ => throw new ArgumentOutOfRangeException(nameof(cursor), cursor, null)
        };
    }

    public override void Focus()
    {
        _element.Focus();
    }

    public override void Invalidate()
    {
        _element.InvalidateVisual();
    }

    public override void Dispose()
    {
        base.Dispose();

        _element.MouseDown -= ElementOnMouseDown;
        _element.MouseUp -= ElementOnMouseUp;
        _element.MouseMove -= ElementOnMouseMove;
        _element.SizeChanged -= ElementOnSizeChanged;
        _element.PreviewKeyDown -= ElementOnKeyDown;
    }
}