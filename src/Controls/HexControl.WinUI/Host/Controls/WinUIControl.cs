using HexControl.SharedControl.Framework.Drawing;
using HexControl.SharedControl.Framework.Host;
using HexControl.SharedControl.Framework.Host.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace HexControl.WinUI.Host.Controls;

internal class WinUIControl : HostControl
{
    private readonly Control _control;

    public WinUIControl(Control control)
    {
        _control = control;

        _control.PointerPressed += OnPointerPressed;
        _control.PointerMoved += OnPointerMoved;
        _control.PointerReleased += OnPointerReleased;

        _control.SizeChanged += OnSizeChanged;
    }

    public override double Width => _control.ActualWidth;
    public override double Height => _control.ActualHeight;

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(_control).Position;
        RaiseMouseUp(HostMouseButton.Left, new SharedPoint(point.X, point.Y));
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(_control).Position;
        RaiseMouseMove(new SharedPoint(point.X, point.Y));
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(_control).Position;
        RaiseMouseDown(HostMouseButton.Left, new SharedPoint(point.X, point.Y));
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        RaiseSizeChanged(new SharedSize(e.PreviousSize.Width, e.PreviousSize.Height),
            new SharedSize(e.NewSize.Width, e.NewSize.Height));
    }

    public override void Focus()
    {
        _control.Focus(FocusState.Programmatic);
    }

    public override void Invalidate()
    {
        _control.InvalidateMeasure();
    }
}