using System;
using System.Collections;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private readonly DialogueRunner _runner;

    private readonly YarnBridgePlaybackDriver _playbackDriver;
    private readonly VNRuntimeStateProvider _vnRuntimeStateProvider;
    private readonly RectTransform _charRigPrefab;

    private readonly VNSideRunnerSyncHub _sideRunnerSyncHub;
    private readonly OneShotPresentationLane _oneShotPresentationLane;
    
    public YarnCommandBridge(
        DialogueRunner runner,
        YarnBridgePlaybackDriver playbackDriver,
        VNRuntimeStateProvider vnRuntimeStateProvider,
        VNSideRunnerSyncHub sideRunnerSyncHub,
        RectTransform charRigPrefab,
        OneShotPresentationLane oneShotPresentationLane,
        bool bindMainLaneCommands)
    {
        _runner = runner;
        _playbackDriver = playbackDriver;
        _vnRuntimeStateProvider = vnRuntimeStateProvider;
        _sideRunnerSyncHub = sideRunnerSyncHub;
        _charRigPrefab = charRigPrefab;

        BindRunnerCommands(_runner);

        if (bindMainLaneCommands)
            BindMainLaneCommands(_runner);
        
        _oneShotPresentationLane = oneShotPresentationLane;
    }
    
    private void BindMainLaneCommands(DialogueRunner runner)
    {
        // Main Runner only commands.
        runner.AddCommandHandler<string>("pres_start", StartSubPresentationNode);
        runner.AddCommandHandler("pres_end", StopSubPresentationNode);
        
        runner.AddCommandHandler("pres_pause",  PauseSubPresentation);  // 일시정지
        runner.AddCommandHandler("pres_resume", ResumeSubPresentation); // 재개

        // 자동 진행 제어 (재호출 시 마지막 값으로 덮어씀)
        runner.AddCommandHandler<int>("pres_hold", HoldSubPresentation);                        // N라인 멈춤
        runner.AddCommandHandler<int>("pres_advance", AdvanceSubPresentationExtra);             // 이번 라인 N개 추가
        runner.AddCommandHandler<bool>("pres_suppress_first", SetSubPresentationSuppressFirst); // 시작 첫 라인 suppress on/off
        
        runner.AddCommandHandler<string>("beat", RunOneShotNode); // One-Shot Node 재생. 커맨드로만 이루어졌기에 즉시 재생 및 자동 종료
    }

    // Lane registration is explicitly handled by bootstrap:
    // hub.RegisterPresentationLane(subRunner).
    private IEnumerator StartSubPresentationNode(string nodeName) => _sideRunnerSyncHub.StartPresentationLaneCoroutine(nodeName);
    private IEnumerator StopSubPresentationNode() => _sideRunnerSyncHub.StopPresentationLaneCoroutine();
    private void PauseSubPresentation()  => _sideRunnerSyncHub.PausePresentation();
    private void ResumeSubPresentation() => _sideRunnerSyncHub.ResumePresentation();
    private void HoldSubPresentation(int lines = 1) => _sideRunnerSyncHub.HoldPresentation(lines);
    private void AdvanceSubPresentationExtra(int steps = 1) => _sideRunnerSyncHub.StepPresentationOnce(steps);
    private void SetSubPresentationSuppressFirst(bool suppress) => _sideRunnerSyncHub.SetPresentationSuppressFirstAutoAdvance(suppress);
    
    private IEnumerator RunOneShotNode(string nodeName) => _oneShotPresentationLane.RunNodeCoroutine(nodeName);
    
    private void BindRunnerCommands(DialogueRunner runner)
    {
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
        // Yarn timing is held until <<play_block>> plays the block.
        runner.AddCommandHandler("block_hold", BeginBlockCapture);
        
        // Plays the currently collected command block at this point in Yarn flow.
        // Yarn waits until playback finishes
        runner.AddCommandHandler<float>("block_end", PlayCapturedBlock);
        
        runner.AddCommandHandler<float>("pause", EnqueueWaitSpec);
        
        runner.AddCommandHandler<string>("ui_patch", EnqueueUIPatchSpec);
        
        runner.AddCommandHandler<float>("box_hide", EnqueueHideDialogueBoxSpec);
        
        runner.AddCommandHandler<string>("debug_log", LogImmediate);
        runner.AddCommandHandler<string>("debug_state", LogYarnState);
    }
    

    private void BindCharRigSetup(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>("slot", EnqueueSetupCharRigSpec);
        
        runner.AddCommandHandler<string, string, string, string, string>("cast", EnqueueCastCharacterSpec);
        
        runner.AddCommandHandler<string, string, string, string>("pose", EnqueueSetPortraitSpriteSpec);
        runner.AddCommandHandler<string, string, bool, bool>("place", EnqueueSetAnchorSpecs);
        runner.AddCommandHandler<string, string>("size", EnqueueSetOriginSizeSpec);
        
        runner.AddCommandHandler<string, int, int>("place_offset", EnqueueSetAnchorOffsetSpecs);
    }

    private void BindCharRigBasic(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, float>("fade_in", EnqueueFadeInSpec);
        runner.AddCommandHandler<string, float>("fade_out", EnqueueFadeOutSpec);
        
        runner.AddCommandHandler<string, string>("face", EnqueueSetEmotionPortraitWipeSpec);
        runner.AddCommandHandler<string, string>("face_crossfade", EnqueueSetPortraitCrossfadeSpec);
        
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
        runner.AddCommandHandler<string, string>("emoji", EnqueueSetCharacterEmojiSpec);
        runner.AddCommandHandler<string, string, string>("emoji_slot", EnqueueSetCharacterEmojiSlotSpec);
        
        runner.AddCommandHandler<string>("emoji_hide", EnqueueHideCharacterEmojiSpec);
        runner.AddCommandHandler<string, string>("emoji_hide_slot", EnqueueHideCharacterEmojiSlotSpec);
    }
    
    private void BindBackgroundRig(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string, string, float, float, float, string>("bg_spawn", EnqueueSpawnBackgroundRigSpec);
        
        runner.AddCommandHandler<string, float, float, float>("bg_place", EnqueueSetBackgroundAnchorSpec);
        runner.AddCommandHandler<string, string, string>("bg_sprite", EnqueueSetBackgroundSpriteSpec);
        runner.AddCommandHandler<string, string>("bg_size", EnqueueSetBackgroundOriginSizeSpec);
        
        runner.AddCommandHandler<string, float, string>("bg_fade_in", EnqueueFadeInBackgroundSpec);
        runner.AddCommandHandler<string, float, string>("bg_fade_out", EnqueueFadeOutBackgroundSpec);
        
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

        runner.AddCommandHandler<string, string>("shot_unbind_bg_response", EnqueueRemoveBackgroundResponseBindingSpec);
        runner.AddCommandHandler<string, string>("shot_unbind_char_response", EnqueueRemoveCharacterResponseBindingSpec);

        runner.AddCommandHandler<string, string, string, float, float>("shot_zoom_focus", EnqueueShotZoomFocusSpec);
        runner.AddCommandHandler<float, float, float, float>("shot_to", EnqueueShotToSpec);
        runner.AddCommandHandler("shot_reset", (Action<float>)EnqueueShotResetSpec);

        runner.AddCommandHandler<float, float>("shot_zoom", EnqueueShotZoomSpec);
        runner.AddCommandHandler<float, float, float>("shot_track", EnqueueShotTrackSpec);
    }
    
    private void BindTransition(DialogueRunner runner)
    {
        runner.AddCommandHandler<float>("tx_slant_in", EnqueueSlantedMaskCutInSpec);
        runner.AddCommandHandler<float>("tx_slant_out", EnqueueSlantedMaskCutOutSpec);

        runner.AddCommandHandler<float>("tx_strip_in", EnqueueVerticalStripCoverSpec);
        runner.AddCommandHandler<float>("tx_strip_out", EnqueueVerticalStripClearSpec);

        runner.AddCommandHandler<float>("tx_shutter_in", EnqueueSlantedShutterCloseSpec);
        runner.AddCommandHandler<float>("tx_shutter_out", EnqueueSlantedShutterOpenSpec);

        runner.AddCommandHandler<float>("tx_focus_fade_in", EnqueueFocusBlurFadeOutSpec);
        runner.AddCommandHandler<float>("tx_focus_fade_out", EnqueueFocusBlurFadeInSpec);

        runner.AddCommandHandler<float>("tx_focus_curtain_in", EnqueueFocusBlurCurtainCloseSpec);
        runner.AddCommandHandler<float>("tx_focus_curtain_out", EnqueueFocusBlurCurtainOpenSpec);

        runner.AddCommandHandler<float>("tx_daze_fade_in", EnqueueDazeFadeCloseSpec);
        runner.AddCommandHandler<float>("tx_daze_fade_out", EnqueueDazeFadeOpenSpec);
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