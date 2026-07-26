using System;
using System.Threading.Tasks;

/// <summary>
/// 조건이 충족될 때까지 비동기적으로 대기한다.
/// 특정 프레임이나 UI 상태를 폴링해야 할 때만 사용한다.
/// </summary>
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