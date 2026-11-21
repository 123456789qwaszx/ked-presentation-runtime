using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

public sealed partial class StageCommandParser
{
    private const string DefaultDepthDurationToken = "13fr";
    
    #region CharDepth
    // size(slot, depth, [keep_y], dur, ease) — 그 사람의 크기. 깊이 레벨의 Y와 배율을 그대로 놓는다. 무대를 재지 않는다.
    //   레벨 · 라벨   level 커브의 Y와 배율. keep_y면 배율만(Y는 지금 값 그대로)
    //   x배율         배율을 직접(절대값). 레벨이 없어 Y의 근거가 없으므로 Y는 건드리지 않는다
    // 토큰은 코어 SizeToken이 읽는다 — 폴드와 같은 함수다.
    private void EnqueueDepthAtPresetSpec(string roleKey, string depthArg, bool keepY, string durationToken, string easeToken)
    {
        var spec = new SetDepthCommandSpecCharR
        {
            roleKey = roleKey,
            keepY = keepY,
            duration = YarnDurationParser.Parse(durationToken),
        };

        if (SizeToken.TryParseScale(depthArg, out float scale))
        {
            spec.useScale = true;
            spec.scale = scale;
        }
        else if (CharacterDepthPresetParser.TryParseDepthLevel(depthArg, out float level))
        {
            spec.useLevel = true;
            spec.level = level;
        }
        else
        {
            spec.useLevel = false;

            if (!CharacterDepthPresetParser.TryParse(depthArg, out CharacterDepthKey preset))
            {
                Debug.LogWarning(
                    $"[StageCommandParser] Unknown depth preset '{depthArg}'. " +
                    $"Fallback to '{CharacterDepthKey.Mid}'.");

                preset = CharacterDepthKey.Mid;
            }

            spec.preset = preset;
        }

        Ease ease = YarnEaseParser.Parse(easeToken);
        spec.ease = ease;

        Emit(spec);
    }

    private void EnqueueDepthResetSpec(string roleKey, float duration, string easeToken)
    {
        Ease ease = YarnEaseParser.Parse(easeToken);

        Emit(new SetDepthCommandSpecCharR
        {
            roleKey = roleKey,
            preset = CharacterDepthKey.Mid,
            useLevel = false,
            duration = duration,
            ease = ease
        });
    }
    #endregion
}
