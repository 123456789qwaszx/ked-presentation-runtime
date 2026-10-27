// 전달 메서드 — Yarn 등록(CommandBridge.cs)이 가리키는 이름들. 인자는 전부 문자열이고 생략은 null.
// 뜻은 StageCommandParser.Table에 있다. 여기에는 생략값이 없다 — 두 곳이 되지 않게.
public sealed partial class YarnCommandBridge
{
    private void EnqueueWaitSpec(string duration = null)
        => Forward("pause", duration);

    private void EnqueueUIPatchSpec(string themeId = null)
        => Forward("ui_patch", themeId);

    private void EnqueuePresentationActorAliasSpec(string aliasSymbol = null, string targetKey = null)
        => Forward("actor", aliasSymbol, targetKey);

    private void EnqueueSetupCharRigSpec(string slotKey = null, string stageKey = null, string layerKey = null)
        => Forward("slot", slotKey, stageKey, layerKey);

    private void EnqueueSetupCharRigStage00Spec(string slotKey = null, string layerKey = null)
        => Forward("slot00", slotKey, layerKey);

    private void EnqueueSetupCharRigStage01Spec(string slotKey = null, string layerKey = null)
        => Forward("slot01", slotKey, layerKey);

    private void EnqueueSetupCharRigStage02Spec(string slotKey = null, string layerKey = null)
        => Forward("slot02", slotKey, layerKey);

    private void EnqueueCastCharacterSpec(string slotKey = null, string characterKey = null, string variantKey = null, string emotionKey = null)
        => Forward("cast", slotKey, characterKey, variantKey, emotionKey);

    private void EnqueueSetPortraitPoseSpec(string slotKey = null, string variantKey = null)
        => Forward("pose", slotKey, variantKey);

    private void EnqueueSetPortraitFaceSpec(string slotKey = null, string emotionKey = null)
        => Forward("face", slotKey, emotionKey);

    private void EnqueueSetEmotionPortraitWipeSpec(string targetKey = null, string emotion = null, string durationToken = null)
        => Forward("face_swap", targetKey, emotion, durationToken);

    private void EnqueueMirrorSetSpec(string roleKey = null, string directionToken = null)
        => Forward("mirror", roleKey, directionToken);

    private void EnqueueSetAnchorOffsetSpecs(string slotKey = null, string xToken = null, string yToken = null, string durationToken = null, string easeToken = null)
        => Forward("move_by", slotKey, xToken, yToken, durationToken, easeToken);

    private void EnqueueRotateBySpec(string roleKey = null, string degree = null, string durationToken = null, string easeToken = null)
        => Forward("rotate_by", roleKey, degree, durationToken, easeToken);

    private void EnqueueSizeBySpec(string roleKey = null, string multiplier = null, string durationToken = null, string easeToken = null)
        => Forward("scale_by", roleKey, multiplier, durationToken, easeToken);

    private void EnqueueSetPlaceResetSpecs(string slotKey = null, string durationToken = null)
        => Forward("move_reset", slotKey, durationToken);

    private void EnqueueRotateResetSpec(string roleKey = null, string durationToken = null, string easeToken = null)
        => Forward("rotate_reset", roleKey, durationToken, easeToken);

    private void EnqueueSizeResetSpec(string roleKey = null, string durationToken = null, string easeToken = null)
        => Forward("scale_reset", roleKey, durationToken, easeToken);

    private void EnqueueMotionSpec(string roleKey = null, string motionName = null)
        => Forward("motion", roleKey, motionName);

    private void EnqueueGestureSpec(string roleKey = null, string xAmpToken = null, string yAmpToken = null, string durationToken = null, string xEaseToken = null, string yEaseToken = null)
        => Forward("gesture", roleKey, xAmpToken, yAmpToken, durationToken, xEaseToken, yEaseToken);

    private void EnqueueCharacterSiblingFrontSpec(string roleKey = null)
        => Forward("sibling_front", roleKey);

    private void EnqueueCharacterSiblingBackSpec(string roleKey = null)
        => Forward("sibling_back", roleKey);

    private void EnqueueMoveCharacterRigToStageLayerSpec(string roleKey = null, string stageKey = null, string layerKey = null)
        => Forward("char_to", roleKey, stageKey, layerKey);

