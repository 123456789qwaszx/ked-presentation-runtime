using DG.Tweening;
using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private const string DefaultShowFaceToken = "e2";
    private const string DefaultShowDurationToken = "14fr";
    
    private const string DefaultNudgeDurationToken = "8fr";
    
    private const string DefaultMoveOneUnitPerFrameToken = "1fr";
    
    #region Show
    private void EnqueueShowAtSpec(
        string roleKey,
        string faceToken = DefaultShowFaceToken,
        string positionPreset = "center",
        string durationToken = DefaultShowDurationToken)
    {
        var spec0SetAnchor = new SetAnchorCommandSpecCharR
        {
            slotKey = roleKey,
            preset = CharAnchorPresetParser.Parse(positionPreset),
            resetSlotPos = true,
            resetCharacterPos = true
        };
        
        var spec1SetPortraitSprite = new SetPortraitSpriteCommandSpecCharR
        {
            slotKey = roleKey,
            portrait = new PortraitIdentity { emotion = ShowFaceAliasParser.Parse(faceToken) }
        };
        
        var spec2FadeInPortraitSprite = new FadeInCommandSpecCharR
        {
            target = CharacterRigTarget.CharacterPortraitSprite_Root,
            
            slotKey = roleKey,
            duration = YarnDurationParser.Parse(durationToken)
        };

        Collect(spec0SetAnchor);
        Collect(spec1SetPortraitSprite);
        Collect(spec2FadeInPortraitSprite);
    }

    private void EnqueueShowAtLeftSpec(
        string roleKey,
        string faceToken = DefaultShowFaceToken,
        string durationToken = DefaultShowDurationToken)
        => EnqueueShowAtSpec(roleKey, faceToken, "left", durationToken);

    private void EnqueueShowAtCenterSpec(
        string roleKey,
        string faceToken = DefaultShowFaceToken,
        string durationToken = DefaultShowDurationToken)
        => EnqueueShowAtSpec(roleKey, faceToken, "center", durationToken);

    private void EnqueueShowAtRightSpec(
        string roleKey,
        string faceToken = DefaultShowFaceToken,
        string durationToken = DefaultShowDurationToken)
        => EnqueueShowAtSpec(roleKey, faceToken, "right", durationToken);

    private void EnqueueShowAtDuoLeftSpec(
        string roleKey,
        string faceToken = DefaultShowFaceToken,
        string durationToken = DefaultShowDurationToken)
        => EnqueueShowAtSpec(roleKey, faceToken, "duoleft", durationToken);

    private void EnqueueShowAtDuoRightSpec(
        string roleKey,
        string faceToken = DefaultShowFaceToken,
        string durationToken = DefaultShowDurationToken)
        => EnqueueShowAtSpec(roleKey, faceToken, "duoright", durationToken);

    private void EnqueueShowSpec(
        string roleKey,
        string faceToken = DefaultShowFaceToken,
        string positionPreset = "center",
        string durationToken = DefaultShowDurationToken)
        => EnqueueShowAtSpec(roleKey, faceToken, positionPreset, durationToken);
    #endregion
    
    #region Nudge
    private void EnqueueDirectionalNudgeSpec(
        string roleKey,
        float xSign,
        float ySign,
        string unitToken,
        string durationToken,
        CharacterRigTarget target)
    {
        float pixels = YarnUnitParser.Parse(unitToken);
        float duration = YarnDurationParser.Parse(durationToken);

        var spec = new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = target,
            useAbsolutePosition = false,
            delta = new Vector2(pixels * xSign, pixels * ySign),
            duration = duration,
            ease = Ease.OutCubic
        };

        Collect(spec);
    }
    
    private void EnqueueNudgeLeftSpec(
        string roleKey, 
        string unitToken, 
        string durationToken = DefaultNudgeDurationToken)
        => EnqueueDirectionalNudgeSpec(roleKey, -1f, 0f, unitToken, durationToken, CharacterRigTarget.CharSlot_Track_X);

    private void EnqueueNudgeRightSpec(
        string roleKey,
        string unitToken, 
        string durationToken = DefaultNudgeDurationToken)
        => EnqueueDirectionalNudgeSpec(roleKey, 1f, 0f, unitToken, durationToken, CharacterRigTarget.CharSlot_Track_X);

    private void EnqueueNudgeUpSpec(
        string roleKey, 
        string unitToken, 
        string durationToken = DefaultNudgeDurationToken)
        => EnqueueDirectionalNudgeSpec(roleKey, 0f, 1f, unitToken, durationToken, CharacterRigTarget.CharSlot_Track_Y);

    private void EnqueueNudgeDownSpec(
        string roleKey, 
        string unitToken, string durationToken = DefaultNudgeDurationToken)
        => EnqueueDirectionalNudgeSpec(roleKey, 0f, -1f, unitToken, durationToken, CharacterRigTarget.CharSlot_Track_Y);
    #endregion
    
    #region MoveOneUnitPerFrame
    private void EnqueueDirectionalMoveOneUnitPerFrameSpec(
        string roleKey,
        float xSign,
        float ySign,
        string frameToken,
        CharacterRigTarget target)
    {
        float frames = YarnDurationParser.ParseFrames(frameToken, 8f);

        float pixels = YarnUnitParser.Parse("1u") * frames;
        float duration = YarnDurationParser.FramesToSeconds(frames);

        var spec = new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = target,
            useAbsolutePosition = false,
            delta = new Vector2(pixels * xSign, pixels * ySign),
            duration = duration,
            ease = Ease.Linear
        };

        Collect(spec);
    }
    
    private void EnqueueMoveLeftOneUnitPerFrameSpec(
        string roleKey,
        string frameToken = DefaultMoveOneUnitPerFrameToken)
        => EnqueueDirectionalMoveOneUnitPerFrameSpec(roleKey, -1f, 0f, frameToken, CharacterRigTarget.CharSlot_Track_X);

    private void EnqueueMoveRightOneUnitPerFrameSpec(
        string roleKey, 
        string frameToken = DefaultMoveOneUnitPerFrameToken)
        => EnqueueDirectionalMoveOneUnitPerFrameSpec(roleKey, 1f, 0f, frameToken, CharacterRigTarget.CharSlot_Track_X);

    private void EnqueueMoveUpOneUnitPerFrameSpec(
        string roleKey,
        string frameToken = DefaultMoveOneUnitPerFrameToken)
        => EnqueueDirectionalMoveOneUnitPerFrameSpec(roleKey, 0f, 1f, frameToken, CharacterRigTarget.CharSlot_Track_Y);

    private void EnqueueMoveDownOneUnitPerFrameSpec(
        string roleKey, 
        string frameToken = DefaultMoveOneUnitPerFrameToken)
        => EnqueueDirectionalMoveOneUnitPerFrameSpec(roleKey, 0f, -1f, frameToken, CharacterRigTarget.CharSlot_Track_Y);
    #endregion
}