using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private const string TyrantProtagonistSlotKey = "tyrant";

    private const string DefaultTyrantScale = "5";
    private const string DefaultTyrantDownUnit = "4u";
    private const string DefaultTyrantRightUnit = "0.6u";
    
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

    private void EnqueueSetupTyrantProtagonistSpec()
    {
        EnqueueSetupProtagonistCharRigSpec(TyrantProtagonistSlotKey);
        
        EnqueueCastCharacterSpec(
            TyrantProtagonistSlotKey,
            "Tyrant",
            "a",
            emotionKey: "2");

        EnqueueFadeInDslSpec(
            TyrantProtagonistSlotKey,
            "-1s");

        EnqueueNudgeDownSpec(
            TyrantProtagonistSlotKey,
            DefaultTyrantDownUnit);

        EnqueueNudgeRightSpec(
            TyrantProtagonistSlotKey,
            DefaultTyrantRightUnit);
    }

    private void EnqueueSetupProtagonistCharRigSpec(string slotKey)
        => Collect(new SetupCharRigCommandSpec
        {
            roleKey = slotKey,
            rigPrefab = _charRigPrefab,
            useProtagonistSlot = true
        });

    private void EnqueueCastCharacterSpec(
        string slotKey,
        string characterKey,
        string variantKey = "a",
        string emotionKey = "2")
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
            defaultEmotionKey = PortraitResolver.DefaultEmotion
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
        string durationToken = "0.4s")
        => Collect(new MoveByCommandSpecCharR
        {
            slotKey = slotKey,
            target = CharacterRigTarget.CharSlot_Track,
            useAbsolutePosition = false,
            delta = new Vector2(ParseSignedUnit(xToken), ParseSignedUnit(yToken)),
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueSizeBySpec(string roleKey, float multiplier, string durationToken = "0.4s")
        => Collect(new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharSlot_Scale,

            toScale = new Vector2(multiplier, multiplier),
            relativeToCurrent = true,

            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueRotateBySpec(string roleKey, float degree, string durationToken = "0.4s")
        => Collect(new RotateToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharSlot_SwayPivot,

            toEuler = new Vector3(0f, 0f, degree),
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

    private void EnqueueRotateResetSpec(string roleKey, string durationToken = "0.4s")
        => Collect(new RotateToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharSlot_SwayPivot,

            toEuler = new Vector3(0f, 0f, 0f),

            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueSizeResetSpec(string roleKey, string durationToken = "0.4s")
        => Collect(new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharSlot_Scale,

            toScale = new Vector2(1, 1),

            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueSpriteColorToDslSpec(
        string roleKey,
        float r,
        float g,
        float b,
        string durationToken = "8fr")
        => Collect(new ColorToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortraitSprite_Image,
            color = new Color(r, g, b, 1f),
            keepAlpha = true,
            duration = YarnDurationParser.Parse(durationToken, 0.35f)
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