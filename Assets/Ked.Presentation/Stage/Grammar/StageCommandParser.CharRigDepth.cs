using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

public sealed partial class StageCommandParser
{
    private const string DefaultDepthDurationToken = "13fr";
    
    #region CharDepth
    // size(slot, depth, dur, ease) — 깊이 레벨의 Y와 배율을 그대로 놓는다. 무대를 재지 않는다.
    private void EnqueueDepthAtPresetSpec(string roleKey, string depthArg, string durationToken, string easeToken)
    {
        var spec = new SetDepthCommandSpecCharR
        {
            roleKey = roleKey,
            duration = YarnDurationParser.Parse(durationToken),
        };

        if (CharacterDepthPresetParser.TryParseDepthLevel(depthArg, out float level))
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
