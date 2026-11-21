using DG.Tweening;
using UnityEngine;

public sealed partial class StageCommandParser
{
    private const string DefaultNudgeDurationToken = "10fr";
    
    #region Nudge
    private void EnqueueDirectionalNudgeSpec(
        string roleKey,
        float xSign,
        float ySign,
        string unitToken,
        string durationToken,
        string easeToken,
        CharacterRigNode node)
    {
        float pixels = YarnUnitParser.Parse(unitToken);
        float duration = YarnDurationParser.Parse(durationToken);

        // 미지정이면 종전 하드코딩 값과 같은 OutCubic이다 — 기존 대본 불변.
        Ease ease = YarnEaseParser.Parse(easeToken);

        Emit(new MoveByCommandSpecCharR
        {
            roleKey = roleKey,
            node = node,
            useAbsolutePosition = false,
            delta = new Vector2(pixels * xSign, pixels * ySign),
            duration = duration,
            ease = ease
        });
    }
    
    
    private void EnqueueNudgeLeftSpec(string roleKey, string unitToken, string durationToken, string easeToken)
        => EnqueueDirectionalNudgeSpec(roleKey, -1f, 0f, unitToken, durationToken, easeToken, CharacterRigNode.CharSlot_Track_X);

    
    private void EnqueueNudgeRightSpec(string roleKey, string unitToken, string durationToken, string easeToken)
        => EnqueueDirectionalNudgeSpec(roleKey, 1f, 0f, unitToken, durationToken, easeToken, CharacterRigNode.CharSlot_Track_X);

    
    private void EnqueueNudgeUpSpec(string roleKey, string unitToken, string durationToken, string easeToken)
        => EnqueueDirectionalNudgeSpec(roleKey, 0f, 1f, unitToken, durationToken, easeToken, CharacterRigNode.CharSlot_Track_Y);

    
    private void EnqueueNudgeDownSpec(string roleKey, string unitToken, string durationToken, string easeToken)
        => EnqueueDirectionalNudgeSpec(roleKey, 0f, -1f, unitToken, durationToken, easeToken, CharacterRigNode.CharSlot_Track_Y);
    #endregion
}
