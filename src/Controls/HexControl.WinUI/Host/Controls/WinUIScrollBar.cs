using System;
using HexControl.SharedControl.Framework.Host.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace HexControl.WinUI.Host.Controls;

internal class WinUIScrollBar : WinUIControl, IHostScrollBar
{
    private readonly ScrollBar _scrollBar;

    public WinUIScrollBar(ScrollBar scrollBar) : base(scrollBar)
    {
        _scrollBar = scrollBar;
        _scrollBar.Scroll += OnScroll;
    }

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

    public bool AutoHide => true;

    public event EventHandler<HostScrollEventArgs>? Scroll;

    private void OnScroll(object sender, ScrollEventArgs e)
    {
        var scrollType = e.ScrollEventType switch
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
        Scroll?.Invoke(this, new HostScrollEventArgs(e.NewValue, scrollType));
    }

    public override void Dispose()
    {
        _scrollBar.Scroll -= OnScroll;

        base.Dispose();
    }
}