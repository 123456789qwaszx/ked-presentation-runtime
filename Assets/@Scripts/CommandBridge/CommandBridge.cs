using System.Collections;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private readonly YarnBridgePlaybackDriver _playbackDriver;
    private readonly RectTransform _charRigPrefab;
    private readonly RectTransform _backgroundRigPrefab;

    private readonly VNSideRunnerSyncHub _sideRunnerSyncHub;
    private readonly OneShotPresentationLane _oneShotPresentationLane;
    private readonly DialogueBoxPresentationController _dialogueBoxPresentation;
    
    public YarnCommandBridge(
        DialogueRunner runner,
        YarnBridgePlaybackDriver playbackDriver,
        VNSideRunnerSyncHub sideRunnerSyncHub,
        RectTransform charRigPrefab,
        RectTransform backgroundRigPrefab,
        OneShotPresentationLane oneShotPresentationLane,
        DialogueBoxPresentationController dialogueBoxPresentation,
        bool bindMainLaneCommands)
    {
        _playbackDriver = playbackDriver;
        _sideRunnerSyncHub = sideRunnerSyncHub;
        _charRigPrefab = charRigPrefab;
        _backgroundRigPrefab = backgroundRigPrefab;
        _oneShotPresentationLane = oneShotPresentationLane;
        _dialogueBoxPresentation = dialogueBoxPresentation;
        
        BindRunnerCommands(runner);

        if (bindMainLaneCommands)
            BindMainLaneCommands(runner);
    }
    
    // Lane registration is explicitly handled by bootstrap:
    // hub.RegisterPresentationLane(subRunner).
    private void StartSubPresentationNode(string nodeName) 
        => _sideRunnerSyncHub.StartPresentationLaneCoroutine(nodeName);
    
    private IEnumerator StopSubPresentationNode() 
        => _sideRunnerSyncHub.StopPresentationLaneCoroutine();
    
    private void PauseSubPresentation() 
        => _sideRunnerSyncHub.PausePresentation();
    
    private void ResumeSubPresentation() 
        => _sideRunnerSyncHub.ResumePresentation();
    
    private void HoldSubPresentation(int lines = 1) 
        => _sideRunnerSyncHub.HoldPresentation(lines);
    
    private void AddSubPresentationForwardAdvance(int steps = 1) 
        => _sideRunnerSyncHub.AdvancePresentationExtra(steps);
    
    private IEnumerator RunOneShotNode(string nodeName)
    {
        return _oneShotPresentationLane.RunNodeCoroutine(nodeName, blockMain: true);
    }

    private IEnumerator RunOneShotNodeFree(string nodeName)
    {
        return _oneShotPresentationLane.RunNodeCoroutine(nodeName, blockMain: false);
    }
    
    private void BindRunnerCommands(DialogueRunner runner)
    {
        RegisterDirectionalNudgeCommands(runner);
        RegisterFocusPlacementCommands(runner);
        RegisterDepthFocusCommands(runner);
        RegisterShowCommands(runner);
        BindBackgroundRig(runner);
        BindCharRigEmoji(runner);
        
        BindControl(runner);

        BindCharRigSetup(runner);
        BindCharRigAppearance(runner);
        BindCharRigStaging(runner);
        BindCharRigActing(runner);
        BindCharRigIdle(runner);
        BindCharRigPreset(runner);
        BindCharRigComposition(runner);

        BindCharRigEmote(runner);

        BindShotStaging(runner);

        BindTransition(runner);
        BindAudio(runner);
        
        BindScreenEffects(runner);
        BindStageDepthDefocus(runner);
    }
    
    private void BindControl(DialogueRunner runner)
    {
        BindFramePauseAliases(runner);
        
        runner.AddCommandHandler<float>(
            "pause", EnqueueWaitSpec);
        
        runner.AddCommandHandler<string>(
            "ui_patch", EnqueueUIPatchSpec);
        
        runner.AddCommandHandler<string>(
            "debug_log", LogImmediate);
        
        runner.AddCommandHandler<string, string, string>(
            "attach_to_bg", EnqueueAttachCharRigToBackgroundObjectSlotSpec);
        
        runner.AddCommandHandler<string, string>(
            "actor", SetPresentationActor);
        
        runner.AddCommandHandler(
            "box_hide", HideDialogueBox);
        runner.AddCommandHandler(
            "box_show", ShowDialogueBox);
        
        runner.AddCommandHandler(
            "box_close", CloseDialogueBox);
    }
    
    private void BindMainLaneCommands(DialogueRunner runner)
    {
        // Main Runner only commands.
        runner.AddCommandHandler<string>(
            "pres_start", StartSubPresentationNode);
        runner.AddCommandHandler(
            "pres_end", StopSubPresentationNode);
        
        runner.AddCommandHandler(
            "pres_pause",  PauseSubPresentation);  // 일시정지
        runner.AddCommandHandler(
            "pres_resume", ResumeSubPresentation); // 재개

        // 자동 진행 제어 (재호출 시 마지막 값으로 덮어씀)
        runner.AddCommandHandler<int>(
            "pres_hold", HoldSubPresentation); // N라인 멈춤
        runner.AddCommandHandler<int>(
            "pres_advance", AddSubPresentationForwardAdvance); // 이번 라인 N개 추가
        
        runner.AddCommandHandler<string>(
            "beat", RunOneShotNode); // One-Shot Node 재생. 커맨드로만 이루어졌기에 즉시 재생 및 자동 종료
        
        runner.AddCommandHandler<string>(
            "beat_fx",
            RunOneShotNodeFree); // non-blocking decorative effect beat
        
        // Portrait = 0,
        // Speaker = 1,
        // LetterBox = 2,
        // OnlyText = 3,
        // BlackBook= 4
        runner.AddCommandHandler<string>(
            "box_named", SetNamedLineBoxKind);
        runner.AddCommandHandler<string>(
            "box_protagonist", SetProtagonistLineBoxKind);
        runner.AddCommandHandler(
            "box_reset", ResetDefaultLineBoxKinds);
    }

    private void BindCharRigSetup(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string>(
            "slot", EnqueueSetupCharRigSpec);
        
        runner.AddCommandHandler<string, string>(
            "slot00", EnqueueSetupCharRigStage00Spec);
        runner.AddCommandHandler<string, string>(
            "slot01", EnqueueSetupCharRigStage01Spec);
        runner.AddCommandHandler<string, string>(
            "slot02", EnqueueSetupCharRigStage02Spec);
        
        runner.AddCommandHandler<string, string, string, string, string, string>(
            "cast", EnqueueCastCharacterSpec);
        runner.AddCommandHandler<string, string>(
            "pose", EnqueueSetPortraitPoseSpec);
        runner.AddCommandHandler<string, string>(
            "face", EnqueueSetPortraitFaceSpec);
        runner.AddCommandHandler<string, string>(
            "size", EnqueueSetOriginSizeCommandSpec);

        runner.AddCommandHandler<string, string>(
            "mirror", EnqueueMirrorSetSpec);
    }
    
    private void BindCharRigStaging(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, int, int, float>(
            "move_by", EnqueueSetAnchorOffsetSpecs);
        runner.AddCommandHandler<string, float, float>(
            "rotate_by", EnqueueRotateBySpec);
        runner.AddCommandHandler<string, float, float>(
            "scale_by", EnqueueSizeBySpec);

        runner.AddCommandHandler<string, float>(
            "move_reset", EnqueueSetPlaceResetSpecs);
        runner.AddCommandHandler<string, float>(
            "rotate_reset", EnqueueRotateResetSpec);
        runner.AddCommandHandler<string, float>(
            "scale_reset", EnqueueSizeResetSpec);
        
        runner.AddCommandHandler<string>(
            "sibling_front", EnqueueCharacterSiblingFrontSpec);

        runner.AddCommandHandler<string>(
            "sibling_back", EnqueueCharacterSiblingBackSpec);
        
        
        runner.AddCommandHandler<string, string, string>(
            "char_to", EnqueueMoveCharacterRigToStageLayerSpec);

        runner.AddCommandHandler<string, string>(
            "char_to_s0", EnqueueMoveCharacterRigToStage00LayerSpec);

        runner.AddCommandHandler<string, string>(
            "char_to_s1", EnqueueMoveCharacterRigToStage01LayerSpec);

        runner.AddCommandHandler<string, string>(
            "char_to_s2", EnqueueMoveCharacterRigToStage02LayerSpec);
    }
    
    private void BindShotStaging(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string, float, float>(
            "shot_focus_to", EnqueueShotZoomFocusSpec);
        runner.AddCommandHandler<float, float, float, float>(
            "shot_to", EnqueueShotToSpec);
        runner.AddCommandHandler<float, float>(
            "shot_zoom", EnqueueShotZoomSpec);
        runner.AddCommandHandler<float, float, float>(
            "shot_track", EnqueueShotTrackSpec);
        runner.AddCommandHandler<float>(
            "shot_reset", EnqueueShotResetSpec);
    }
    
    private void BindCharRigActing(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>(
            "dip", EnqueueDipInOutSpec);
        runner.AddCommandHandler<string, int, float, float, float>(
            "hop", EnqueueHopSpec);
        runner.AddCommandHandler<string, string, float, float, int>(
            "shake", EnqueueJoltSpecShake);
        runner.AddCommandHandler<string, float, float, float, string>(
            "tremble", EnqueueTrembleSpec);
        runner.AddCommandHandler<string>(
            "sway", EnqueueSwaySpec);
    }
    
    private void BindCharRigPreset(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>(
            "jolt", EnqueueJoltSpec);
        runner.AddCommandHandler<string, string>(
            "tap", EnqueueJoltSpecTap);
        runner.AddCommandHandler<string, string>(
            "tap_hard", EnqueueJoltSpecTapHard);
        
        runner.AddCommandHandler<string>(
            "slide_in_sway", EnqueueSlideInSwayCombo);
        runner.AddCommandHandler<string, string>(
            "slide_in_nudge", EnqueueSlideInJoltCombo);
        
        runner.AddCommandHandler<string>(
            "sway_hard", EnqueueSwaySpecPendulum);
        runner.AddCommandHandler<string>(
            "sway_fast", EnqueueSwaySpecFast);
        runner.AddCommandHandler<string>(
            "sway_away", EnqueueSwaySpecAway);
    }
    
    private void BindCharRigComposition(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, float, string>(
            "char_focus", EnqueueCharFocusSpec);

        runner.AddCommandHandler<string, float, string>(
            "char_defocus", EnqueueCharDefocusSpec);

        runner.AddCommandHandler<string, string>(
            "char_clear_focus", EnqueueCharClearFocusSpec);

        runner.AddCommandHandler<string, float, string>(
            "char_dim", EnqueueCharDimSpec);

        runner.AddCommandHandler<string, float, string>(
            "char_silhouette", EnqueueCharSilhouetteSpec);

        runner.AddCommandHandler<string, float, string>(
            "char_inner_rim", EnqueueCharInnerRimSpec);

        runner.AddCommandHandler<string, float, string>(
            "char_outer_rim", EnqueueCharOuterRimSpec);

        runner.AddCommandHandler<string, float, float, float, string>(
            "char_visual", EnqueueCharVisualSpec);

        runner.AddCommandHandler<string, float, float, float, float, float, float, string>(
            "char_visual_color", EnqueueCharVisualRimColorSpec);

        runner.AddCommandHandler<string, float, float, float, string>(
            "char_color_to", EnqueueSpriteColorToDslSpec);
    }
    
    private void BindCharRigIdle(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, float, float, float, float>(
            "idle_bounce", EnqueueBounceInPlaceSpec);
        runner.AddCommandHandler<string, float, float, float>(
            "idle_breathe", EnqueueBreathInPlaceSpec);
        runner.AddCommandHandler<string, float, float, float, float, float, string>(
            "idle_flinch", EnqueueTremblePulseSpec);
        runner.AddCommandHandler<string, float, float, float, float>(
            "idle_walk", EnqueueWalkInPlaceSpec);
    }
    
    private void BindTransition(DialogueRunner runner)
    {
        runner.AddCommandHandler<float>(
            "tx_slant_in", EnqueueSlantedMaskCutInSpec);
        runner.AddCommandHandler<float>(
            "tx_slant_out", EnqueueSlantedMaskCutOutSpec);
        runner.AddCommandHandler<float>(
            "tx_out_slant", EnqueueTransitionOutSlantSpec);

        runner.AddCommandHandler<float>(
            "tx_strip_in", EnqueueVerticalStripCoverSpec);
        runner.AddCommandHandler<float>(
            "tx_strip_out", EnqueueVerticalStripClearSpec);

        runner.AddCommandHandler<float>(
            "tx_shutter_in", EnqueueSlantedShutterCloseSpec);
        runner.AddCommandHandler<float>(
            "tx_shutter_out", EnqueueSlantedShutterOpenSpec);

        runner.AddCommandHandler<float>(
            "tx_focus_fade_in", EnqueueFocusBlurFadeOutSpec);
        runner.AddCommandHandler<float>(
            "tx_focus_fade_out", EnqueueFocusBlurFadeInSpec);

        runner.AddCommandHandler<float>(
            "tx_focus_curtain_in", EnqueueFocusBlurCurtainCloseSpec);
        runner.AddCommandHandler<float>(
            "tx_focus_curtain_out", EnqueueFocusBlurCurtainOpenSpec);

        runner.AddCommandHandler<float>(
            "tx_daze_fade_in", EnqueueDazeFadeCloseSpec);
        runner.AddCommandHandler<float>(
            "tx_daze_fade_out", EnqueueDazeFadeOpenSpec);
        
        runner.AddCommandHandler(
            "tx_clear_all", EnqueueClearAllTransitionsSpec);
        runner.AddCommandHandler<string, float>(
            "tx_reveal", EnqueueRevealWithTransitionSpec);
        
        runner.AddCommandHandler<float>(
            "tx_out_shutter", EnqueueTransitionOutShutterSpec);
        runner.AddCommandHandler<float>(
            "tx_out_strip", EnqueueTransitionOutStripSpec);
        runner.AddCommandHandler<float>(
            "tx_out_focus_fade", EnqueueTransitionOutFocusFadeSpec);
        runner.AddCommandHandler<float>(
            "tx_out_focus_curtain", EnqueueTransitionOutFocusCurtainSpec);
        runner.AddCommandHandler<float>(
            "tx_out_daze", EnqueueTransitionOutDazeFadeSpec);
    }
    
    private void BindAudio(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, float>(
            "bgm", EnqueuePlayBgmSpec);
        runner.AddCommandHandler<float>(
            "stop_bgm", EnqueueStopBgmSpec);

        runner.AddCommandHandler<string>(
            "sfx", EnqueuePlaySfxSpec);
        runner.AddCommandHandler(
            "stop_all_sfx", EnqueueStopAllSfxSpec);
        
        runner.AddCommandHandler<string>(
            "voice", EnqueuePlayVoiceSpec);
        runner.AddCommandHandler(
            "stop_voice", EnqueueStopVoiceSpec);
    }
    
    private void BindScreenEffects(DialogueRunner runner)
    {
        runner.AddCommandHandler<float, float>(
            "screen_flash", EnqueueScreenFlashSpec);
        runner.AddCommandHandler<float, float, float, float, float>(
            "screen_flash_rgb", EnqueueScreenFlashRgbSpec);
        runner.AddCommandHandler(
            "screen_flash_hit", EnqueueScreenFlashHitSpec);
        runner.AddCommandHandler<string, float>(
            "screen_flash_preset", EnqueueScreenFlashPresetSpec);

        runner.AddCommandHandler<string, float, float>(
            "screen_vignette", EnqueueScreenVignettePresetSpec);
        runner.AddCommandHandler<float>(
            "screen_vignette_clear", EnqueueScreenVignetteClearSpec);
        runner.AddCommandHandler<float, float>(
            "screen_letterbox", EnqueueScreenLetterBoxSpec);
        runner.AddCommandHandler<float, float, float, float, float, float, float>(
            "screen_vignette_custom", EnqueueScreenVignetteCustomSpec);

        runner.AddCommandHandler<string, float, float>(
            "screen_noise", EnqueueScreenNoisePresetSpec);
        runner.AddCommandHandler<float>(
            "screen_noise_clear", EnqueueScreenNoiseClearSpec);
        runner.AddCommandHandler<float, float, float, float, float, float>(
            "screen_noise_custom", EnqueueScreenNoiseCustomSpec);
    }
    
    // private void BindStageDepthDefocus(DialogueRunner runner)
    // {
    //     runner.AddCommandHandler<string>(
    //         "depth_blur", EnqueueStageDepthBlurSpec);
    //     runner.AddCommandHandler<string>(
    //         "depth_blur_clear", EnqueueStageDepthBlurClearSpec);
    //
    //     runner.AddCommandHandler<string, string>(
    //         "depth_blur_layer", EnqueueStageDepthBlurLayerSpec);
    //     runner.AddCommandHandler<string, string, float, float>(
    //         "depth_blur_layer_t", EnqueueStageDepthBlurLayerTimedSpec);
    //     runner.AddCommandHandler<string, string, float, float, float>(
    //         "depth_blur_layer_a", EnqueueStageDepthBlurLayerAlphaSpec);
    //
    //     runner.AddCommandHandler<string, string, float, float, int, string, float>(
    //         "depth_defocus", EnqueueStageDepthDefocusSpec);
    //     runner.AddCommandHandler<string, string, float>(
    //         "depth_defocus_off", EnqueueStageDepthDefocusOffSpec);
    //     
    //     runner.AddCommandHandler<string, string, float, float, float, float>(
    //         "depth_blur_layer_ap",
    //         EnqueueStageDepthBlurLayerAlphaCoverageSpec);
    //
    //     runner.AddCommandHandler<string, string, float, float, int, string, float, float>(
    //         "depth_defocus_p",
    //         EnqueueStageDepthDefocusSpec);
    // }

    private void Collect(CommandSpecBase spec)
    {
        _playbackDriver.Enqueue(spec);
    }
}