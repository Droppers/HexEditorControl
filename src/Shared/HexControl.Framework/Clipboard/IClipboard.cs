namespace HexControl.Framework.Clipboard;

internal interface IClipboard
{
    Task<(bool IsSuccess, string content)> TryReadAsync(CancellationToken cancellationToken = default);

    Task<bool> TrySetAsync(string content, CancellationToken cancellationToken = default);
}