    private void EnqueueMoveCharacterRigToStage00LayerSpec(string roleKey = null, string layerKey = null)
        => Forward("char_to_s0", roleKey, layerKey);

    private void EnqueueMoveCharacterRigToStage01LayerSpec(string roleKey = null, string layerKey = null)
        => Forward("char_to_s1", roleKey, layerKey);

    private void EnqueueMoveCharacterRigToStage02LayerSpec(string roleKey = null, string layerKey = null)
        => Forward("char_to_s2", roleKey, layerKey);

    private void EnqueueShotZoomFocusSpec(string roleKey = null, string focusName = null, string screenPointName = null, string zoom = null, string durationToken = null, string easeToken = null)
        => Forward("shot_focus_to", roleKey, focusName, screenPointName, zoom, durationToken, easeToken);

    private void EnqueueShotToSpec(string zoom = null, string xToken = null, string yToken = null, string durationToken = null, string easeToken = null)
        => Forward("shot_to", zoom, xToken, yToken, durationToken, easeToken);

    private void EnqueueShotZoomSpec(string zoom = null, string durationToken = null, string easeToken = null)
        => Forward("shot_zoom", zoom, durationToken, easeToken);

    private void EnqueueShotTrackSpec(string xToken = null, string yToken = null, string durationToken = null, string easeToken = null)
        => Forward("shot_track", xToken, yToken, durationToken, easeToken);

    private void EnqueueShotResetSpec(string durationToken = null, string easeToken = null)
        => Forward("shot_reset", durationToken, easeToken);

    private void EnqueueShowAtSpec(string roleKey = null, string faceToken = null, string durationToken = null)
        => Forward("show", roleKey, faceToken, durationToken);

    private void EnqueueNudgeLeftSpec(string roleKey = null, string unitToken = null, string durationToken = null, string easeToken = null)
        => Forward("left", roleKey, unitToken, durationToken, easeToken);

    private void EnqueueNudgeRightSpec(string roleKey = null, string unitToken = null, string durationToken = null, string easeToken = null)
        => Forward("right", roleKey, unitToken, durationToken, easeToken);

    private void EnqueueNudgeUpSpec(string roleKey = null, string unitToken = null, string durationToken = null, string easeToken = null)
        => Forward("up", roleKey, unitToken, durationToken, easeToken);

    private void EnqueueNudgeDownSpec(string roleKey = null, string unitToken = null, string durationToken = null, string easeToken = null)
        => Forward("down", roleKey, unitToken, durationToken, easeToken);

    private void EnqueuePlaceCharacterFocusSpec(string roleKey = null, string focus = null, string screenPoint = null, string durationToken = null, string easeToken = null)
        => Forward("place", roleKey, focus, screenPoint, durationToken, easeToken);

    private void EnqueueFocusToLeftSpec(string roleKey = null, string focus = null, string durationToken = null, string easeToken = null)
        => Forward("place_left", roleKey, focus, durationToken, easeToken);

    private void EnqueueFocusToCenterSpec(string roleKey = null, string focus = null, string durationToken = null, string easeToken = null)
        => Forward("place_center", roleKey, focus, durationToken, easeToken);

    private void EnqueueFocusToRightSpec(string roleKey = null, string focus = null, string durationToken = null, string easeToken = null)
        => Forward("place_right", roleKey, focus, durationToken, easeToken);

    private void EnqueueFocusToTopLeftSpec(string roleKey = null, string focus = null, string durationToken = null, string easeToken = null)
        => Forward("place_tl", roleKey, focus, durationToken, easeToken);

    private void EnqueueFocusToTopSpec(string roleKey = null, string focus = null, string durationToken = null, string easeToken = null)
        => Forward("place_top", roleKey, focus, durationToken, easeToken);

    private void EnqueueFocusToTopRightSpec(string roleKey = null, string focus = null, string durationToken = null, string easeToken = null)
        => Forward("place_tr", roleKey, focus, durationToken, easeToken);

    private void EnqueueFocusToBottomLeftSpec(string roleKey = null, string focus = null, string durationToken = null, string easeToken = null)
        => Forward("place_bl", roleKey, focus, durationToken, easeToken);

    private void EnqueueFocusToBottomSpec(string roleKey = null, string focus = null, string durationToken = null, string easeToken = null)
        => Forward("place_bottom", roleKey, focus, durationToken, easeToken);

