using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private readonly YarnPlaybackDriver _playbackDriver;
    private readonly RectTransform _charRigPrefab;
    private readonly RectTransform _backgroundRigPrefab;

    private readonly DialogueBoxPresentationController _dialogueBoxPresentation;
    private readonly EaseCurveLibrary _easeCurves;

    public YarnCommandBridge(
        DialogueRunner runner,
        YarnPlaybackDriver playbackDriver,
        RectTransform charRigPrefab,
        RectTransform backgroundRigPrefab,
        DialogueBoxPresentationController dialogueBoxPresentation,
        EaseCurveLibrary easeCurves = null)
    {
        _playbackDriver = playbackDriver;
        _charRigPrefab = charRigPrefab;
        _backgroundRigPrefab = backgroundRigPrefab;
        _dialogueBoxPresentation = dialogueBoxPresentation;
        _easeCurves = easeCurves ?? EaseCurveLibrary.Empty;

        BindRunnerCommands(runner);
    }

    private void BindRunnerCommands(DialogueRunner runner)
    {
        RegisterCharFocusPlacementCommands(runner);
        RegisterCharRigPlacementCommands(runner);
        BindBackgroundRig(runner);
        
        BindControl(runner);

        BindCharRigSetup(runner);
        BindCharRigPresentation(runner);
        BindCharRigStaging(runner);
        BindCharRigComposition(runner);

        BindShotStaging(runner);

        BindTransition(runner);
        BindAudio(runner);
        
        BindScreenEffects(runner);
        BindStageDepthDefocus(runner);
        BindDepthFocus(runner);
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

        runner.AddCommandHandler<string, string, string, string>(
            "cast", EnqueueCastCharacterSpec);
        runner.AddCommandHandler<string, string>(
            "pose", EnqueueSetPortraitPoseSpec);
        runner.AddCommandHandler<string, string>(
            "face", EnqueueSetPortraitFaceSpec);
        runner.AddCommandHandler<string, string, string>(
            "face_swap", EnqueueSetEmotionPortraitWipeSpec);

        runner.AddCommandHandler<string, string>(
            "mirror", EnqueueMirrorSetSpec);
    }
    
    private void BindCharRigStaging(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string, string, string>(
            "move_by", EnqueueSetAnchorOffsetSpecs);
        runner.AddCommandHandler<string, float, string, string>(
            "rotate_by", EnqueueRotateBySpec);

        runner.AddCommandHandler<string, float, string, string>(
            "scale_by", EnqueueSizeBySpec);

        runner.AddCommandHandler<string, string>(
            "move_reset", EnqueueSetPlaceResetSpecs);
        runner.AddCommandHandler<string, string, string>(
            "rotate_reset", EnqueueRotateResetSpec);

        runner.AddCommandHandler<string, string, string>(
            "scale_reset", EnqueueSizeResetSpec);

        // 제자리 몸짓 — 순변위 0. 이동 계열과 다른 노드라 같은 라인에 겹칠 수 있다.
        runner.AddCommandHandler<string, string, string, string, string, string>(
            "gesture", EnqueueGestureSpec);
        
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
        runner.AddCommandHandler<string, string, string, float, string, string>(
            "shot_focus_to", EnqueueShotZoomFocusSpec);
        runner.AddCommandHandler<float, string, string, string, string>(
            "shot_to", EnqueueShotToSpec);
        
        runner.AddCommandHandler<float, string, string>(
            "shot_zoom", EnqueueShotZoomSpec);
        runner.AddCommandHandler<string, string, string, string>(
            "shot_track", EnqueueShotTrackSpec);
        
        runner.AddCommandHandler<string, string>(
            "shot_reset", EnqueueShotResetSpec);
    }
    
    private void RegisterCharRigPlacementCommands(DialogueRunner runner)
    {
        // Show
        runner.AddCommandHandler<string, string, string>(
            "show", EnqueueShowAtSpec);
        
        // Nudge
        runner.AddCommandHandler<string, string, string, string>(
            "left", EnqueueNudgeLeftSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "right", EnqueueNudgeRightSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "up", EnqueueNudgeUpSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "down", EnqueueNudgeDownSpec);
    }
    
    private void RegisterCharFocusPlacementCommands(DialogueRunner runner)
    {
        // Char focus placement
        runner.AddCommandHandler<string, string, string, string, string>(
            "place", EnqueuePlaceCharacterFocusSpec);
        
        runner.AddCommandHandler<string, string, string, string>(
            "place_left", EnqueueFocusToLeftSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "place_center", EnqueueFocusToCenterSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "place_right", EnqueueFocusToRightSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "place_tl", EnqueueFocusToTopLeftSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "place_top", EnqueueFocusToTopSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "place_tr", EnqueueFocusToTopRightSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "place_bl", EnqueueFocusToBottomLeftSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "place_bottom", EnqueueFocusToBottomSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "place_br", EnqueueFocusToBottomRightSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "place_inner_tl", EnqueueFocusToInnerTopLeftSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "place_inner_tr", EnqueueFocusToInnerTopRightSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "place_inner_bl", EnqueueFocusToInnerBottomLeftSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "place_inner_br", EnqueueFocusToInnerBottomRightSpec);
        
        // Char focus depth
        // Generic form:
        // <<at c1 close bust 12fr>>
        // <<at c1 front>>
        // <<at c1 7 face 8fr>>
        runner.AddCommandHandler<string, string, string, string, string>(
            "size", EnqueueDepthAtPresetSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "size_far", EnqueueDepthAtFarSpec);
        
        runner.AddCommandHandler<string, string, string, string>(
            "size_back", EnqueueDepthAtBackSpec);
        
        runner.AddCommandHandler<string, string, string, string>(
            "size_mid", EnqueueDepthAtMidSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "size_front", EnqueueDepthAtFrontSpec);
        
        runner.AddCommandHandler<string, string, string, string>(
            "size_close", EnqueueDepthAtCloseSpec);
        
        runner.AddCommandHandler<string, float, string>(
            "size_reset", EnqueueDepthResetSpec);
    }
    
    private void BindCharRigPresentation(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>(
            "fade_in", EnqueueFadeInSpec);

        runner.AddCommandHandler<string, string>(
            "fade_out", EnqueueFadeOutSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "slide_in", EnqueueSlideInSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "slide_out", EnqueueSlideOutSpec);
        
        runner.AddCommandHandler<string, string, string, string>(
            "char_move_to", EnqueueMoveByUnitCharSpec);
        
        runner.AddCommandHandler<string, float, string, string>(
            "char_scale_to", EnqueueScaleSpec);

        runner.AddCommandHandler<string, float, string, string>(
            "char_rotate_to", EnqueuePortraitRotateToSpec);
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
    
    private void BindDepthFocus(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, float, string>(
            "focus_on", EnqueueDepthFocusOnSpec);

        runner.AddCommandHandler<string>(
            "focus_clear", EnqueueDepthFocusClearSpec);
    }

    private void BindStageDepthDefocus(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, float>(
            "screen_blur", EnqueueStage00LayerBlurSpec);

        runner.AddCommandHandler<string, float>(
            "screen_blur_s1", EnqueueStage01LayerBlurSpec);

        runner.AddCommandHandler<string, float>(
            "screen_blur_s2", EnqueueStage02LayerBlurSpec);

        runner.AddCommandHandler(
            "screen_blur_reset", EnqueueStageLayerBlurClearSpec);
    }

    private void Collect(CommandSpecBase spec)
    {
        _playbackDriver.Enqueue(spec);
    }
}
