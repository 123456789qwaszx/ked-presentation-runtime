using System;
using System.Collections;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private readonly YarnBridgePlaybackDriver _playbackDriver;
    private readonly RectTransform _charRigPrefab;
    private readonly RectTransform _backgroundRigPrefab;
    private readonly RectTransform _overlayRigPrefab;

    private readonly VNSideRunnerSyncHub _sideRunnerSyncHub;
    private readonly OneShotPresentationLane _oneShotPresentationLane;
    private readonly DialogueBoxPresentationController _dialogueBoxPresentation;
    private readonly OverlaySequenceRunner _overlaySequenceRunner;
    private readonly SequenceCatalogSO _overlaySequenceCatalog;
    
    public YarnCommandBridge(
        DialogueRunner runner,
        YarnBridgePlaybackDriver playbackDriver,
        VNSideRunnerSyncHub sideRunnerSyncHub,
        RectTransform charRigPrefab,
        RectTransform backgroundRigPrefab,
        RectTransform overlayRigPrefab,
        OneShotPresentationLane oneShotPresentationLane,
        DialogueBoxPresentationController dialogueBoxPresentation,
        OverlaySequenceRunner overlaySequenceRunner,
        SequenceCatalogSO overlaySequenceCatalog,
        bool bindMainLaneCommands)
    {
        _playbackDriver = playbackDriver;
        _sideRunnerSyncHub = sideRunnerSyncHub;
        _charRigPrefab = charRigPrefab;
        _backgroundRigPrefab = backgroundRigPrefab;
        _overlayRigPrefab = overlayRigPrefab;
        _oneShotPresentationLane = oneShotPresentationLane;
        _dialogueBoxPresentation = dialogueBoxPresentation;
        _overlaySequenceRunner = overlaySequenceRunner;
        _overlaySequenceCatalog = overlaySequenceCatalog;
        
        BindRunnerCommands(runner);

        if (bindMainLaneCommands)
            BindMainLaneCommands(runner);
    }
    
    private void BindRunnerCommands(DialogueRunner runner)
    {
        RegisterCharFocusPlacementCommands(runner);
        RegisterCharRigPlacementCommands(runner);
        BindBackgroundRig(runner);
        BindCharRigEmoji(runner);
        
        BindControl(runner);

        BindCharRigSetup(runner);
        BindCharRigPresentation(runner);
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

        BindOverlayRig(runner);
    }
    
    // Main Runner only commands.
    private void BindMainLaneCommands(DialogueRunner runner)
    {
        // 자동 진행 제어
        runner.AddCommandHandler<string>(
            "pres_start", StartSubPresentationNode);
        runner.AddCommandHandler(
            "pres_end", StopSubPresentationNode);
        
        runner.AddCommandHandler(
            "pres_pause",  PauseSubPresentation);  // 일시정지
        runner.AddCommandHandler(
            "pres_resume", ResumeSubPresentation); // 재개

        // (재호출 시 마지막 값으로 덮어씀)
        runner.AddCommandHandler<int>(
            "pres_hold", HoldSubPresentation); // N라인 멈춤
        
        // (재호출 시 누적)
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
    
    private void PlayOverlaySequence(string sequenceKey)
        => _overlaySequenceRunner.Play(sequenceKey, _overlaySequenceCatalog);
    
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
        => _oneShotPresentationLane.RunNodeCoroutine(nodeName, blockMain: true);

    private IEnumerator RunOneShotNodeFree(string nodeName)
        => _oneShotPresentationLane.RunNodeCoroutine(nodeName, blockMain: false);
    
    private void SetNamedLineBoxKind(string key)
    {
        Enum.TryParse(key, true, out DialogueBoxKind kind);
        _dialogueBoxPresentation.SetNamedLineBoxKind(kind);
    }

    private void SetProtagonistLineBoxKind(string key)
    {
        Enum.TryParse(key, true, out DialogueBoxKind kind);
        _dialogueBoxPresentation.SetProtagonistLineBoxKind(kind);
    }

    private void ResetDefaultLineBoxKinds()
        => _dialogueBoxPresentation.ResetDefaultLineBoxKinds();

    private void BindControl(DialogueRunner runner)
    {
        BindFramePauseAliases(runner);
        
        runner.AddCommandHandler<string>(
            "seq", PlayOverlaySequence);
        
        runner.AddCommandHandler<float>(
            "pause", EnqueueWaitSpec);
        
        runner.AddCommandHandler<string>(
            "ui_patch", EnqueueUIPatchSpec);
        
        runner.AddCommandHandler<string>(
            "debug_log", LogImmediate);
        
        runner.AddCommandHandler<string, string, string>(
            "attach_to_bg", EnqueueAttachCharRigToBackgroundObjectSlotSpec);
        
        runner.AddCommandHandler<string, string>(
            "actor", EnqueuePresentationActorAliasSpec);
        
        runner.AddCommandHandler(
            "box_hide", HideDialogueBox);
        runner.AddCommandHandler(
            "box_show", ShowDialogueBox);
        
        runner.AddCommandHandler(
            "box_close", CloseDialogueBox);
        
        runner.AddCommandHandler<string>(
            "surface_layout", SetSurfaceLayout);

        runner.AddCommandHandler(
            "surface_reset", ResetSurfaceLayout);
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

        runner.AddCommandHandler(
            "slot_tyrant", EnqueueSetupTyrantProtagonistSpec);
        
        runner.AddCommandHandler<string, string, string, string>(
            "cast", EnqueueCastCharacterSpec);
        runner.AddCommandHandler<string, string>(
            "pose", EnqueueSetPortraitPoseSpec);
        runner.AddCommandHandler<string, string>(
            "face", EnqueueSetPortraitFaceSpec);
        
        runner.AddCommandHandler<string, float, float, float, string>(
            "char_color_to", EnqueueSpriteColorToDslSpec);

        runner.AddCommandHandler<string, string>(
            "mirror", EnqueueMirrorSetSpec);
    }
    
    private void BindCharRigStaging(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string, string>(
            "move_by", EnqueueSetAnchorOffsetSpecs);
        runner.AddCommandHandler<string, float, string>(
            "rotate_by", EnqueueRotateBySpec);
        runner.AddCommandHandler<string, float, string>(
            "scale_by", EnqueueSizeBySpec);

        runner.AddCommandHandler<string, string>(
            "move_reset", EnqueueSetPlaceResetSpecs);
        runner.AddCommandHandler<string, string>(
            "rotate_reset", EnqueueRotateResetSpec);
        runner.AddCommandHandler<string, string>(
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
        runner.AddCommandHandler<string, string, string, float, string>(
            "shot_focus_to", EnqueueShotZoomFocusSpec);
        runner.AddCommandHandler<float, string, string, string>(
            "shot_to", EnqueueShotToSpec);
        
        runner.AddCommandHandler<float, string>(
            "shot_zoom", EnqueueShotZoomSpec);
        runner.AddCommandHandler<string, string, string>(
            "shot_track", EnqueueShotTrackSpec);
        
        runner.AddCommandHandler<string>(
            "shot_reset", EnqueueShotResetSpec);
    }
    
    private void RegisterCharRigPlacementCommands(DialogueRunner runner)
    {
        // Show
        runner.AddCommandHandler<string, string, string>(
            "show", EnqueueShowAtSpec);
        
        // Nudge
        runner.AddCommandHandler<string, string, string>(
            "left", EnqueueNudgeLeftSpec);

        runner.AddCommandHandler<string, string, string>(
            "right", EnqueueNudgeRightSpec);

        runner.AddCommandHandler<string, string, string>(
            "up", EnqueueNudgeUpSpec);

        runner.AddCommandHandler<string, string, string>(
            "down", EnqueueNudgeDownSpec);
        
        // MoveOneUnitPerFrame
        runner.AddCommandHandler<string, string>(
            "left_per", EnqueueMoveLeftOneUnitPerFrameSpec);

        runner.AddCommandHandler<string, string>(
            "right_per", EnqueueMoveRightOneUnitPerFrameSpec);

        runner.AddCommandHandler<string, string>(
            "up_per", EnqueueMoveUpOneUnitPerFrameSpec);

        runner.AddCommandHandler<string, string>(
            "down_per", EnqueueMoveDownOneUnitPerFrameSpec);
    }
    
    private void RegisterCharFocusPlacementCommands(DialogueRunner runner)
    {
        // Char focus placement
        runner.AddCommandHandler<string, string, string, string>(
            "place", EnqueuePlaceCharacterFocusSpec);
        
        runner.AddCommandHandler<string, string, string>(
            "place_left", EnqueueFocusToLeftSpec);

        runner.AddCommandHandler<string, string, string>(
            "place_center", EnqueueFocusToCenterSpec);

        runner.AddCommandHandler<string, string, string>(
            "place_right", EnqueueFocusToRightSpec);

        runner.AddCommandHandler<string, string, string>(
            "place_tl", EnqueueFocusToTopLeftSpec);

        runner.AddCommandHandler<string, string, string>(
            "place_top", EnqueueFocusToTopSpec);

        runner.AddCommandHandler<string, string, string>(
            "place_tr", EnqueueFocusToTopRightSpec);

        runner.AddCommandHandler<string, string, string>(
            "place_bl", EnqueueFocusToBottomLeftSpec);

        runner.AddCommandHandler<string, string, string>(
            "place_bottom", EnqueueFocusToBottomSpec);

        runner.AddCommandHandler<string, string, string>(
            "place_br", EnqueueFocusToBottomRightSpec);

        runner.AddCommandHandler<string, string, string>(
            "place_inner_tl", EnqueueFocusToInnerTopLeftSpec);

        runner.AddCommandHandler<string, string, string>(
            "place_inner_tr", EnqueueFocusToInnerTopRightSpec);

        runner.AddCommandHandler<string, string, string>(
            "place_inner_bl", EnqueueFocusToInnerBottomLeftSpec);

        runner.AddCommandHandler<string, string, string>(
            "place_inner_br", EnqueueFocusToInnerBottomRightSpec);
        
        // Char focus depth
        // Generic form:
        // <<at c1 close bust 12fr>>
        // <<at c1 front>>
        // <<at c1 7 face 8fr>>
        runner.AddCommandHandler<string, string, string, string>(
            "size", EnqueueDepthAtPresetSpec);

        runner.AddCommandHandler<string, string, string>(
            "size_far", EnqueueDepthAtFarSpec);
        
        runner.AddCommandHandler<string, string, string>(
            "size_back", EnqueueDepthAtBackSpec);
        
        runner.AddCommandHandler<string, string, string>(
            "size_mid", EnqueueDepthAtMidSpec);

        runner.AddCommandHandler<string, string, string>(
            "size_front", EnqueueDepthAtFrontSpec);
        
        runner.AddCommandHandler<string, string, string>(
            "size_close", EnqueueDepthAtCloseSpec);
        
        runner.AddCommandHandler<string, float>(
            "size_reset", EnqueueDepthResetSpec);
    }
    
    private void BindCharRigPresentation(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>(
            "fade_in", EnqueueFadeInDslSpec);

        runner.AddCommandHandler<string, string>(
            "fade_out", EnqueueFadeOutDslSpec);

        runner.AddCommandHandler<string, string, string>(
            "face_swap", EnqueueSetEmotionPortraitWipeDslSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "face_crossfade", EnqueueSetPortraitCrossfadeDslSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "slide_in", EnqueueSlideInDslSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "slide_out", EnqueueSlideOutDslSpec);
        
        runner.AddCommandHandler<string, string, string, string>(
            "char_move_to", EnqueueMoveByUnitCharSpec);
        
        runner.AddCommandHandler<string, float, string>(
            "char_scale_to", EnqueueScaleToDslSpec);
        
        runner.AddCommandHandler<string, int, string>(
            "char_rotate_to", EnqueuePivotRotateToDslSpec);
        
        runner.AddCommandHandler<string, int, string>(
            "char_flip_horizontal", EnqueueFlipHorizontalDslSpec);
        
        runner.AddCommandHandler<string, int, string>(
            "char_flip_vertical", EnqueueFlipVerticalDslSpec);
    }
    
    private void BindCharRigActing(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>(
            "dip", EnqueueDipInOutSpec);
        runner.AddCommandHandler<string>(
            "hop", EnqueueHopSpec);
        runner.AddCommandHandler<string, string>(
            "shake", EnqueueShakeJoltSpec);
        runner.AddCommandHandler<string, string>(
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
        runner.AddCommandHandler<string, string, float, string>(
            "char_visual", EnqueueCharVisualPresetSpec);
        
        runner.AddCommandHandler<string, float, string>(
            "char_visual_focus", EnqueueCharFocusSpec);

        runner.AddCommandHandler<string, float, string>(
            "char_visual_defocus", EnqueueCharDefocusSpec);


        runner.AddCommandHandler<string, float, string>(
            "char_visual_dim", EnqueueCharDimSpec);

        runner.AddCommandHandler<string, float, string>(
            "char_visual_silhouette", EnqueueCharSilhouetteSpec);

        runner.AddCommandHandler<string, float, string>(
            "char_visual_inner_rim", EnqueueCharInnerRimSpec);

        runner.AddCommandHandler<string, float, string>(
            "char_visual_outer_rim", EnqueueCharOuterRimSpec);
        
        runner.AddCommandHandler<string, string>(
            "char_visual_clear", EnqueueCharRigVisualClearSpec);
    }
    
    private void BindCharRigIdle(DialogueRunner runner)
    {
        runner.AddCommandHandler<string>(
            "idle_bounce", EnqueueBounceInPlaceSpec);
        runner.AddCommandHandler<string>(
            "idle_breathe", EnqueueBreathInPlaceSpec);
        runner.AddCommandHandler<string, string>(
            "idle_flinch", EnqueueTremblePulseSpec);
        runner.AddCommandHandler<string>(
            "idle_walk", EnqueueWalkInPlaceSpec);
    }
    
    private void BindBackgroundRig(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>(
            "bg_spawn", EnqueueSpawnBackgroundRigSpec);
        
        runner.AddCommandHandler<string, string, string>(
            "bg_slot00", EnqueueSpawnBackgroundRigStage00Spec);
        runner.AddCommandHandler<string, string, string>(
            "bg_slot01", EnqueueSpawnBackgroundRigStage01Spec);
        runner.AddCommandHandler<string, string, string>(
            "bg_slot02", EnqueueSpawnBackgroundRigStage02Spec);

        runner.AddCommandHandler<string, string, string, float>(
            "bg_place", EnqueueSetBackgroundAnchorDslSpec);

        runner.AddCommandHandler<string, string, string>(
            "bg_sprite", EnqueueSetBackgroundSpriteSpec);

        runner.AddCommandHandler<string, string>(
            "bg_size", EnqueueSetBackgroundOriginSizeSpec);

        runner.AddCommandHandler<string, string>(
            "bg_fade_in", EnqueueFadeInBackgroundDslSpec);

        runner.AddCommandHandler<string, string>(
            "bg_fade_out", EnqueueFadeOutBackgroundDslSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "bg_move", EnqueueMoveBackgroundDslSpec);

        runner.AddCommandHandler<string, float, string>(
            "bg_scale", EnqueueScaleBackgroundDslSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "bg_slide_in", EnqueueSlideInBackgroundDslSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "bg_slide_out", EnqueueSlideOutBackgroundDslSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "bg_jolt", EnqueueJoltBackgroundDslSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "bg_idle_tremble", EnqueueTrembleBackgroundDslSpec);

        runner.AddCommandHandler<string, string, string, float>(
            "bg_idle_breath", EnqueueBreathBackgroundDslSpec);
        
        runner.AddCommandHandler<string>(
            "bg_slot_cutin", EnqueueBackgroundCutInSpec);
        
        runner.AddCommandHandler<string, string, string, string>(
            "bg_cutin_in", EnqueueBackgroundCutInMotionSpec);
    }
    
    private void BindTransition(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>(
            "tx_slant_in", EnqueueSlantedMaskCutInSpec);
        runner.AddCommandHandler<string, string>(
            "tx_slant_out", EnqueueSlantedMaskCutOutSpec);
        
        runner.AddCommandHandler<string, string>(
            "tx_hstrip_open",
            EnqueueHorizontalStripOpenInSpec);
        runner.AddCommandHandler<string, string>(
            "tx_hstrip_close",
            EnqueueHorizontalStripCloseOutSpec);
        
        runner.AddCommandHandler<string, string>(
            "tx_hstrip_in",
            EnqueueHorizontalStripCutInSpec);
        runner.AddCommandHandler<string, string>(
            "tx_hstrip_out",
            EnqueueHorizontalStripCutOutSpec);

        runner.AddCommandHandler<string, string>(
            "tx_vstrip_open",
            EnqueueVerticalStripOpenInSpec);
        runner.AddCommandHandler<string, string>(
            "tx_vstrip_close",
            EnqueueVerticalStripCloseOutSpec);
        
        runner.AddCommandHandler<string, string>(
            "tx_vstrip_in",
            EnqueueVerticalStripCutInSpec);
        runner.AddCommandHandler<string, string>(
            "tx_vstrip_out",
            EnqueueVerticalStripCutOutSpec);

        runner.AddCommandHandler<string, string>(
            "tx_band_in",
            EnqueueDiagonalBandCutInSpec);
        runner.AddCommandHandler<string, string>(
            "tx_band_out",
            EnqueueDiagonalBandCutOutSpec);

        runner.AddCommandHandler<string, string>(
            "tx_iris_in",
            EnqueueCircleIrisInSpec);
        runner.AddCommandHandler<string, string>(
            "tx_iris_out",
            EnqueueCircleIrisOutSpec);
        
        runner.AddCommandHandler<string, string>(
            "tx_daze_in", EnqueueDazeFadeCloseSpec);
        runner.AddCommandHandler<string, string>(
            "tx_daze_out", EnqueueTransitionOutDazeFadeSpec);
        
        runner.AddCommandHandler<string, string>(
            "tx_strip_in", EnqueueVerticalStripCoverSpec);
        runner.AddCommandHandler<string, string>(
            "tx_strip_out", EnqueueTransitionOutStripSpec);
        
        runner.AddCommandHandler(
            "tx_stage_mask_clear",
            EnqueueStageMaskClearSpec);

    }
    
    private void BindAudio(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>(
            "bgm", EnqueuePlayBgmSpec);
        runner.AddCommandHandler<string>(
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
        runner.AddCommandHandler<string, float>(
            "screen_flash", EnqueueScreenFlashPresetSpec);
        runner.AddCommandHandler(
            "screen_flash_clear", EnqueueScreenFlashClearSpec);

        runner.AddCommandHandler<string, float, string>(
            "screen_vignette", EnqueueScreenVignettePresetSpec);
        runner.AddCommandHandler<string>(
            "screen_vignette_clear", EnqueueScreenVignetteClearSpec);

        runner.AddCommandHandler<string, float, string>(
            "screen_noise", EnqueueScreenNoisePresetSpec);
        runner.AddCommandHandler<string>(
            "screen_noise_clear", EnqueueScreenNoiseClearSpec);
    }
    
    private void BindStageDepthDefocus(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, float>(
            "screen_blur", EnqueueStage00DepthBlurSpec);

        runner.AddCommandHandler<string, float>(
            "screen_blur_s1", EnqueueStage01DepthBlurSpec);

        runner.AddCommandHandler<string, float>(
            "screen_blur_s2", EnqueueStage02DepthBlurSpec);

        runner.AddCommandHandler(
            "screen_blur_reset", EnqueueStageDepthBlurClearSpec);
    }

    private void Collect(CommandSpecBase spec)
    {
        _playbackDriver.Enqueue(spec);
    }
}