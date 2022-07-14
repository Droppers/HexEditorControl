using HexControl.Framework.Drawing;
using HexControl.Framework.Host;
using HexControl.Framework.Host.Controls;

namespace HexControl.WinForms.Host.Controls;

internal class WinFormsControl : HostControl
{
    private readonly Control _control;
    protected HostKeyModifier modifiers = HostKeyModifier.Default;

    protected WinFormsControl(Control control)
    {
        _control = control;

        _control.SizeChanged += ControlOnSizeChanged;

        _control.MouseDown += ControlOnMouseDown;
        _control.MouseMove += ControlOnMouseMove;
        _control.MouseUp += ControlOnMouseUp;
        _control.MouseWheel += ControlOnMouseWheel;

        _control.MouseLeave += ControlOnMouseLeave;
        _control.MouseEnter += ControlOnMouseEnter;

        _control.KeyDown += ControlOnKeyDown;
        _control.KeyUp += ControlOnKeyUp;
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

            _control.Cursor = MapCursor(value);
            currentCursor = value;
        }
    }

    public override bool Visible
    {
        get => _control.Visible;
        set => _control.Visible = value;
    }

    public override double Width => ConvertDpi(_control.Width);
    public override double Height => ConvertDpi(_control.Height);

    private static Cursor MapCursor(HostCursor? cursor)
    {
        if (cursor is null)
        {
            return Cursors.Default;
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

    private void ControlOnMouseLeave(object? sender, EventArgs e)
    {
        RaiseMouseLeave();
    }

    private void ControlOnMouseEnter(object? sender, EventArgs e)
    {
        RaiseMouseEnter();
    }

    private void ControlOnMouseWheel(object? sender, MouseEventArgs e)
    {
        RaiseMouseWheel(MapPoint(e.Location), e.Delta);
    }

    private void ControlOnSizeChanged(object? sender, EventArgs e)
    {
        RaiseSizeChanged(new SharedSize(Width, Height), new SharedSize(Width, Height));
    }

    private void ControlOnMouseDown(object? sender, MouseEventArgs e)
    {
        RaiseMouseDown(MapButton(e.Button), MapPoint(e.Location));
    }

    private void ControlOnMouseMove(object? sender, MouseEventArgs e)
    {
        RaiseMouseMove(MapPoint(e.Location));
    }

    private void ControlOnMouseUp(object? sender, MouseEventArgs e)
    {
        RaiseMouseUp(MapButton(e.Button), MapPoint(e.Location));
    }

    private void ControlOnKeyDown(object? sender, KeyEventArgs e)
    {
        modifiers |= MapKeyModifier(e.KeyCode);
        RaiseKeyDown(modifiers, MapKey(e.KeyCode));
    }

    private void ControlOnKeyUp(object? sender, KeyEventArgs e)
    {
        modifiers &= ~MapKeyModifier(e.KeyCode);
        RaiseKeyUp(modifiers, MapKey(e.KeyCode));
    }

    private static HostKeyModifier MapKeyModifier(Keys key)
    {
        return key switch
        {
            Keys.Control => HostKeyModifier.Control,
            Keys.ControlKey => HostKeyModifier.Control,
            Keys.LControlKey => HostKeyModifier.Control,
            Keys.RControlKey => HostKeyModifier.Control,
            Keys.Shift => HostKeyModifier.Shift,
            Keys.ShiftKey => HostKeyModifier.Shift,
            Keys.LShiftKey => HostKeyModifier.Shift,
            Keys.RShiftKey => HostKeyModifier.Shift,
            _ => HostKeyModifier.Default
        };
    }

    private static HostKey MapKey(Keys key)
    {
        return key switch
        {
            Keys.Left => HostKey.Left,
            Keys.Up => HostKey.Up,
            Keys.Right => HostKey.Right,
            Keys.Down => HostKey.Down,
            Keys.Control => HostKey.Control,
            Keys.ControlKey => HostKey.Control,
            Keys.LControlKey => HostKey.Control,
            Keys.RControlKey => HostKey.Control,
            Keys.Shift => HostKey.Shift,
            Keys.ShiftKey => HostKey.Shift,
            Keys.LShiftKey => HostKey.Shift,
            Keys.RShiftKey => HostKey.Shift,
            Keys.Back => HostKey.Back,
            Keys.Delete => HostKey.Delete,
            Keys.Z => HostKey.Z,
            Keys.Y => HostKey.Y,
            Keys.A => HostKey.A,
            _ => HostKey.Unknown
        };
    }

    private SharedPoint MapPoint(Point point)
    {
        return new SharedPoint(ConvertDpi(point.X), ConvertDpi(point.Y));
    }

    private float ConvertDpi(float value)
    {
        return value / (_control.DeviceDpi / 96f);
    }

    private static HostMouseButton MapButton(MouseButtons button)
    {
        return button switch
        {
            MouseButtons.Left => HostMouseButton.Left,
            MouseButtons.Middle => HostMouseButton.Middle,
            MouseButtons.Right => HostMouseButton.Right,
            _ => HostMouseButton.Unknown
        };
    }

    public override void Focus()
    {
        _control.Focus();
    }

    public override void Invalidate()
    {
        _control.Invalidate();
    }

    public override void Dispose()
    {
        _control.SizeChanged -= ControlOnSizeChanged;
        _control.MouseDown -= ControlOnMouseDown;
        _control.MouseMove -= ControlOnMouseMove;
        _control.MouseUp -= ControlOnMouseUp;

        base.Dispose();
    }
}