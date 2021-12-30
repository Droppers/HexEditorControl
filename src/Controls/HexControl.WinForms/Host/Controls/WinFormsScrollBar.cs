using HexControl.SharedControl.Framework.Host.Controls;

namespace HexControl.WinForms.Host.Controls;

internal class WinFormsScrollBar : WinFormsControl, IHostScrollBar
{
    private readonly ScrollBar _scrollBar;

    public WinFormsScrollBar(ScrollBar scrollBar) : base(scrollBar)
    {
        _scrollBar = scrollBar;
        _scrollBar.Scroll += ScrollBarOnScroll;
    }

    public event EventHandler<HostScrollEventArgs>? Scroll;

    public double Value
    {
        get => _scrollBar.Value;
        set => _scrollBar.Value = (int)value;
    }

    public double Minimum
    {
        get => _scrollBar.Minimum;
        set => _scrollBar.Minimum = (int)value;
    }

    public double Maximum
    {
        get => _scrollBar.Maximum;
        set => _scrollBar.Maximum = (int)value;
    }

    public double Viewport { get; set; }
    public bool AutoHide => false;

    private void ScrollBarOnScroll(object? sender, ScrollEventArgs e)
    {
        var scrollType = e.Type switch
        {
            ScrollEventType.First => HostScrollEventType.First,
            ScrollEventType.Last => HostScrollEventType.Last,
            ScrollEventType.LargeDecrement => HostScrollEventType.LargeDecrement,
            ScrollEventType.SmallDecrement => HostScrollEventType.SmallDecrement,
            ScrollEventType.LargeIncrement => HostScrollEventType.LargeIncrement,
            ScrollEventType.SmallIncrement => HostScrollEventType.SmallIncrement,
            ScrollEventType.EndScroll => HostScrollEventType.EndScroll,
            ScrollEventType.ThumbPosition => HostScrollEventType.ThumbPosition,
            ScrollEventType.ThumbTrack => HostScrollEventType.ThumbTrack,
            _ => throw new NotSupportedException($"Scroll event type {e.Type} is not supported.")
        };
        Scroll?.Invoke(this, new HostScrollEventArgs(e.NewValue, scrollType));
    }

    public override void Dispose()
    {
        _scrollBar.Scroll -= ScrollBarOnScroll;
        base.Dispose();
    }
}