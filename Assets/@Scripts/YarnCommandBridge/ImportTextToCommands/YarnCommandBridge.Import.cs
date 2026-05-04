using System;
using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private ICommandSpecSink _importSink;

    public void Import_SetSink(ICommandSpecSink sink)
    {
        _importSink = sink;
    }

    public void Import_ClearSink()
    {
        _importSink = null;
    }

    public bool Import_IsActive => _importSink != null;

    public void Import_Collect(CommandSpecBase spec)
    {
        if (spec == null)
            return;

        if (_importSink != null)
        {
            _importSink.Enqueue(spec);
            return;
        }

        _playbackDriver.Enqueue(spec);
    }

    // ---- import wrappers ------------------------------------------------

    public void Import_Slot(string roleKey)
    {
        EnqueueSetupCharRigSpec(roleKey);
    }

    public void Import_SlotBoxside(string roleKey)
    {
        EnqueueSetupCharRigSpecProtagonistSlot(roleKey);
    }

    public void Import_Place(string roleKey, string positionPreset)
    {
        EnqueueSetAnchorSpecs(roleKey, positionPreset);
    }

    public void Import_PlaceOffset(string roleKey, int x, int y)
    {
        EnqueueSetAnchorOffsetSpecs(roleKey, x, y);
    }
    
    public void Import_Size(string roleKey, string scaleArg)
    {
        EnqueueSetOriginSizeSpec(roleKey, scaleArg);
    }

    public void Import_ToScale(string roleKey, float xyValue)
    {
        EnqueueScaleToSpec(roleKey, xyValue);
    }

    public void Import_Cast(string roleKey, string character)
    {
        EnqueueCastCharAndSetPortraitCommandSpec(roleKey, character);
    }

    public void Import_Uncast(string roleKey)
    {
        EnqueueUncastCharacterSpec(roleKey);
    }

    public void Import_SlideIn(string roleKey, string direction = "left")
    {
        EnqueueSlideInSpec(roleKey, direction);
    }

    public void Import_SlideOut(string roleKey, string direction = "right")
    {
        EnqueueSlideOutSpec(roleKey, direction);
    }

    public void Import_FadeIn(string roleKey)
    {
        EnqueueFadeInSpec(roleKey);
    }

    public void Import_FadeOut(string roleKey)
    {
        EnqueueFadeOutSpec(roleKey);
    }

    public void Import_MoveBy(string roleKey, float x, float y)
    {
        EnqueueMoveBySpec(roleKey, x, y);
    }

    public void Import_Dip(string roleKey, string direction = "down")
    {
        EnqueueDipInOutSpec(roleKey, direction);
    }

    public void Import_HopIn(string roleKey, float distance, string direction = "left")
    {
        EnqueueArcHopInSpec(roleKey, distance, direction);
    }

    public void Import_Jolt(string roleKey, string direction = "right")
    {
        EnqueueJoltSpec(roleKey, direction);
    }

    public void Import_Shake(string roleKey, string direction = "right")
    {
        EnqueueJoltSpecShake(roleKey, direction);
    }

    public void Import_Nudge(string roleKey, string direction = "right")
    {
        EnqueueJoltSpecTap(roleKey, direction);
    }

    public void Import_NudgeHard(string roleKey, string direction = "down")
    {
        EnqueueJoltSpecTapHard(roleKey, direction);
    }

    public void Import_SlideInNudge(string roleKey, string direction = "right")
    {
        EnqueueSlideInJoltCombo(roleKey, direction);
    }

    public void Import_Sway(string roleKey)
    {
        EnqueueSwaySpecGentle(roleKey);
    }

    public void Import_SwayHard(string roleKey)
    {
        EnqueueSwaySpecPendulum(roleKey);
    }

    public void Import_SwayFast(string roleKey)
    {
        EnqueueSwaySpecFast(roleKey);
    }

    public void Import_SwayAway(string roleKey)
    {
        EnqueueSwaySpecAway(roleKey);
    }

    public void Import_SwayTo(string roleKey, int angle)
    {
        EnqueuePivotRotateToSpec(roleKey, angle);
    }

    public void Import_SlideInSway(string roleKey)
    {
        EnqueueSlideInSwayCombo(roleKey);
    }

    public void Import_PortraitCross(string roleKey, string character)
    {
        EnqueueSetPortraitCrossfadeSpec(roleKey, character);
    }

    public void Import_PortraitSwap(string roleKey, string character)
    {
        EnqueueSetEmotionPortraitWipeSpec(roleKey, character);
    }

    public void Import_EmotionWipe(string roleKey, string character, string variant, string emotion)
    {
        EnqueueSetEmotionPortraitWipeSpec(roleKey, character, variant, emotion);
    }

    public void Import_Blackout(string transitionMode)
    {
        EnqueueBlackoutTransitionSpec(transitionMode);
    }

    public void Import_UIPatch(string themeId)
    {
        EnqueueUIPatchSpec(themeId);
    }

    public void Import_Bgm(string clipKey, float fadeDuration = 1f)
    {
        EnqueuePlayBgmSpec(clipKey, fadeDuration);
    }

    public void Import_StopBgm(float fadeDuration = 1f)
    {
        EnqueueStopBgmSpec(fadeDuration);
    }

    public void Import_Voice(string clipKey)
    {
        EnqueuePlayVoiceSpec(clipKey);
    }

    public void Import_StopVoice()
    {
        EnqueueStopVoiceSpec();
    }

    public void Import_Sfx(string clipKey)
    {
        EnqueuePlaySfxSpec(clipKey);
    }

    public void Import_StopAllSfx()
    {
        EnqueueStopAllSfxSpec();
    }

    public void Import_Destroy(string roleKey)
    {
        EnqueueDestroySpec(roleKey);
    }

    public void Import_Emoji(string characterKey, string cue)
    {
        EnqueueInlineEmojiByCharacter(characterKey, cue);
    }

    public void Import_EmojiHide(string characterKey)
    {
        EnqueueInlineEmojiHideByCharacter(characterKey);
    }
}