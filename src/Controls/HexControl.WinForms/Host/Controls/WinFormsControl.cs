using HexControl.SharedControl.Framework.Drawing;
using HexControl.SharedControl.Framework.Host;
using HexControl.SharedControl.Framework.Host.Controls;

namespace HexControl.WinForms.Host.Controls;

internal class WinFormsControl : HostControl
{
    private readonly Control _control;

    public WinFormsControl(Control control)
    {
        _control = control;
        _control.SizeChanged += ControlOnSizeChanged;
        _control.MouseDown += ControlOnMouseDown;
        _control.MouseMove += ControlOnMouseMove;
        _control.MouseUp += ControlOnMouseUp;
    }

    public override double Width => _control.Width;
    public override double Height => _control.Height;

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

    private static SharedPoint MapPoint(Point point) => new(point.X, point.Y);

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