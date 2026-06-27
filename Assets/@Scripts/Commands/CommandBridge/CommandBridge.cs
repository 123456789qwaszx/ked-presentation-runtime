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

        BindOverlayRig(runner);
    }
    
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
        
        runner.AddCommandHandler<string, string, string, string, string, string>(
            "cast", EnqueueCastCharacterSpec);
        runner.AddCommandHandler<string, string>(
            "pose", EnqueueSetPortraitPoseSpec);
        runner.AddCommandHandler<string, string>(
            "face", EnqueueSetPortraitFaceSpec);
        runner.AddCommandHandler<string, string>(
            "size", EnqueueSetOriginSizeCommandSpec);
        
        runner.AddCommandHandler<string, float, float, float, string>(
            "char_color_to", EnqueueSpriteColorToDslSpec);

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
        runner.AddCommandHandler<string, string, float, string>(
            "char_visual", EnqueueCharVisualPresetSpec);
        
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
        runner.AddCommandHandler<string, float>(
            "tx_slant_in", EnqueueSlantedMaskCutInSpec);
        runner.AddCommandHandler<string, float>(
            "tx_slant_out", EnqueueSlantedMaskCutOutSpec);
        
        
        runner.AddCommandHandler<string, float>(
            "tx_hstrip_open",
            EnqueueHorizontalStripOpenInSpec);
        runner.AddCommandHandler<string, float>(
            "tx_hstrip_close",
            EnqueueHorizontalStripCloseOutSpec);
        
        runner.AddCommandHandler<string, float>(
            "tx_hstrip_in",
            EnqueueHorizontalStripCutInSpec);
        runner.AddCommandHandler<string, float>(
            "tx_hstrip_out",
            EnqueueHorizontalStripCutOutSpec);

        runner.AddCommandHandler<string, float>(
            "tx_vstrip_open",
            EnqueueVerticalStripOpenInSpec);
        runner.AddCommandHandler<string, float>(
            "tx_vstrip_close",
            EnqueueVerticalStripCloseOutSpec);
        
        runner.AddCommandHandler<string, float>(
            "tx_vstrip_in",
            EnqueueVerticalStripCutInSpec);
        runner.AddCommandHandler<string, float>(
            "tx_vstrip_out",
            EnqueueVerticalStripCutOutSpec);

        runner.AddCommandHandler<string, float>(
            "tx_band_in",
            EnqueueDiagonalBandCutInSpec);
        runner.AddCommandHandler<string, float>(
            "tx_band_out",
            EnqueueDiagonalBandCutOutSpec);

        runner.AddCommandHandler<string, float>(
            "tx_iris_in",
            EnqueueCircleIrisInSpec);
        runner.AddCommandHandler<string, float>(
            "tx_iris_out",
            EnqueueCircleIrisOutSpec);
        
        runner.AddCommandHandler<string, float>(
            "tx_daze_in", EnqueueDazeFadeCloseSpec);
        runner.AddCommandHandler<string, float>(
            "tx_daze_out", EnqueueTransitionOutDazeFadeSpec);
        
        runner.AddCommandHandler<string, float>(
            "tx_strip_in", EnqueueVerticalStripCoverSpec);
        runner.AddCommandHandler<string, float>(
            "tx_strip_out", EnqueueTransitionOutStripSpec);
        
        runner.AddCommandHandler(
            "tx_stage_mask_clear",
            EnqueueStageMaskClearSpec);

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
        runner.AddCommandHandler<string, float>(
            "screen_flash", EnqueueScreenFlashPresetSpec);
        runner.AddCommandHandler(
            "screen_flash_clear", EnqueueScreenFlashClearSpec);

        runner.AddCommandHandler<string, float, float>(
            "screen_vignette", EnqueueScreenVignettePresetSpec);
        runner.AddCommandHandler<float>(
            "screen_vignette_clear", EnqueueScreenVignetteClearSpec);

        runner.AddCommandHandler<string, float, float>(
            "screen_noise", EnqueueScreenNoisePresetSpec);
        runner.AddCommandHandler<float>(
            "screen_noise_clear", EnqueueScreenNoiseClearSpec);
    }

    private void Collect(CommandSpecBase spec)
    {
        _playbackDriver.Enqueue(spec);
    }
}