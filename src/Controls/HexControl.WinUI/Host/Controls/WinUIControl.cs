using System;
using HexControl.Core.Helpers;
using HexControl.SharedControl.Framework.Drawing;
using HexControl.SharedControl.Framework.Host;
using HexControl.SharedControl.Framework.Host.Controls;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace HexControl.WinUI.Host.Controls;

internal class WinUIControl : HostControl
{
    private readonly FrameworkElement _element;

    protected WinUIControl(FrameworkElement element)
    {
        _element = element;

        _element.PointerPressed += OnPointerPressed;
        _element.PointerMoved += OnPointerMoved;
        _element.PointerReleased += OnPointerReleased;
        _element.PointerWheelChanged += OnPointerWheelChanged;

        _element.PointerExited += OnPointerExited;
        _element.PointerEntered += OnPointerEntered;


        _element.SizeChanged += OnSizeChanged;
    }

    public override double Width => _element.ActualWidth;
    public override double Height => _element.ActualHeight;

    public override HostCursor? Cursor
    {
        get => currentCursor;
        set
        {
            if (currentCursor == value || _element is not ICursorChangeable changeableCursor)
            {
                return;
            }

            var oldCursor = changeableCursor.Cursor;
            changeableCursor.Cursor = value is null ? null! : InputSystemCursor.Create(MapCursor(value.Value));
            Disposer.SafeDispose(ref oldCursor);
            currentCursor = value;
        }
    }

    public override bool Visible
    {
        get => _element.Visibility is Visibility.Visible;
        set => _element.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }

    private static SharedPoint MapPoint(PointerPoint point) => new(point.Position.X, point.Position.Y);


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
        _element.CapturePointer(e.Pointer);

        var point = e.GetCurrentPoint(_element);
        RaiseMouseDown(HostMouseButton.Left, MapPoint(point));
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(_element);
        RaiseMouseMove(MapPoint(point));
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _element.ReleasePointerCapture(e.Pointer);

        var point = e.GetCurrentPoint(_element);
        RaiseMouseUp(HostMouseButton.Left, MapPoint(point));
    }

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(_element);
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
        _element.Focus(FocusState.Pointer);
    }

    public override void Invalidate()
    {
        _element.InvalidateMeasure();
    }
}