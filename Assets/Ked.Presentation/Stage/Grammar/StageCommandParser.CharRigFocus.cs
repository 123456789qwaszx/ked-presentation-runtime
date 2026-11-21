using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

public sealed partial class StageCommandParser
{
    private const string DefaultFocusPlacementFocusToken = "face";
    private const string DefaultFocusPlacementDurationToken = "0fr";
    
    private const string DefaultDepthDurationToken = "13fr";
    
    #region CharFocusPlacement
    private void EnqueueFocusToLeftSpec(string roleKey, string focus, string durationToken, string easeToken)
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.Left, durationToken, easeToken);

    private void EnqueueFocusToCenterSpec(string roleKey, string focus, string durationToken, string easeToken)
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.Center, durationToken, easeToken);

    private void EnqueueFocusToRightSpec(string roleKey, string focus, string durationToken, string easeToken)
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.Right, durationToken, easeToken);

    private void EnqueuePlaceCharacterFocusToSpec(
        string roleKey,
        string focus,
        ScreenFocusPoint screenPoint,
        string durationToken,
        string easeToken = "")
    {
        Ease ease = YarnEaseParser.Parse(easeToken);

        Emit(new PlaceCharacterFocusCommandSpecCharR
        {
            roleKey = roleKey,
            focusPreset = CharacterFocusPresetParser.Parse(focus),
            screenPoint = screenPoint,
            moveNode = CharacterRigNode.CharSlot_Track_Focus,
            duration = YarnDurationParser.Parse(durationToken),
            ease = ease
        });
    }

    private void EnqueuePlaceCharacterFocusSpec(string roleKey, string focus, string screenPoint, string durationToken, string easeToken)
    {
        Ease ease = YarnEaseParser.Parse(easeToken);

        Emit(new PlaceCharacterFocusCommandSpecCharR
        {
            roleKey = roleKey,
            focusPreset = CharacterFocusPresetParser.Parse(focus),
            screenPoint = ParseScreenPoint("place", screenPoint),
            moveNode = CharacterRigNode.CharSlot_Track_Focus,
            duration = YarnDurationParser.Parse(durationToken),
            ease = ease
        });
    }
    #endregion
    
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
