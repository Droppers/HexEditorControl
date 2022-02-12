using System;
using HexControl.SharedControl.Framework.Drawing;
using HexControl.SharedControl.Framework.Host;
using HexControl.SharedControl.Framework.Host.Controls;
using Microsoft.UI.Input;
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
        _control.PointerWheelChanged += OnPointerWheelChanged;

        _control.PointerExited += OnPointerExited;
        _control.PointerEntered += OnPointerEntered;


        _control.SizeChanged += OnSizeChanged;
    }

    public override double Width => _control.ActualWidth;
    public override double Height => _control.ActualHeight;

    public override HostCursor? Cursor
    {
        get => currentCursor;
        set
        {
            if (currentCursor == value || _control is not ICursorChangeable changeableCursor)
            {
                return;
            }

            var oldCursor = changeableCursor.Cursor;
            changeableCursor.Cursor = value is null ? null! : InputSystemCursor.Create(MapCursor(value.Value));
            oldCursor?.Dispose();
            currentCursor = value;
        }
    }

    public override bool Visible
    {
        get => _control.Visibility is Visibility.Visible;
        set => _control.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }

    private static SharedPoint MapPoint(PointerPoint point) => new SharedPoint(point.Position.X, point.Position.Y);


    private static InputSystemCursorShape MapCursor(HostCursor cursor)
    {
        return cursor switch
        {
            HostCursor.Arrow => InputSystemCursorShape.Arrow,
            HostCursor.Hand => InputSystemCursorShape.Hand,
            HostCursor.Text => InputSystemCursorShape.IBeam,
            HostCursor.SizeNs => InputSystemCursorShape.SizeNorthSouth,
            HostCursor.SizeNesw => InputSystemCursorShape.SizeNortheastSouthwest,
            HostCursor.SizeWe => InputSystemCursorShape.SizeWestEast,
            HostCursor.SizeNwse => InputSystemCursorShape.SizeNorthwestSoutheast,
            _ => throw new ArgumentOutOfRangeException(nameof(cursor), cursor, null)
        };
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _control.CapturePointer(e.Pointer);

        var point = e.GetCurrentPoint(_control);
        RaiseMouseDown(HostMouseButton.Left, MapPoint(point));
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(_control);
        RaiseMouseMove(MapPoint(point));
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _control.ReleasePointerCapture(e.Pointer);

        var point = e.GetCurrentPoint(_control);
        RaiseMouseUp(HostMouseButton.Left, MapPoint(point));
    }

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(_control);
        RaiseMouseWheel(MapPoint(point), point.Properties.MouseWheelDelta);
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        RaiseMouseLeave();
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        RaiseMouseEnter();
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