using Avalonia.Controls.Primitives;
using HexControl.Framework.Host.Controls;

namespace HexControl.Avalonia.Host.Controls;

internal class AvaloniaScrollBar : AvaloniaControl, IHostScrollBar
{
    private readonly ScrollBar _scrollBar;

    public AvaloniaScrollBar(ScrollBar scrollBar) : base(scrollBar)
    {
        _scrollBar = scrollBar;
        _scrollBar.Scroll += ScrollBarOnScroll;
    }

    public event EventHandler<HostScrollEventArgs>? Scroll;

    public double Value
    {
        get => _scrollBar.Value;
        set => _scrollBar.Value = value;
    }

    public double Minimum
    {
        get => _scrollBar.Minimum;
        set => _scrollBar.Minimum = value;
    }

    public double Maximum
    {
        get => _scrollBar.Maximum;
        set => _scrollBar.Maximum = value;
    }

    public double Viewport
    {
        get => _scrollBar.ViewportSize;
        set => _scrollBar.ViewportSize = value;
    }

    public bool AutoHide => _scrollBar.AllowAutoHide;

    private void ScrollBarOnScroll(object? sender, ScrollEventArgs e)
    {
        var eventType = e.ScrollEventType switch
        {
            ScrollEventType.EndScroll => HostScrollEventType.EndScroll,
            ScrollEventType.LargeDecrement => HostScrollEventType.LargeDecrement,
            ScrollEventType.LargeIncrement => HostScrollEventType.LargeIncrement,
            ScrollEventType.SmallDecrement => HostScrollEventType.SmallDecrement,
            ScrollEventType.SmallIncrement => HostScrollEventType.SmallIncrement,
            ScrollEventType.ThumbTrack => HostScrollEventType.ThumbTrack,
            _ => throw new InvalidOperationException($"Scroll event type {e.ScrollEventType} is not yet supported.")
        };
        Scroll?.Invoke(this, new HostScrollEventArgs(e.NewValue, eventType));
    }

    public override void Dispose()
    {
        base.Dispose();

        _scrollBar.Scroll -= ScrollBarOnScroll;
    }
}