    private void EnqueueFocusToBottomRightSpec(string roleKey = null, string focus = null, string durationToken = null, string easeToken = null)
        => Forward("place_br", roleKey, focus, durationToken, easeToken);

    private void EnqueueFocusToInnerTopLeftSpec(string roleKey = null, string focus = null, string durationToken = null, string easeToken = null)
        => Forward("place_inner_tl", roleKey, focus, durationToken, easeToken);

    private void EnqueueFocusToInnerTopRightSpec(string roleKey = null, string focus = null, string durationToken = null, string easeToken = null)
        => Forward("place_inner_tr", roleKey, focus, durationToken, easeToken);

    private void EnqueueFocusToInnerBottomLeftSpec(string roleKey = null, string focus = null, string durationToken = null, string easeToken = null)
        => Forward("place_inner_bl", roleKey, focus, durationToken, easeToken);

    private void EnqueueFocusToInnerBottomRightSpec(string roleKey = null, string focus = null, string durationToken = null, string easeToken = null)
        => Forward("place_inner_br", roleKey, focus, durationToken, easeToken);

    private void EnqueueDepthAtPresetSpec(string roleKey = null, string depthArg = null, string preserveFocusArg = null, string durationToken = null, string easeToken = null)
        => Forward("size", roleKey, depthArg, preserveFocusArg, durationToken, easeToken);

    private void EnqueueDepthAtFarSpec(string roleKey = null, string preserveFocusArg = null, string durationToken = null, string easeToken = null)
        => Forward("size_far", roleKey, preserveFocusArg, durationToken, easeToken);

    private void EnqueueDepthAtBackSpec(string roleKey = null, string preserveFocusArg = null, string durationToken = null, string easeToken = null)
        => Forward("size_back", roleKey, preserveFocusArg, durationToken, easeToken);

    private void EnqueueDepthAtMidSpec(string roleKey = null, string preserveFocusArg = null, string durationToken = null, string easeToken = null)
        => Forward("size_mid", roleKey, preserveFocusArg, durationToken, easeToken);

    private void EnqueueDepthAtFrontSpec(string roleKey = null, string preserveFocusArg = null, string durationToken = null, string easeToken = null)
        => Forward("size_front", roleKey, preserveFocusArg, durationToken, easeToken);

    private void EnqueueDepthAtCloseSpec(string roleKey = null, string preserveFocusArg = null, string durationToken = null, string easeToken = null)
        => Forward("size_close", roleKey, preserveFocusArg, durationToken, easeToken);

    private void EnqueueDepthResetSpec(string roleKey = null, string duration = null, string easeToken = null)
        => Forward("size_reset", roleKey, duration, easeToken);

    private void EnqueueFadeInSpec(string roleKey = null, string durationToken = null)
        => Forward("fade_in", roleKey, durationToken);

    private void EnqueueFadeOutSpec(string roleKey = null, string durationToken = null)
        => Forward("fade_out", roleKey, durationToken);

    private void EnqueueSlideInSpec(string roleKey = null, string direction = null, string distanceToken = null, string durationToken = null)
        => Forward("slide_in", roleKey, direction, distanceToken, durationToken);

    private void EnqueueSlideOutSpec(string roleKey = null, string direction = null, string distanceToken = null, string durationToken = null)
        => Forward("slide_out", roleKey, direction, distanceToken, durationToken);

    private void EnqueueMoveByUnitCharSpec(string roleKey = null, string xToken = null, string yToken = null, string durationToken = null)
        => Forward("char_move_to", roleKey, xToken, yToken, durationToken);

    private void EnqueueScaleSpec(string roleKey = null, string xyValue = null, string durationToken = null, string easeToken = null)
        => Forward("char_scale_to", roleKey, xyValue, durationToken, easeToken);

    private void EnqueuePortraitRotateToSpec(string roleKey = null, string degree = null, string durationToken = null, string easeToken = null)
        => Forward("char_rotate_to", roleKey, degree, durationToken, easeToken);

    private void EnqueueCharVisualPresetSpec(string roleKey = null, string presetKey = null, string intensity = null, string durationToken = null)
        => Forward("char_visual", roleKey, presetKey, intensity, durationToken);

