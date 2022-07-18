using System.Diagnostics;

namespace HexControl.Framework.Clipboard;

internal class LinuxClipboard : IClipboard
{
    private static bool? _isWsl;
    private static SemaphoreSlim _wslLock = new SemaphoreSlim(1, 1);

    private static async Task<bool> DetectWslAsync(CancellationToken cancellationToken)
    {
        await _wslLock.WaitAsync();

        try
        {
            if (_isWsl.HasValue)
            {
                return _isWsl.Value;
            }

            var result = await RunCommandAsync("uname -r", cancellationToken);
            var isWsl = result.Contains("Microsoft", StringComparison.OrdinalIgnoreCase);
            _isWsl = isWsl;
            return _isWsl.Value;
        }
        finally
        {
            _wslLock.Release();
        }
    }

    public async Task<(bool IsSuccess, string content)> TryReadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var command = await DetectWslAsync(cancellationToken)
                    ? "powershell.exe Get-Clipboard"
                    : "xsel -o --clipboard";
            var content = await RunCommandAsync(command, cancellationToken);
            return (true, content);
        }
        catch (Exception ex) when (ex is OperationCanceledException)
        {
            throw;
        }
        // ReSharper disable once RedundantCatchClause
        catch
        {
#if DEBUG
            throw;
#else
            return (false, null!);
#endif
        }
    }

    public async Task<bool> TrySetAsync(string content, CancellationToken cancellationToken = default)
    {
        string? tempFile = null;
        try
        {
            tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, content, cancellationToken);

            var command = await DetectWslAsync(cancellationToken)
                    ? $"cat \"{tempFile}\" | clip.exe"
                    : $"cat \"{tempFile}\" | xsel -i --clipboard";
            _ = await RunCommandAsync(command, cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is OperationCanceledException)
        {
            throw;
        }
        // ReSharper disable once RedundantCatchClause
        catch
        {
#if DEBUG
            throw;
#else
            return false;
#endif
        }
        finally
        {
            if (tempFile is not null && File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    private static async Task<string> RunCommandAsync(string command, CancellationToken cancellationToken)
    {
        var arguments = $"-c \"{command}\"";
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = false,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            }
        };
        process.Start();
        await process.WaitForExitAsync(cancellationToken);
        return await process.StandardOutput.ReadToEndAsync();
    }
}
