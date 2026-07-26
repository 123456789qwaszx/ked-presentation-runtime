using System;
using Yarn.Unity;

/// <summary>
/// 조건 대기 헬퍼.
/// 기존 프레젠테이션 레이어와 동일하게 YarnTask.Yield 폴링을 쓴다.
/// 완료 소스 기반으로 바꾸더라도 호출부는 그대로 유지된다.
/// </summary>
public static class YarnWait
{
    public static async YarnTask UntilAsync(Func<bool> predicate)
    {
        if (predicate == null)
            return;

        while (!predicate())
            await YarnTask.Yield();
    }

    public static async YarnTask UntilAsync(Func<bool> predicate, Func<bool> cancelIf)
    {
        if (predicate == null)
            return;

        while (!predicate())
        {
            if (cancelIf != null && cancelIf())
                return;

            await YarnTask.Yield();
        }
    }
}
