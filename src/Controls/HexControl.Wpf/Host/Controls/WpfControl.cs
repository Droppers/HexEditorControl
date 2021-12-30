using System.Windows;
using System.Windows.Input;
using HexControl.SharedControl.Framework.Drawing;
using HexControl.SharedControl.Framework.Host;
using HexControl.SharedControl.Framework.Host.Controls;

namespace HexControl.Wpf.Host.Controls;

internal class WpfControl : HostControl
{
    private readonly FrameworkElement _element;
    private HostKeyModifier _modifiers = HostKeyModifier.Default;

    public WpfControl(FrameworkElement element)
    {
        _element = element;
        element.Focusable = true;
        element.MouseDown += ElementOnMouseDown;
        element.MouseUp += ElementOnMouseUp;
        element.MouseMove += ElementOnMouseMove;
        element.SizeChanged += ElementOnSizeChanged;
        element.PreviewKeyDown += ElementOnKeyDown;
        element.PreviewKeyUp += ElementOnKeyUp;
        element.MouseWheel += ElementOnMouseWheel;
    }

    public override double Width => _element.ActualWidth;

    public override double Height => _element.ActualHeight;

    private void ElementOnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var point = e.GetPosition(_element);
        RaiseMouseWheel(new SharedPoint(point.X, point.Y), e.Delta);
    }

    private void ElementOnKeyDown(object sender, KeyEventArgs e)
    {
        _modifiers |= MapKeyModifier(e.Key);
        RaiseKeyDown(e.IsRepeat, e.IsUp, e.IsDown, _modifiers, MapKey(e.Key));
    }

    private void ElementOnKeyUp(object sender, KeyEventArgs e)
    {
        _modifiers &= ~MapKeyModifier(e.Key);
        RaiseKeyUp(e.IsRepeat, e.IsUp, e.IsDown, _modifiers, MapKey(e.Key));
    }


    private void ElementOnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        RaiseSizeChanged(new SharedSize(e.PreviousSize.Width, e.PreviousSize.Height),
            new SharedSize(e.NewSize.Width, e.NewSize.Height));
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