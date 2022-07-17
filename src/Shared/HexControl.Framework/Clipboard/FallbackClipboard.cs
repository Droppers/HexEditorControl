namespace HexControl.Framework.Clipboard;

internal class FallbackClipboard : IClipboard
{
    public Task<(bool IsSuccess, string content)> TryReadAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult((false, string.Empty));
    }

    public Task<bool> TrySetAsync(string content, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }
}
