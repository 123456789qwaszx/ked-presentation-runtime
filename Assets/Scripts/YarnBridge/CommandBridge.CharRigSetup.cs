using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueSetupCharRigSpec(
        string slotKey,
        string stageKey = "stage00",
        string layerKey = "mid")
        => Collect(new SetupCharRigCommandSpec
        {
            roleKey = slotKey,
            rigPrefab = _charRigPrefab,

            stage = PresentationStageKeyParser.Parse(stageKey),
            layer = PresentationDepthLayerKeyParser.Parse(layerKey)
        });

    private void EnqueueSetupCharRigStage00Spec(string slotKey, string layerKey = "mid")
        => EnqueueSetupCharRigAtDepthSpec(slotKey, PresentationStageKey.Stage00, layerKey);

    private void EnqueueSetupCharRigStage01Spec(string slotKey, string layerKey = "mid")
        => EnqueueSetupCharRigAtDepthSpec(slotKey, PresentationStageKey.Stage01, layerKey);

    private void EnqueueSetupCharRigStage02Spec(string slotKey, string layerKey = "mid")
        => EnqueueSetupCharRigAtDepthSpec(slotKey, PresentationStageKey.Stage02, layerKey);

    private void EnqueueSetupCharRigAtDepthSpec(
        string slotKey,
        PresentationStageKey stage,
        string layerKey)
        => Collect(new SetupCharRigCommandSpec
        {
            roleKey = slotKey,
            rigPrefab = _charRigPrefab,

            stage = stage,
            layer = PresentationDepthLayerKeyParser.Parse(layerKey)
        });

    private void EnqueueCastCharacterSpec(
        string slotKey,
        string characterKey,
        string variantKey = "a",
        string emotionKey = "1")
    {
        var castSpec = new CastCharacterCommandSpec
        {
            slotKey = slotKey,
            characterKey = characterKey
        };

        Collect(castSpec);

        EnqueueSetPortraitPoseSpec(slotKey, variantKey);
        EnqueueSetPortraitFaceSpec(slotKey, emotionKey);
        EnqueueSetAnchorSpecs(slotKey);
    }

    private void EnqueueSetAnchorSpecs(string slotKey, bool resetSlotPos = true,
        bool resetCharPos = true)
        => Collect(new SetAnchorCommandSpecCharR
        {
            slotKey = slotKey,
            target = CharacterRigTarget.CharacterPortrait_VisualOffset,
            resetSlotPos = resetSlotPos,
            resetCharacterPos = resetCharPos
        });

    private void EnqueueMirrorSetSpec(
        string roleKey,
        string directionToken = "")
        => Collect(new MirrorCharacterCommandSpecCharR
        {
            slotKey = roleKey,
            mode = CharacterMirrorModeParser.Parse(directionToken),
            target = CharacterRigTarget.CharacterPortrait_ActingScale_X,
            duration = 0f,
        });

    private void EnqueueSetPortraitPoseSpec(string slotKey, string variantKey)
        => Collect(new SetPortraitPoseCommandSpecCharR
        {
            slotKey = slotKey,
            variantKey = variantKey,
        });

    private void EnqueueSetPortraitFaceSpec(string slotKey, string emotionKey)
        => Collect(new SetPortraitSpriteCommandSpecCharR
        {
            slotKey = slotKey,
            portrait = new PortraitIdentity
            {
                character = "",
                variant = "",
                emotion = emotionKey
            }
        });

    private void EnqueueSetAnchorOffsetSpecs(
        string slotKey,
        string xToken = "0u",
        string yToken = "0u",
        string durationToken = "0.4s",
        string easeToken = "")
    {
        var spec = new MoveByCommandSpecCharR
        {
            slotKey = slotKey,
            target = CharacterRigTarget.CharSlot_Track,
            useAbsolutePosition = false,
            delta = new Vector2(ParseSignedUnit(xToken), ParseSignedUnit(yToken)),
            duration = YarnDurationParser.Parse(durationToken)
        };

        ApplyEaseToken(spec, easeToken);
        Collect(spec);
    }

    /// <summary>
    /// 다섯째 토큰의 두 갈래: "@이름" = curves.json의 커스텀 곡선,
    /// 그 외 = EaseKind 이름. 미지정("")·실패는 스펙 기본값 OutCubic —
    /// 기존 4-인자 대본의 재생 결과 불변.
    /// </summary>
    private void ApplyEaseToken(MoveByCommandSpecCharR spec, string easeToken)
    {
        if (!string.IsNullOrWhiteSpace(easeToken) && easeToken[0] == '@')
        {
            string curveName = easeToken.Substring(1);

            if (_easeCurves.TryGet(curveName, out Ked.Presentation.Core.CurveKey[] keys))
            {
                spec.customCurveKeys = keys;
                return;
            }

            // 침묵 금지 — 1차 방어는 VnTool 저작 검증이고, 여기서는 소리 내고 굴러간다.
            Debug.LogError(
                $"[YarnCommandBridge] Unknown ease curve '{easeToken}' — " +
                $"{EaseCurveLibrary.BundleFileName}에 없다. Fallback to {spec.ease}.");
            return;
        }

        spec.ease = YarnEaseParser.Parse(easeToken);
    }

    private void EnqueueSizeBySpec(string roleKey, float multiplier, string durationToken = "0.4s")
        => Collect(new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharSlot_Scale,

            toScale = new Vector2(multiplier, multiplier),
            relativeToCurrent = true,

            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueSetPlaceResetSpecs(string slotKey, string durationToken = "0.4s")
    {
        float duration = YarnDurationParser.Parse(durationToken);

        var slotOffsetSpec = new MoveByCommandSpecCharR
        {
            slotKey = slotKey,
            target = CharacterRigTarget.CharSlot_Track,
            useAbsolutePosition = true,
            delta = new Vector2(0, 0),
            duration = duration
        };

        var spec2 = new MoveByCommandSpecCharR
        {
            slotKey = slotKey,
            target = CharacterRigTarget.CharSlot_Track_Focus,
            useAbsolutePosition = true,
            delta = new Vector2(0, 0),
            duration = duration
        };

        Collect(slotOffsetSpec);
        Collect(spec2);
    }

    private void EnqueueSizeResetSpec(string roleKey, string durationToken = "0.4s")
        => Collect(new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharSlot_Scale,

            toScale = new Vector2(1, 1),

            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueCharacterSiblingFrontSpec(string roleKey)
        => Collect(new SetCharacterSiblingOrderCommandSpecCharR
        {
            slotKey = roleKey,
            mode = CharacterRigSiblingOrderMode.Front
        });

    private void EnqueueCharacterSiblingBackSpec(string roleKey)
        => Collect(new SetCharacterSiblingOrderCommandSpecCharR
        {
            slotKey = roleKey,
            mode = CharacterRigSiblingOrderMode.Back
        });

    private void EnqueueMoveCharacterRigToStageLayerSpec(
        string roleKey,
        string stageKey = "stage00",
        string layerKey = "mid")
        => Collect(new MoveCharacterRigToStageLayerCommandSpecCharR
        {
            slotKey = roleKey,

            stage = PresentationStageKeyParser.Parse(stageKey),
            layer = PresentationDepthLayerKeyParser.Parse(layerKey),

            siblingMode = CharacterRigReparentSiblingMode.Front
        });

    private void EnqueueMoveCharacterRigToStage00LayerSpec(string roleKey, string layerKey = "mid")
        => EnqueueMoveCharacterRigToStageLayerSpec(roleKey, "stage00", layerKey);

    private void EnqueueMoveCharacterRigToStage01LayerSpec(string roleKey, string layerKey = "mid")
        => EnqueueMoveCharacterRigToStageLayerSpec(roleKey, "stage01", layerKey);

    private void EnqueueMoveCharacterRigToStage02LayerSpec(string roleKey, string layerKey = "mid")
        => EnqueueMoveCharacterRigToStageLayerSpec(roleKey, "stage02", layerKey);
}