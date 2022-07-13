namespace HexControl.Framework;

public static class Disposer
{
    public static void SafeDispose<T>(ref T? resource) where T : class
    {
        if (resource is IDisposable disposer)
        {
            try
            {
                disposer.Dispose();
            }
            catch
            {
                // ignored
            }
        }

        resource = null;
    }
}