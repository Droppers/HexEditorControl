using System;
using System.Windows.Controls.Primitives;
using HexControl.SharedControl.Framework.Host.Controls;

namespace HexControl.Wpf.Host.Controls;

internal class WpfScrollBar : WpfControl, IHostScrollBar
{
    private readonly ScrollBar _element;

    public WpfScrollBar(ScrollBar element) : base(element)
    {
        _element = element;
        _element.Scroll += ElementOnScroll;
    }

    public event EventHandler<HostScrollEventArgs>? Scroll;

    public double Value
    {
        get => _element.Value;
        set => _element.Value = value;
    }

    public double Minimum
    {
        get => _element.Minimum;
        set => _element.Minimum = value;
    }

    public double Maximum
    {
        get => _element.Maximum;
        set => _element.Maximum = value;
    }

    public double Viewport
    {
        get => _element.ViewportSize;
        set => _element.ViewportSize = value;
    }

    public bool AutoHide => false;

    private void ElementOnScroll(object sender, ScrollEventArgs e)
    {
        var eventType = e.ScrollEventType switch
        {
            ScrollEventType.EndScroll => HostScrollEventType.EndScroll,
            ScrollEventType.First => HostScrollEventType.First,
            ScrollEventType.LargeDecrement => HostScrollEventType.LargeDecrement,
            ScrollEventType.LargeIncrement => HostScrollEventType.LargeIncrement,
            ScrollEventType.Last => HostScrollEventType.Last,
            ScrollEventType.SmallDecrement => HostScrollEventType.SmallDecrement,
            ScrollEventType.SmallIncrement => HostScrollEventType.SmallIncrement,
            ScrollEventType.ThumbPosition => HostScrollEventType.ThumbPosition,
            ScrollEventType.ThumbTrack => HostScrollEventType.ThumbTrack,
            _ => throw new ArgumentOutOfRangeException()
        };
        Scroll?.Invoke(this, new HostScrollEventArgs(e.NewValue, eventType));
    }

    public override void Dispose()
    {
        base.Dispose();

        _element.Scroll -= ElementOnScroll;
    }
}