    private void EnqueueCharFocusSpec(string roleKey = null, string intensity = null, string durationToken = null)
        => Forward("char_visual_focus", roleKey, intensity, durationToken);

    private void EnqueueCharDefocusSpec(string roleKey = null, string intensity = null, string durationToken = null)
        => Forward("char_visual_defocus", roleKey, intensity, durationToken);

    private void EnqueueCharDimSpec(string roleKey = null, string intensity = null, string durationToken = null)
        => Forward("char_visual_dim", roleKey, intensity, durationToken);

    private void EnqueueCharSilhouetteSpec(string roleKey = null, string intensity = null, string durationToken = null)
        => Forward("char_visual_silhouette", roleKey, intensity, durationToken);

    private void EnqueueCharInnerRimSpec(string roleKey = null, string intensity = null, string durationToken = null)
        => Forward("char_visual_inner_rim", roleKey, intensity, durationToken);

    private void EnqueueCharOuterRimSpec(string roleKey = null, string intensity = null, string durationToken = null)
        => Forward("char_visual_outer_rim", roleKey, intensity, durationToken);

    private void EnqueueCharRigVisualClearSpec(string roleKey = null, string durationToken = null)
        => Forward("char_visual_clear", roleKey, durationToken);

    private void EnqueueSpawnBackgroundRigSpec(string rigKey = null, string spriteKey = null)
        => Forward("bg_spawn", rigKey, spriteKey);

    private void EnqueueSpawnBackgroundRigStage00Spec(string rigKey = null, string spriteKey = null, string layerKey = null)
        => Forward("bg_slot00", rigKey, spriteKey, layerKey);

    private void EnqueueSpawnBackgroundRigStage01Spec(string rigKey = null, string spriteKey = null, string layerKey = null)
        => Forward("bg_slot01", rigKey, spriteKey, layerKey);

    private void EnqueueSpawnBackgroundRigStage02Spec(string rigKey = null, string spriteKey = null, string layerKey = null)
        => Forward("bg_slot02", rigKey, spriteKey, layerKey);

    private void EnqueueSetBackgroundAnchorDslSpec(string rigKey = null, string xToken = null, string yToken = null, string rotationZ = null)
        => Forward("bg_place", rigKey, xToken, yToken, rotationZ);

    private void EnqueueSetBackgroundSpriteSpec(string rigKey = null, string spriteKey = null, string layerKey = null)
        => Forward("bg_sprite", rigKey, spriteKey, layerKey);

    private void EnqueueSetBackgroundOriginSizeSpec(string rigKey = null, string scaleArg = null)
        => Forward("bg_size", rigKey, scaleArg);

    private void EnqueueFadeInBackgroundDslSpec(string rigKey = null, string durationToken = null)
        => Forward("bg_fade_in", rigKey, durationToken);

    private void EnqueueFadeOutBackgroundDslSpec(string rigKey = null, string durationToken = null)
        => Forward("bg_fade_out", rigKey, durationToken);

    private void EnqueueMoveBackgroundDslSpec(string rigKey = null, string xToken = null, string yToken = null, string durationToken = null)
        => Forward("bg_move", rigKey, xToken, yToken, durationToken);

    private void EnqueueScaleBackgroundDslSpec(string rigKey = null, string scale = null, string durationToken = null)
        => Forward("bg_scale", rigKey, scale, durationToken);

    private void EnqueueSlantedMaskCutInSpec(string stage = null, string durationToken = null)
        => Forward("tx_slant_in", stage, durationToken);

    private void EnqueueSlantedMaskCutOutSpec(string stage = null, string durationToken = null)
        => Forward("tx_slant_out", stage, durationToken);

    private void EnqueueHorizontalStripOpenInSpec(string stage = null, string durationToken = null)
        => Forward("tx_hstrip_open", stage, durationToken);

    private void EnqueueHorizontalStripCloseOutSpec(string stage = null, string durationToken = null)
        => Forward("tx_hstrip_close", stage, durationToken);

    private void EnqueueHorizontalStripCutInSpec(string stage = null, string durationToken = null)
        => Forward("tx_hstrip_in", stage, durationToken);

    private void EnqueueHorizontalStripCutOutSpec(string stage = null, string durationToken = null)
        => Forward("tx_hstrip_out", stage, durationToken);

