using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

public sealed partial class StageCommandParser
{
    private void EnqueueFadeInSpec(string roleKey, string durationToken)
        => Emit(new FadeCommandSpecCharR
        {
            node = CharacterRigNode.CharacterPortraitSprite_Root,
            targetAlpha = NodeTargets.FadeInAlpha,
            roleKey = roleKey,
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueFadeOutSpec(string roleKey, string durationToken)
        => Emit(new FadeCommandSpecCharR
        {
            node = CharacterRigNode.CharacterPortraitSprite_Root,
            targetAlpha = NodeTargets.FadeOutAlpha,
            roleKey = roleKey,
            duration = YarnDurationParser.Parse(durationToken)
        });

    // fade(slot, alpha, dur) — 반투명까지의 페이드. fade_in · fade_out은 이것의 두 끝(1 · 0)이다.
    // [0,1] 밖이면 소리 내고 잘라 낸다 — 자르는 규칙은 코어 NodeTargets.Fade 한 곳이라 폴드와 같은 값이다.
    private void EnqueueFadeToSpec(string roleKey, float alpha, string durationToken)
    {
        if (alpha < 0f || alpha > 1f)
            Debug.LogWarning($"[StageCommandParser] <<fade>> alpha {alpha}는 [0,1] 밖이다. 잘라 낸다.");

        Emit(new FadeCommandSpecCharR
        {
            node = CharacterRigNode.CharacterPortraitSprite_Root,
            targetAlpha = NodeTargets.FadeAlpha(alpha),
            roleKey = roleKey,
            duration = YarnDurationParser.Parse(durationToken)
        });
    }

    private void EnqueueMoveByUnitCharSpec(string roleKey, string xToken, string yToken, string durationToken)
        => Emit(new MoveByCommandSpecCharR
        {
            roleKey = roleKey,
            node = CharacterRigNode.CharacterPortrait_Track,
            delta = new Vector2(YarnUnitParser.Parse(xToken), YarnUnitParser.Parse(yToken)),
            duration = YarnDurationParser.Parse(durationToken),
        });

    // 표정 교체. 밑장을 즉시 갈아끼우는 face와 달리 위 겹을 페이드로 덮었다 걷는다 —
    // 정지 프레임의 결과는 face와 같으므로 코어는 둘을 한 자리에서 접는다.
    private void EnqueueSetEmotionPortraitWipeSpec(string roleKey, string emotion, string durationToken)
        => Emit(new SetEmotionPortraitWipeCommandSpecCharR
        {
            roleKey = roleKey,
            portrait = new PortraitIdentity { emotion = emotion },
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueScaleSpec(string roleKey, float xyValue, string durationToken, string easeToken)
    {
        Ease ease = YarnEaseParser.Parse(easeToken);

        Emit(new ScaleToCommandSpecCharR
        {
            roleKey = roleKey,
            node = CharacterRigNode.CharacterPortrait_ActingScale,
            duration = YarnDurationParser.Parse(durationToken),
            toScale = new Vector2(xyValue, xyValue),
            ease = ease
        });
    }
}