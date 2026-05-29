using System;
using System.Collections;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private readonly DialogueRunner _dialogueRunner;
    private readonly YarnBridgePlaybackDriver _playbackDriver;
    private readonly RectTransform _charRigPrefab;
    
    public YarnCommandBridge(DialogueRunner dialogueRunner, YarnBridgePlaybackDriver playbackDriver, RectTransform charRigPrefab)
    {
        _dialogueRunner = dialogueRunner;
        _playbackDriver = playbackDriver;
        _charRigPrefab = charRigPrefab;
        
        BindControl();
        
        BindCharRigSetup();
        BindCharRigBasic();
        BindCharRigActing();
        BindCharRigIdle();
        BindCharRigPreset();
        
        BindCharRigEmote();
        
        BindBackgroundRig();
        BindShotResponse();
        
        BindTransition();
        BindAudio();
    }

    private void BindControl()
    {
        // Marks the next N collected commands as wait=true inside Presentation/Executor.
        // This affects command playback order, but does NOT block Yarn by itself.
        _dialogueRunner.AddCommandHandler<int>("await", AwaitFor);

        // Starts a Yarn-level hold block.
        _dialogueRunner.AddCommandHandler("hold_begin", BeginHold);

        // Blocking Yarn command:
        // closes the hold block and pauses Yarn until the held commands
        // marked with wait=true finish inside Presentation/Executor.
        //_dialogueRunner.AddCommandHandler("hold_end", (Func<IEnumerator>)(() => PlayHeldCommands()));
        _dialogueRunner.AddCommandHandler("hold_end", PlayHeldCommands);
        
        _dialogueRunner.AddCommandHandler<float>("pause", EnqueueWaitSpec);
        
        _dialogueRunner.AddCommandHandler<string>("ui_patch", EnqueueUIPatchSpec);
        
        _dialogueRunner.AddCommandHandler<float>("box_hide", EnqueueHideDialogueBoxSpec);
        _dialogueRunner.AddCommandHandler<string>("debug_log", LogImmediate);
    }
    
    private void LogImmediate(string message)
    {
        Debug.Log($"[YarnCommandBridge] {message}");
    }

    private void BindCharRigSetup()
    {
        _dialogueRunner.AddCommandHandler<string, string>("slot", EnqueueSetupCharRigSpec);
        
        _dialogueRunner.AddCommandHandler<string, string,  string, string, bool, string, string>("cast", EnqueueCastCharacterSpec);
        
        _dialogueRunner.AddCommandHandler<string, string, string, string>("pose", EnqueueSetPortraitSpriteSpec);
        _dialogueRunner.AddCommandHandler<string, string, bool, bool>("place", EnqueueSetAnchorSpecs);
        _dialogueRunner.AddCommandHandler<string, string>("size", EnqueueSetOriginSizeSpec);
        
        _dialogueRunner.AddCommandHandler<string, int, int>("place_offset", EnqueueSetAnchorOffsetSpecs);
    }

    private void BindCharRigBasic()
    {
        _dialogueRunner.AddCommandHandler<string, float>("fade_in", EnqueueFadeInSpec);
        _dialogueRunner.AddCommandHandler<string, float>("fade_out", EnqueueFadeOutSpec);
        
        _dialogueRunner.AddCommandHandler<string, string>("expression", EnqueueSetEmotionPortraitWipeSpec);
        _dialogueRunner.AddCommandHandler<string, string>("expression_crossfade", EnqueueSetPortraitCrossfadeSpec);
        
        _dialogueRunner.AddCommandHandler<string, string>("slide_in", EnqueueSlideInSpec);
        _dialogueRunner.AddCommandHandler<string, string>("slide_out", EnqueueSlideOutSpec);
        
        _dialogueRunner.AddCommandHandler<string, float, float, float>("move_by", EnqueueMoveByCharSpec);
        _dialogueRunner.AddCommandHandler<string, float, float>("scale_to", EnqueueScaleToSpec);
        _dialogueRunner.AddCommandHandler<string, int>("rotate_to", EnqueuePivotRotateToSpec);
    }
    
    private void BindCharRigActing()
    {
        _dialogueRunner.AddCommandHandler<string, string>("dip", EnqueueDipInOutSpec);
        
        _dialogueRunner.AddCommandHandler<string, int, float, float>("hop", EnqueueHopSpec);
        
        _dialogueRunner.AddCommandHandler<string, string, float, float, int>("shake", EnqueueJoltSpecShake);
        _dialogueRunner.AddCommandHandler<string, float, float, float, string>("tremble", EnqueueTrembleSpec);
        
        _dialogueRunner.AddCommandHandler<string>("sway", EnqueueSwaySpec);
    }
    
    private void BindCharRigIdle()
    {
        _dialogueRunner.AddCommandHandler<string, float, float, float, float>("idle_bounce", EnqueueBounceInPlaceSpec);
        _dialogueRunner.AddCommandHandler<string, float, float, float>("idle_breathe", EnqueueBreathInPlaceSpec);
        _dialogueRunner.AddCommandHandler<string, float, float, float, float, float, string>("idle_flinch", EnqueueTremblePulseSpec);
        _dialogueRunner.AddCommandHandler<string, float, float, float, float>("idle_walk", EnqueueWalkInPlaceSpec);
    }

    private void BindCharRigPreset()
    {
        _dialogueRunner.AddCommandHandler<string, string>("jolt", EnqueueJoltSpec);
        _dialogueRunner.AddCommandHandler<string, string>("nudge", EnqueueJoltSpecTap);
        _dialogueRunner.AddCommandHandler<string, string>("nudge_hard", EnqueueJoltSpecTapHard);
        
        _dialogueRunner.AddCommandHandler<string>("slide_in_sway", EnqueueSlideInSwayCombo);
        _dialogueRunner.AddCommandHandler<string, string>("slide_in_nudge", EnqueueSlideInJoltCombo);
        
        _dialogueRunner.AddCommandHandler<string>("sway_hard", EnqueueSwaySpecPendulum);
        _dialogueRunner.AddCommandHandler<string>("sway_fast", EnqueueSwaySpecFast);
        _dialogueRunner.AddCommandHandler<string>("sway_away", EnqueueSwaySpecAway);
    }
    
    private void BindCharRigEmote()
    {
        _dialogueRunner.AddCommandHandler<string, string>("emote", EnqueueSetCharacterEmojiSpec);
        _dialogueRunner.AddCommandHandler<string, string, string>("emote_slot", EnqueueSetCharacterEmojiSlotSpec);
        
        _dialogueRunner.AddCommandHandler<string>("emote_hide", EnqueueHideCharacterEmojiSpec);
        _dialogueRunner.AddCommandHandler<string, string>("emote_hide_slot", EnqueueHideCharacterEmojiSlotSpec);
    }
    
    private void BindBackgroundRig()
    {
        _dialogueRunner.AddCommandHandler<string, string, string, string, string, float, float, float>("spawn_bg", EnqueueSpawnBackgroundRigSpec);
        
        _dialogueRunner.AddCommandHandler<string, float, float, float>("bg_place", EnqueueSetBackgroundAnchorSpec);
        _dialogueRunner.AddCommandHandler<string, string, string>("bg_sprite", EnqueueSetBackgroundSpriteSpec);
        _dialogueRunner.AddCommandHandler<string, string>("bg_size", EnqueueSetBackgroundOriginSizeSpec);
        
        _dialogueRunner.AddCommandHandler<string, string, float>("bg_fade_in", EnqueueFadeInBackgroundSpec);
        _dialogueRunner.AddCommandHandler<string, string, float>("bg_fade_out", EnqueueFadeOutBackgroundSpec);
        
        _dialogueRunner.AddCommandHandler<string, float, float, float>("bg_move", EnqueueMoveBackgroundSpec);
        _dialogueRunner.AddCommandHandler<string, float, float>("bg_scale", EnqueueScaleBackgroundSpec);
        
        _dialogueRunner.AddCommandHandler<string, string, float, float>("bg_slide_in", EnqueueSlideInBackgroundSpec);
        _dialogueRunner.AddCommandHandler<string, string, float, float>("bg_slide_out", EnqueueSlideOutBackgroundSpec);
        _dialogueRunner.AddCommandHandler<string, string, float, float>("bg_jolt", EnqueueJoltBackgroundSpec);
        
        _dialogueRunner.AddCommandHandler<string, string, float, float>("bg_idle_tremble", EnqueueTrembleBackgroundSpec);
        _dialogueRunner.AddCommandHandler<string, float, float, float>("bg_idle_breath", EnqueueBreathBackgroundSpec);
    }
    
    private void BindShotResponse()
    {
        _dialogueRunner.AddCommandHandler<string, string>("shot_bind_bg_response", EnqueueRegisterBackgroundResponseBindingSpec);
        _dialogueRunner.AddCommandHandler<string, string>("shot_bind_char_response", EnqueueRegisterCharacterResponseBindingSpec);
        
        _dialogueRunner.AddCommandHandler<string, string, string, float, float>("shot_zoom_focus", EnqueueShotZoomFocusSpec);
        _dialogueRunner.AddCommandHandler<float, float, float, float>("shot_to", EnqueueShotToSpec);
        _dialogueRunner.AddCommandHandler("shot_reset", (Action<float>)EnqueueShotResetSpec);
        
        _dialogueRunner.AddCommandHandler<float, float>("shot_zoom", EnqueueShotZoomSpec);
        _dialogueRunner.AddCommandHandler<float, float, float>("shot_track", EnqueueShotTrackSpec);
    }
    
    private void BindTransition()
    {
        _dialogueRunner.AddCommandHandler<string, float>("tx_slant_in", EnqueueSlantedMaskCutInSpec);
        _dialogueRunner.AddCommandHandler<string, float>("tx_slant_out", EnqueueSlantedMaskCutOutSpec);

        _dialogueRunner.AddCommandHandler<string, float>("tx_strip_cover", EnqueueVerticalStripCoverSpec);
        _dialogueRunner.AddCommandHandler<string, float>("tx_strip_clear", EnqueueVerticalStripClearSpec);

        _dialogueRunner.AddCommandHandler<string, float>("tx_shutter_open", EnqueueSlantedShutterOpenSpec);
        _dialogueRunner.AddCommandHandler<string, float>("tx_shutter_close", EnqueueSlantedShutterCloseSpec);

        _dialogueRunner.AddCommandHandler<string, float>("tx_focus_fade_out", EnqueueFocusBlurFadeOutSpec);
        _dialogueRunner.AddCommandHandler<string, float>("tx_focus_fade_in", EnqueueFocusBlurFadeInSpec);

        _dialogueRunner.AddCommandHandler<string, float>("tx_focus_curtain_close", EnqueueFocusBlurCurtainCloseSpec);
        _dialogueRunner.AddCommandHandler<string, float>("tx_focus_curtain_open", EnqueueFocusBlurCurtainOpenSpec);

        _dialogueRunner.AddCommandHandler<string, float>("tx_daze_fade_close", EnqueueDazeFadeCloseSpec);
        _dialogueRunner.AddCommandHandler<string, float>("tx_daze_fade_open", EnqueueDazeFadeOpenSpec);
    }
    
    private void BindAudio()
    {
        _dialogueRunner.AddCommandHandler<string, float>("bgm", EnqueuePlayBgmSpec);
        _dialogueRunner.AddCommandHandler<float>("stop_bgm", EnqueueStopBgmSpec);

        _dialogueRunner.AddCommandHandler<string>("sfx", EnqueuePlaySfxSpec);
        _dialogueRunner.AddCommandHandler("stop_all_sfx", EnqueueStopAllSfxSpec);
        
        _dialogueRunner.AddCommandHandler<string>("voice", EnqueuePlayVoiceSpec);
        _dialogueRunner.AddCommandHandler("stop_voice", EnqueueStopVoiceSpec);
    }

    private void Collect(CommandSpecBase spec)
    {
        _playbackDriver.Enqueue(spec);
    }
}