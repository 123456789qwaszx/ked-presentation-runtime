using System;
using System.Collections;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private readonly DialogueRunner _dialogueRunner;
    private readonly DialogueRunner _subPresentationRunner;
    
    private readonly YarnBridgePlaybackDriver _playbackDriver;
    private readonly RectTransform _charRigPrefab;
    
    public YarnCommandBridge(
        DialogueRunner dialogueRunner,
        DialogueRunner subPresentationRunner,
        YarnBridgePlaybackDriver playbackDriver, 
        RectTransform charRigPrefab)
    {
        _dialogueRunner = dialogueRunner;
        _subPresentationRunner = subPresentationRunner;
        
        _playbackDriver = playbackDriver;
        _charRigPrefab = charRigPrefab;
        
        BindRunnerCommands(_dialogueRunner);
        BindRunnerCommands(_subPresentationRunner);

        // Main Runner only commands.
        _dialogueRunner.AddCommandHandler<string>("sub_start", StartSubPresentationNode);
        _dialogueRunner.AddCommandHandler<string>("a", EnqueueSubPresentationAdvanceSpec);
    }
    
    private IEnumerator StartSubPresentationNode(string nodeName)
    {
        _subPresentationRunner.StartDialogue(nodeName);

        const int minWaitFrames = 10;

        for (int frame = 0; frame < minWaitFrames; frame++)
            yield return null;
    }
    
    private void EnqueueSubPresentationAdvanceSpec(string _ = "doNothing") => Collect(new SubPresentationAdvanceCommandSpec());
    
    private void BindRunnerCommands(DialogueRunner runner)
    {
        if (runner == null)
            Debug.LogWarning("[YarnCommandBridge] Cannot bind commands. DialogueRunner is null.");
        
        BindControl(runner);

        BindCharRigSetup(runner);
        BindCharRigBasic(runner);
        BindCharRigActing(runner);
        BindCharRigIdle(runner);
        BindCharRigPreset(runner);

        BindCharRigEmote(runner);

        BindBackgroundRig(runner);
        BindShotResponse(runner);

        BindTransition(runner);
        BindAudio(runner);
    }
    

    private void BindControl(DialogueRunner runner)
    {
        // Starts capturing commands into a virtual command block.
        // Commands after <<capture_block>> are collected as one block-level execution unit.
        // Yarn timing is held until <<block_end>> plays the block.
        runner.AddCommandHandler("capture_block", BeginBlockCapture);
        runner.AddCommandHandler<float>("play_block", PlayCapturedBlock);
        
        runner.AddCommandHandler<float>("pause", EnqueueWaitSpec);
        
        runner.AddCommandHandler<string>("ui_patch", EnqueueUIPatchSpec);
        
        runner.AddCommandHandler<float>("box_hide", EnqueueHideDialogueBoxSpec);
        
        runner.AddCommandHandler<string>("debug_log", LogImmediate);
        runner.AddCommandHandler<string>("debug_state", LogYarnState);
    }
    

    private void BindCharRigSetup(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>("slot", EnqueueSetupCharRigSpec);
        
        runner.AddCommandHandler<string, string, string, string, bool, string, string>("cast", EnqueueCastCharacterSpec);
        
        runner.AddCommandHandler<string, string, string, string>("pose", EnqueueSetPortraitSpriteSpec);
        runner.AddCommandHandler<string, string, bool, bool>("place", EnqueueSetAnchorSpecs);
        runner.AddCommandHandler<string, string>("size", EnqueueSetOriginSizeSpec);
        
        runner.AddCommandHandler<string, int, int>("place_offset", EnqueueSetAnchorOffsetSpecs);
    }

    private void BindCharRigBasic(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, float>("fade_in", EnqueueFadeInSpec);
        runner.AddCommandHandler<string, float>("fade_out", EnqueueFadeOutSpec);
        
        runner.AddCommandHandler<string, string>("expression", EnqueueSetEmotionPortraitWipeSpec);
        runner.AddCommandHandler<string, string>("expression_crossfade", EnqueueSetPortraitCrossfadeSpec);
        
        runner.AddCommandHandler<string, string>("slide_in", EnqueueSlideInSpec);
        runner.AddCommandHandler<string, string>("slide_out", EnqueueSlideOutSpec);
        
        runner.AddCommandHandler<string, float, float, float>("move_by", EnqueueMoveByCharSpec);
        runner.AddCommandHandler<string, float, float>("scale_to", EnqueueScaleToSpec);
        runner.AddCommandHandler<string, int>("rotate_to", EnqueuePivotRotateToSpec);
    }
    
    private void BindCharRigActing(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>("dip", EnqueueDipInOutSpec);
        
        runner.AddCommandHandler<string, int, float, float>("hop", EnqueueHopSpec);
        
        runner.AddCommandHandler<string, string, float, float, int>("shake", EnqueueJoltSpecShake);
        runner.AddCommandHandler<string, float, float, float, string>("tremble", EnqueueTrembleSpec);
        
        runner.AddCommandHandler<string>("sway", EnqueueSwaySpec);
    }
    
    private void BindCharRigIdle(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, float, float, float, float>("idle_bounce", EnqueueBounceInPlaceSpec);
        runner.AddCommandHandler<string, float, float, float>("idle_breathe", EnqueueBreathInPlaceSpec);
        runner.AddCommandHandler<string, float, float, float, float, float, string>("idle_flinch", EnqueueTremblePulseSpec);
        runner.AddCommandHandler<string, float, float, float, float>("idle_walk", EnqueueWalkInPlaceSpec);
    }

    private void BindCharRigPreset(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>("jolt", EnqueueJoltSpec);
        runner.AddCommandHandler<string, string>("nudge", EnqueueJoltSpecTap);
        runner.AddCommandHandler<string, string>("nudge_hard", EnqueueJoltSpecTapHard);
        
        runner.AddCommandHandler<string>("slide_in_sway", EnqueueSlideInSwayCombo);
        runner.AddCommandHandler<string, string>("slide_in_nudge", EnqueueSlideInJoltCombo);
        
        runner.AddCommandHandler<string>("sway_hard", EnqueueSwaySpecPendulum);
        runner.AddCommandHandler<string>("sway_fast", EnqueueSwaySpecFast);
        runner.AddCommandHandler<string>("sway_away", EnqueueSwaySpecAway);
    }
    
    private void BindCharRigEmote(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>("emote", EnqueueSetCharacterEmojiSpec);
        runner.AddCommandHandler<string, string, string>("emote_slot", EnqueueSetCharacterEmojiSlotSpec);
        
        runner.AddCommandHandler<string>("emote_hide", EnqueueHideCharacterEmojiSpec);
        runner.AddCommandHandler<string, string>("emote_hide_slot", EnqueueHideCharacterEmojiSlotSpec);
    }
    
    private void BindBackgroundRig(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string, string, string, float, float, float>("spawn_bg", EnqueueSpawnBackgroundRigSpec);
        
        runner.AddCommandHandler<string, float, float, float>("bg_place", EnqueueSetBackgroundAnchorSpec);
        runner.AddCommandHandler<string, string, string>("bg_sprite", EnqueueSetBackgroundSpriteSpec);
        runner.AddCommandHandler<string, string>("bg_size", EnqueueSetBackgroundOriginSizeSpec);
        
        runner.AddCommandHandler<string, string, float>("bg_fade_in", EnqueueFadeInBackgroundSpec);
        runner.AddCommandHandler<string, string, float>("bg_fade_out", EnqueueFadeOutBackgroundSpec);
        
        runner.AddCommandHandler<string, float, float, float>("bg_move", EnqueueMoveBackgroundSpec);
        runner.AddCommandHandler<string, float, float>("bg_scale", EnqueueScaleBackgroundSpec);
        
        runner.AddCommandHandler<string, string, float, float>("bg_slide_in", EnqueueSlideInBackgroundSpec);
        runner.AddCommandHandler<string, string, float, float>("bg_slide_out", EnqueueSlideOutBackgroundSpec);
        runner.AddCommandHandler<string, string, float, float>("bg_jolt", EnqueueJoltBackgroundSpec);
        
        runner.AddCommandHandler<string, string, float, float>("bg_idle_tremble", EnqueueTrembleBackgroundSpec);
        runner.AddCommandHandler<string, float, float, float>("bg_idle_breath", EnqueueBreathBackgroundSpec);
    }
    
    private void BindShotResponse(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>("shot_bind_bg_response", EnqueueRegisterBackgroundResponseBindingSpec);
        runner.AddCommandHandler<string, string>("shot_bind_char_response", EnqueueRegisterCharacterResponseBindingSpec);
        
        runner.AddCommandHandler<string, string, string, float, float>("shot_zoom_focus", EnqueueShotZoomFocusSpec);
        runner.AddCommandHandler<float, float, float, float>("shot_to", EnqueueShotToSpec);
        runner.AddCommandHandler("shot_reset", (Action<float>)EnqueueShotResetSpec);
        
        runner.AddCommandHandler<float, float>("shot_zoom", EnqueueShotZoomSpec);
        runner.AddCommandHandler<float, float, float>("shot_track", EnqueueShotTrackSpec);
    }
    
    private void BindTransition(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, float>("tx_slant_in", EnqueueSlantedMaskCutInSpec);
        runner.AddCommandHandler<string, float>("tx_slant_out", EnqueueSlantedMaskCutOutSpec);

        runner.AddCommandHandler<string, float>("tx_strip_cover", EnqueueVerticalStripCoverSpec);
        runner.AddCommandHandler<string, float>("tx_strip_clear", EnqueueVerticalStripClearSpec);

        runner.AddCommandHandler<string, float>("tx_shutter_open", EnqueueSlantedShutterOpenSpec);
        runner.AddCommandHandler<string, float>("tx_shutter_close", EnqueueSlantedShutterCloseSpec);

        runner.AddCommandHandler<string, float>("tx_focus_fade_out", EnqueueFocusBlurFadeOutSpec);
        runner.AddCommandHandler<string, float>("tx_focus_fade_in", EnqueueFocusBlurFadeInSpec);

        runner.AddCommandHandler<string, float>("tx_focus_curtain_close", EnqueueFocusBlurCurtainCloseSpec);
        runner.AddCommandHandler<string, float>("tx_focus_curtain_open", EnqueueFocusBlurCurtainOpenSpec);

        runner.AddCommandHandler<string, float>("tx_daze_fade_close", EnqueueDazeFadeCloseSpec);
        runner.AddCommandHandler<string, float>("tx_daze_fade_open", EnqueueDazeFadeOpenSpec);
    }
    
    private void BindAudio(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, float>("bgm", EnqueuePlayBgmSpec);
        runner.AddCommandHandler<float>("stop_bgm", EnqueueStopBgmSpec);

        runner.AddCommandHandler<string>("sfx", EnqueuePlaySfxSpec);
        runner.AddCommandHandler("stop_all_sfx", EnqueueStopAllSfxSpec);
        
        runner.AddCommandHandler<string>("voice", EnqueuePlayVoiceSpec);
        runner.AddCommandHandler("stop_voice", EnqueueStopVoiceSpec);
    }

    private void Collect(CommandSpecBase spec)
    {
        _playbackDriver.Enqueue(spec);
    }
}