namespace HexControl.SharedControl.Framework.Drawing;

internal class RenderStateChangedEventArgs : EventArgs
{
    public RenderStateChangedEventArgs(bool canRender)
    {
        CanRender = canRender;
    }

    public bool CanRender { get; }
}

internal interface IRenderStateProvider
{
    public bool CanRender { get; set; }
    public event EventHandler<RenderStateChangedEventArgs>? RenderStateChanged;
}