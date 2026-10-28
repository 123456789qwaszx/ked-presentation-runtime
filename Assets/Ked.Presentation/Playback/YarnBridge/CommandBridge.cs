using System.Collections.Generic;
using Ked.Presentation.Core;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private readonly YarnPlaybackDriver _playbackDriver;
    private readonly StageCommandParser _parser;
    private readonly DialogueBoxPresentationController _dialogueBoxPresentation;

    // Forward가 쓰는 스크래치 — 커맨드 하나가 스펙 여럿을 낳는다(show·cast·bg_spawn).
    private readonly List<CommandSpecBase> _parsed = new();

    public YarnCommandBridge(
        DialogueRunner runner,
        YarnPlaybackDriver playbackDriver,
        StageCommandParser parser,
        DialogueBoxPresentationController dialogueBoxPresentation)
    {
        _playbackDriver = playbackDriver;
        _parser = parser;
        _dialogueBoxPresentation = dialogueBoxPresentation;

        BindRunnerCommands(runner);
    }

    private void BindRunnerCommands(DialogueRunner runner)
    {
        RegisterCharFocusPlacementCommands(runner);
        RegisterCharRigPlacementCommands(runner);
        BindBackground(runner);
        
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

        runner.AddCommandHandler<string>(
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
        runner.AddCommandHandler<string, string, string, string>(
            "rotate_by", EnqueueRotateBySpec);

        runner.AddCommandHandler<string, string, string, string>(
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

        // 모션 — motions.json의 클립(모션 툴·Animation 창에서 내보낸 것). 길이는 클립이 안다.
        // 세 번째 인자 relative|absolute(생략 relative)가 키 값을 어떻게 읽을지 정한다 — 클립은 mode를 모른다(motions/2).
        runner.AddCommandHandler<string, string, string>(
            "motion", EnqueueMotionSpec);
        
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
        runner.AddCommandHandler<string, string, string, string, string, string>(
            "shot_focus_to", EnqueueShotZoomFocusSpec);
        runner.AddCommandHandler<string, string, string, string, string>(
            "shot_to", EnqueueShotToSpec);
        
        runner.AddCommandHandler<string, string, string>(
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
        
        runner.AddCommandHandler<string, string, string>(
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
        
        runner.AddCommandHandler<string, string, string, string>(
            "char_scale_to", EnqueueScaleSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "char_rotate_to", EnqueuePortraitRotateToSpec);
    }
    
    private void BindCharRigComposition(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string, string>(
            "char_visual", EnqueueCharVisualPresetSpec);
        
        runner.AddCommandHandler<string, string, string>(
            "char_visual_focus", EnqueueCharFocusSpec);

        runner.AddCommandHandler<string, string, string>(
            "char_visual_defocus", EnqueueCharDefocusSpec);


        runner.AddCommandHandler<string, string, string>(
            "char_visual_dim", EnqueueCharDimSpec);

        runner.AddCommandHandler<string, string, string>(
            "char_visual_silhouette", EnqueueCharSilhouetteSpec);

        runner.AddCommandHandler<string, string, string>(
            "char_visual_inner_rim", EnqueueCharInnerRimSpec);

        runner.AddCommandHandler<string, string, string>(
            "char_visual_outer_rim", EnqueueCharOuterRimSpec);
        
        runner.AddCommandHandler<string, string>(
            "char_visual_clear", EnqueueCharRigVisualClearSpec);
    }
    
    // 배경 = 캐스팅하지 않은 캐릭터 슬롯. 뜻은 무대의 StageCommandParser.Background에 있다.
    // bg_place·bg_size는 지운 어휘라 등록하지 않는다 — 남은 대본은 Yarn이 "모르는 커맨드"로 소리 낸다.
    private void BindBackground(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>(
            "bg_spawn", EnqueueSpawnBackgroundSpec);
        
        runner.AddCommandHandler<string, string, string>(
            "bg_slot00", EnqueueSpawnBackgroundStage00Spec);
        runner.AddCommandHandler<string, string, string>(
            "bg_slot01", EnqueueSpawnBackgroundStage01Spec);
        runner.AddCommandHandler<string, string, string>(
            "bg_slot02", EnqueueSpawnBackgroundStage02Spec);

        runner.AddCommandHandler<string, string>(
            "bg_sprite", EnqueueSetBackgroundSpriteSpec);

        runner.AddCommandHandler<string, string>(
            "bg_fade_in", EnqueueFadeInBackgroundDslSpec);

        runner.AddCommandHandler<string, string>(
            "bg_fade_out", EnqueueFadeOutBackgroundDslSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "bg_move", EnqueueMoveBackgroundDslSpec);

        runner.AddCommandHandler<string, string, string>(
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
        runner.AddCommandHandler<string, string>(
            "screen_flash", EnqueueScreenFlashPresetSpec);
        runner.AddCommandHandler(
            "screen_flash_clear", EnqueueScreenFlashClearSpec);

        runner.AddCommandHandler<string, string, string>(
            "screen_vignette", EnqueueScreenVignettePresetSpec);
        runner.AddCommandHandler<string>(
            "screen_vignette_clear", EnqueueScreenVignetteClearSpec);

        runner.AddCommandHandler<string, string, string>(
            "screen_noise", EnqueueScreenNoisePresetSpec);
        runner.AddCommandHandler<string>(
            "screen_noise_clear", EnqueueScreenNoiseClearSpec);
    }
    
    private void BindDepthFocus(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string>(
            "focus_on", EnqueueDepthFocusOnSpec);

        runner.AddCommandHandler<string>(
            "focus_clear", EnqueueDepthFocusClearSpec);
    }

    private void BindStageDepthDefocus(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>(
            "screen_blur", EnqueueStage00LayerBlurSpec);

        runner.AddCommandHandler<string, string>(
            "screen_blur_s1", EnqueueStage01LayerBlurSpec);

        runner.AddCommandHandler<string, string>(
            "screen_blur_s2", EnqueueStage02LayerBlurSpec);

        runner.AddCommandHandler(
            "screen_blur_reset", EnqueueStageLayerBlurClearSpec);
    }

    /// <summary>
    /// Yarn이 나눠 준 토큰을 StageCommand로 묶어 무대 파서에 넘긴다. 뜻(인자 자리·생략값·단위)은
    /// 파서가 정한다 — 여기는 텍스트를 옮기기만 한다. 생략된 인자는 null로 가고, 파서가 생략값을 채운다.
    /// </summary>
    private void Forward(string commandName, params string[] args)
    {
        _parsed.Clear();

        if (!_parser.TryParse(new StageCommand(commandName, args), _parsed))
        {
            Debug.LogError($"[YarnCommandBridge] 무대 파서가 모르는 커맨드 '{commandName}' — 등록표와 파서 표가 어긋났다.");
            return;
        }

        for (int i = 0; i < _parsed.Count; i++)
            _playbackDriver.Enqueue(_parsed[i]);
    }
}
