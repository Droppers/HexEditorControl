using HexControl.Framework.Host.Controls;

namespace HexControl.WinForms.Host.Controls;

internal class WinFormsScrollBar : WinFormsControl, IHostScrollBar
{
    private const double FLOATING_POINT_MULTIPLIER = 100000d;

    private readonly ScrollBar _scrollBar;

    public WinFormsScrollBar(ScrollBar scrollBar) : base(scrollBar)
    {
        _scrollBar = scrollBar;
        _scrollBar.Scroll += ScrollBarOnScroll;
    }

    public event EventHandler<HostScrollEventArgs>? Scroll;

    public double Value
    {
        get => _scrollBar.Value / FLOATING_POINT_MULTIPLIER;
        set => _scrollBar.Value = (int)(value * FLOATING_POINT_MULTIPLIER);
    }

    public double Minimum
    {
        get => _scrollBar.Minimum / FLOATING_POINT_MULTIPLIER;
        set => _scrollBar.Minimum = (int)(value * FLOATING_POINT_MULTIPLIER);
    }

    public double Maximum
    {
        get => _scrollBar.Maximum / FLOATING_POINT_MULTIPLIER;
        set => _scrollBar.Maximum = (int)(value * FLOATING_POINT_MULTIPLIER) + (_scrollBar.LargeChange - 1);
    }

    public double Viewport { get; set; }
    public bool AutoHide => false;

    private void ScrollBarOnScroll(object? sender, ScrollEventArgs e)
    {
        if (e.Type is ScrollEventType.EndScroll)
        {
            return;
        }

        var scrollType = e.Type switch
        {
            ScrollEventType.First => HostScrollEventType.First,
            ScrollEventType.Last => HostScrollEventType.Last,
            ScrollEventType.LargeDecrement => HostScrollEventType.LargeDecrement,
            ScrollEventType.SmallDecrement => HostScrollEventType.SmallDecrement,
            ScrollEventType.LargeIncrement => HostScrollEventType.LargeIncrement,
            ScrollEventType.SmallIncrement => HostScrollEventType.SmallIncrement,
            ScrollEventType.ThumbPosition => HostScrollEventType.ThumbPosition,
            ScrollEventType.ThumbTrack => HostScrollEventType.ThumbTrack,
            _ => throw new NotSupportedException($"Scroll event type {e.Type} is not supported.")
        };
        Scroll?.Invoke(this, new HostScrollEventArgs(e.NewValue / FLOATING_POINT_MULTIPLIER, scrollType));
    }

    public override void Dispose()
    {
        _scrollBar.Scroll -= ScrollBarOnScroll;
        base.Dispose();
    }
}