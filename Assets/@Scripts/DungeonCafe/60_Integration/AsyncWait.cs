using System;
using System.Threading.Tasks;

public static class AsyncWait
{
    public static async Task UntilAsync(Func<bool> predicate)
    {
        if (predicate == null)
            return;

        while (!predicate())
            await Task.Yield();
    }

    public static async Task UntilAsync(
        Func<bool> predicate,
        Func<bool> cancelIf)
    {
        if (predicate == null)
            return;

        while (!predicate())
        {
            if (cancelIf != null && cancelIf())
                return;

            await Task.Yield();
        }
    }
}