    private void EnqueueVerticalStripOpenInSpec(string stage = null, string durationToken = null)
        => Forward("tx_vstrip_open", stage, durationToken);

    private void EnqueueVerticalStripCloseOutSpec(string stage = null, string durationToken = null)
        => Forward("tx_vstrip_close", stage, durationToken);

    private void EnqueueVerticalStripCutInSpec(string stage = null, string durationToken = null)
        => Forward("tx_vstrip_in", stage, durationToken);

    private void EnqueueVerticalStripCutOutSpec(string stage = null, string durationToken = null)
        => Forward("tx_vstrip_out", stage, durationToken);

    private void EnqueueDiagonalBandCutInSpec(string stage = null, string durationToken = null)
        => Forward("tx_band_in", stage, durationToken);

    private void EnqueueDiagonalBandCutOutSpec(string stage = null, string durationToken = null)
        => Forward("tx_band_out", stage, durationToken);

    private void EnqueueCircleIrisInSpec(string stage = null, string durationToken = null)
        => Forward("tx_iris_in", stage, durationToken);

    private void EnqueueCircleIrisOutSpec(string stage = null, string durationToken = null)
        => Forward("tx_iris_out", stage, durationToken);

    private void EnqueueDazeFadeCloseSpec(string stage = null, string durationToken = null)
        => Forward("tx_daze_in", stage, durationToken);

    private void EnqueueTransitionOutDazeFadeSpec(string stage = null, string durationToken = null)
        => Forward("tx_daze_out", stage, durationToken);

    private void EnqueueVerticalStripCoverSpec(string stage = null, string durationToken = null)
        => Forward("tx_strip_in", stage, durationToken);

    private void EnqueueTransitionOutStripSpec(string stage = null, string durationToken = null)
        => Forward("tx_strip_out", stage, durationToken);

    private void EnqueueStageMaskClearSpec()
        => Forward("tx_stage_mask_clear");

    private void EnqueuePlayBgmSpec(string clipKey = null, string fadeDurationToken = null)
        => Forward("bgm", clipKey, fadeDurationToken);

    private void EnqueueStopBgmSpec(string fadeDurationToken = null)
        => Forward("stop_bgm", fadeDurationToken);

    private void EnqueuePlaySfxSpec(string clipKey = null)
        => Forward("sfx", clipKey);

    private void EnqueueStopAllSfxSpec()
        => Forward("stop_all_sfx");

    private void EnqueuePlayVoiceSpec(string clipKey = null)
        => Forward("voice", clipKey);

    private void EnqueueStopVoiceSpec()
        => Forward("stop_voice");

    private void EnqueueScreenFlashPresetSpec(string presetKey = null, string intensity = null)
        => Forward("screen_flash", presetKey, intensity);

    private void EnqueueScreenFlashClearSpec()
        => Forward("screen_flash_clear");

    private void EnqueueScreenVignettePresetSpec(string presetKey = null, string intensity = null, string durationToken = null)
        => Forward("screen_vignette", presetKey, intensity, durationToken);

    private void EnqueueScreenVignetteClearSpec(string durationToken = null)
        => Forward("screen_vignette_clear", durationToken);

    private void EnqueueScreenNoisePresetSpec(string presetKey = null, string intensity = null, string durationToken = null)
        => Forward("screen_noise", presetKey, intensity, durationToken);

    private void EnqueueScreenNoiseClearSpec(string durationToken = null)
        => Forward("screen_noise_clear", durationToken);

    private void EnqueueDepthFocusOnSpec(string slotKey = null, string maxBlur = null, string durationToken = null)
        => Forward("focus_on", slotKey, maxBlur, durationToken);

    private void EnqueueDepthFocusClearSpec(string durationToken = null)
        => Forward("focus_clear", durationToken);

    private void EnqueueStage00LayerBlurSpec(string layerKey = null, string amount = null)
        => Forward("screen_blur", layerKey, amount);

    private void EnqueueStage01LayerBlurSpec(string layerKey = null, string amount = null)
        => Forward("screen_blur_s1", layerKey, amount);

    private void EnqueueStage02LayerBlurSpec(string layerKey = null, string amount = null)
        => Forward("screen_blur_s2", layerKey, amount);

    private void EnqueueStageLayerBlurClearSpec()
        => Forward("screen_blur_reset");
}
