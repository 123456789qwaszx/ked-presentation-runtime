using System.Collections.Generic;

/// <summary>
/// 접객 종료 시점의 붕괴도를 결산 배율로 환산한다.
/// 밴드는 ProgressionTuning 에서 오름차순 정렬되어 들어온다.
/// </summary>
public static class CollapseMultiplierTable
{
    public static CollapseMultiplierBand Resolve(ProgressionTuning tuning, int collapse)
    {
        IReadOnlyList<CollapseMultiplierBand> bands = tuning.MultiplierBands;

        CollapseMultiplierBand resolved = bands[0];

        for (int i = 0; i < bands.Count; i++)
        {
            if (collapse < bands[i].MinCollapse)
                break;

            resolved = bands[i];
        }

        return resolved;
    }

    public static int ApplyToScore(int reactionScore, float multiplier)
    {
        if (reactionScore <= 0)
            return 0;

        return (int)System.Math.Floor(reactionScore * multiplier);
    }
}
