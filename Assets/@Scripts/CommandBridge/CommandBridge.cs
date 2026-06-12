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
    private readonly RectTransform _backgroundRigPrefab;

    private readonly VNSideRunnerSyncHub _sideRunnerSyncHub;
    private readonly OneShotPresentationLane _oneShotPresentationLane;
    private readonly DialogueBoxPresentationController _dialogueBoxPresentation;
    
    public YarnCommandBridge(
        DialogueRunner runner,
        YarnBridgePlaybackDriver playbackDriver,
        VNRuntimeStateProvider vnRuntimeStateProvider,
        VNSideRunnerSyncHub sideRunnerSyncHub,
        RectTransform charRigPrefab,
        RectTransform backgroundRigPrefab,
        OneShotPresentationLane oneShotPresentationLane,
        DialogueBoxPresentationController dialogueBoxPresentation,
        bool bindMainLaneCommands)
    {
        _runner = runner;
        _playbackDriver = playbackDriver;
        _vnRuntimeStateProvider = vnRuntimeStateProvider;
        _sideRunnerSyncHub = sideRunnerSyncHub;
        _charRigPrefab = charRigPrefab;
        _backgroundRigPrefab = backgroundRigPrefab;
        _oneShotPresentationLane = oneShotPresentationLane;
        _dialogueBoxPresentation = dialogueBoxPresentation;
        
        BindRunnerCommands(_runner);

        if (bindMainLaneCommands)
            BindMainLaneCommands(_runner);
        
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
        
        
        // Portrait = 0,
        // Speaker = 1,
        // LetterBox = 2,
        // OnlyText = 3,
        // BlackBook= 4
        runner.AddCommandHandler<string>("box_named", SetNamedLineBoxKind);
        runner.AddCommandHandler<string>("box_protagonist", SetProtagonistLineBoxKind);
        runner.AddCommandHandler("box_reset", ResetDefaultLineBoxKinds);
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
        BindCharRigAppearance(runner);
        BindCharRigStaging(runner);
        BindCharRigActing(runner);
        BindCharRigIdle(runner);
        BindCharRigPreset(runner);
        BindCharRigComposition(runner);

        BindCharRigEmote(runner);

        BindBackgroundRig(runner);
        BindShotStaging(runner);

        BindTransition(runner);
        BindAudio(runner);
        
        BindScreenEffects(runner);
    }
    
    private void BindControl(DialogueRunner runner)
    {
        runner.AddCommandHandler<float>("pause", EnqueueWaitSpec);
        runner.AddCommandHandler<string>("ui_patch", EnqueueUIPatchSpec);
        runner.AddCommandHandler<string>("debug_log", LogImmediate);
        runner.AddCommandHandler<string, string, string>("attach_to_bg", EnqueueAttachCharRigToBackgroundObjectSlotSpec);
        runner.AddCommandHandler<string, string>("pres_actor", SetPresentationActor);
    }

    private void BindCharRigSetup(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>(
            "slot",
            EnqueueSetupCharRigSpec);
        
        runner.AddCommandHandler<string, string, string, string, string, string>(
            "cast",
            EnqueueCastCharacterSpec);
        runner.AddCommandHandler<string, string>(
            "pose", EnqueueSetPortraitPoseSpec);
        runner.AddCommandHandler<string, string>(
            "face", 
            EnqueueSetPortraitFaceSpec);
        runner.AddCommandHandler<string, string>(
            "size", 
            EnqueueSetOriginSizeCommandSpec);
    }
    private void BindCharRigAppearance(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, float>(
            "fade_in", EnqueueFadeInSpec);
        runner.AddCommandHandler<string, float>(
            "fade_out", EnqueueFadeOutSpec);
        
        runner.AddCommandHandler<string, string, float>(
            "face_swap", EnqueueSetEmotionPortraitWipeSpec);
        runner.AddCommandHandler<string, string, float>(
            "face_crossfade", EnqueueSetPortraitCrossfadeSpec);
        
        runner.AddCommandHandler<string, string, float>(
            "slide_in", EnqueueSlideInSpec);
        runner.AddCommandHandler<string, string, float>(
            "slide_out", EnqueueSlideOutSpec);
        
        runner.AddCommandHandler<string, string>(
            "emoji", EnqueueEmojiPopSpec);
    }
    
    private void BindCharRigStaging(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, bool, bool>
            ("place", EnqueueSetAnchorSpecs);
        runner.AddCommandHandler<string, string, float>     
            ("place_to", EnqueuePlaceToSpec);
        
        runner.AddCommandHandler<string, string, string, float>(
            "focus_to", EnqueuePlaceCharacterFocusSpec);
        
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
        
        runner.AddCommandHandler<string>(
            "shot_bind_bg", EnqueueRegisterBackgroundResponseBindingSpec);
        runner.AddCommandHandler<string>(
            "shot_bind_char_far", EnqueueRegisterCharacterResponseBindingSpec0);
        runner.AddCommandHandler<string>(
            "shot_bind_char_close", EnqueueRegisterCharacterResponseBindingSpec1);
        
        runner.AddCommandHandler<string, string>(
            "shot_unbind_bg", EnqueueRemoveBackgroundResponseBindingSpec);
        runner.AddCommandHandler<string, string>(
            "shot_unbind_char", EnqueueRemoveCharacterResponseBindingSpec);
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
        
        runner.AddCommandHandler<string, float, float, float>(
            "char_move_to", EnqueueMoveByCharSpec);
        runner.AddCommandHandler<string, float, float>(
            "char_scale_to", EnqueueScaleToSpec);
        runner.AddCommandHandler<string, int, float>(
            "char_rotate_to", EnqueuePivotRotateToSpec);
        runner.AddCommandHandler<string, int, float>(
            "char_flip_horizontal", EnqueueFlipHorizontalSpec);
        runner.AddCommandHandler<string, int, float>(
            "char_flip_vertical", EnqueueFlipVerticalSpec);
    }
    
    private void BindCharRigPreset(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>(
            "jolt", EnqueueJoltSpec);
        runner.AddCommandHandler<string, string>(
            "nudge", EnqueueJoltSpecTap);
        runner.AddCommandHandler<string, string>(
            "nudge_hard", EnqueueJoltSpecTapHard);
        
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
        runner.AddCommandHandler<string, float, float>(
            "char_focus", EnqueueCharFocusSpec);
        runner.AddCommandHandler<string, float, float, float>(
            "char_defocus", EnqueueCharDefocusSpec);
        runner.AddCommandHandler<string, float>(
            "char_clear_focus", EnqueueCharClearFocusSpec);
        
        runner.AddCommandHandler<string, float, float>(
            "char_dim", EnqueueCharDimSpec);
        runner.AddCommandHandler<string, float, float>(
            "char_silhouette", EnqueueCharSilhouetteSpec);
        runner.AddCommandHandler<string, float, float>(
            "char_inner_rim", EnqueueCharInnerRimSpec);
        runner.AddCommandHandler<string, float, float>(
            "char_outer_rim", EnqueueCharOuterRimSpec);
        
        runner.AddCommandHandler<string, float, float, float, float>(
            "char_visual", EnqueueCharVisualSpec);
        runner.AddCommandHandler<string, float, float, float, float, float, float, float>(
            "char_visual_color", EnqueueCharVisualRimColorSpec);
        
        runner.AddCommandHandler<string, float, float, float, float>(
            "char_color_to", EnqueueSpriteColorToSpec);
    }
    
    private void BindCharRigEmote(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>(
            "emoji_drop", EnqueueEmojiDropSpec);
        runner.AddCommandHandler<string, string>(
            "emoji_shock", EnqueueEmojiShockSpec);
        runner.AddCommandHandler<string, string>(
            "emoji_hop", EnqueueEmojiHopSpec);
        runner.AddCommandHandler<string, string>(
            "emoji_sway", EnqueueEmojiSwaySpec);
        runner.AddCommandHandler<string, string>(
            "emoji_tremble", EnqueueEmojiTrembleSpec);
        
        runner.AddCommandHandler<string, string>(
            "emoji_set", EnqueueEmojiSetSpec);
        runner.AddCommandHandler<string>(
            "emoji_hide", EnqueueEmojiHideSpec);
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
    
    private void BindBackgroundRig(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string>(
            "bg_spawn", EnqueueSpawnBackgroundRigSpec);
        
        runner.AddCommandHandler<string, float, float, float>(
            "bg_place", EnqueueSetBackgroundAnchorSpec);
        runner.AddCommandHandler<string, string, string>(
            "bg_sprite", EnqueueSetBackgroundSpriteSpec);
        runner.AddCommandHandler<string, string>(
            "bg_size", EnqueueSetBackgroundOriginSizeSpec);
        
        runner.AddCommandHandler<string, float>(
            "bg_fade_in", EnqueueFadeInBackgroundSpec);
        runner.AddCommandHandler<string, float>(
            "bg_fade_out", EnqueueFadeOutBackgroundSpec);
        
        runner.AddCommandHandler<string, string>(
            "bg_hide_layers", EnqueueHideBackgroundRootLayersSpec);
        runner.AddCommandHandler<string, string>(
            "bg_show_layers", EnqueueShowBackgroundRootLayersSpec);
        
        runner.AddCommandHandler<string, float, float, float>(
            "bg_move", EnqueueMoveBackgroundSpec);
        runner.AddCommandHandler<string, float, float>(
            "bg_scale", EnqueueScaleBackgroundSpec);
        
        runner.AddCommandHandler<string, string, float, float>(
            "bg_slide_in", EnqueueSlideInBackgroundSpec);
        runner.AddCommandHandler<string, string, float, float>(
            "bg_slide_out", EnqueueSlideOutBackgroundSpec);
        runner.AddCommandHandler<string, string, float, float>(
            "bg_jolt", EnqueueJoltBackgroundSpec);
        
        runner.AddCommandHandler<string, string, float, float>(
            "bg_idle_tremble", EnqueueTrembleBackgroundSpec);
        runner.AddCommandHandler<string, float, float, float>(
            "bg_idle_breath", EnqueueBreathBackgroundSpec);
        
        runner.AddCommandHandler<string, float, float>(
            "bg_defocus", EnqueueBackgroundDefocusSpec);
        runner.AddCommandHandler<string, float, float, int, string, float>(
            "bg_defocus_custom", EnqueueBackgroundDefocusCustomSpec);
        runner.AddCommandHandler<string, float>(
            "bg_defocus_clear", EnqueueBackgroundDefocusClearSpec);
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

    private void Collect(CommandSpecBase spec)
    {
        _playbackDriver.Enqueue(spec);
    }
}