using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueSetupCharRigSpec(
        string slotKey,
        string stageKey = "stage00",
        string layerKey = "mid")
    {
        var spec = new SetupCharRigCommandSpec
        {
            roleKey = slotKey,
            rigPrefab = _charRigPrefab,

            stage = PresentationStageKeyParser.Parse(
                stageKey,
                PresentationStageKey.Stage00),

            layer = PresentationDepthLayerKeyParser.Parse(
                layerKey,
                PresentationDepthLayerKey.Mid)
        };

        Collect(spec);
    }
    
    private void EnqueueSetupCharRigStage00Spec(
        string slotKey,
        string layerKey = "mid")
    {
        EnqueueSetupCharRigAtDepthSpec(
            slotKey,
            PresentationStageKey.Stage00,
            layerKey);
    }

    private void EnqueueSetupCharRigStage01Spec(
        string slotKey,
        string layerKey = "mid")
    {
        EnqueueSetupCharRigAtDepthSpec(
            slotKey,
            PresentationStageKey.Stage01,
            layerKey);
    }

    private void EnqueueSetupCharRigStage02Spec(
        string slotKey,
        string layerKey = "mid")
    {
        EnqueueSetupCharRigAtDepthSpec(
            slotKey,
            PresentationStageKey.Stage02,
            layerKey);
    }

    private void EnqueueSetupCharRigAtDepthSpec(
        string slotKey,
        PresentationStageKey stage,
        string layerKey)
    {
        var spec = new SetupCharRigCommandSpec
        {
            roleKey = slotKey,
            rigPrefab = _charRigPrefab,

            stage = stage,
            layer = PresentationDepthLayerKeyParser.Parse(
                layerKey,
                PresentationDepthLayerKey.Mid)
        };

        Collect(spec);
    }
    
    private void EnqueueSetupProtagonistCharRigSpec(string slotKey)
    {
        var spec = new SetupCharRigCommandSpec
        {
            roleKey = slotKey,
            rigPrefab = _charRigPrefab,
            useProtagonistSlot = true
        };

        Collect(spec);
    }
    
    private void EnqueueCastCharacterSpec(
        string slotKey,
        string characterKey,
        string variantKey = "a",
        string emotionKey = "2",
        string positionPreset = "center",
        string scaleArg = "normal")
    {
        var castSpec = new CastCharacterCommandSpec
        {
            slotKey = slotKey,
            characterKey = characterKey
        };

        Collect(castSpec);

        EnqueueSetPortraitPoseSpec(slotKey, variantKey);
        EnqueueSetPortraitFaceSpec(slotKey, emotionKey);
        EnqueueSetAnchorSpecs(slotKey, positionPreset);
        EnqueueSetOriginSizeCommandSpec(slotKey, scaleArg);
    }
    
    private void EnqueueSetAnchorSpecs(string slotKey, string positionPreset, bool resetSlotPos = true, bool resetCharPos = true)
    {
        CharAnchorPreset preset = CharAnchorPresetParser.Parse(positionPreset);

        var anchorSpec = new SetAnchorCommandSpecCharR
        {
            slotKey = slotKey,
            target = CharacterRigTarget.CharSlot_Anchor,
            preset = preset,
            resetSlotPos = resetSlotPos,
            resetCharacterPos =  resetCharPos
        };

        Collect(anchorSpec);
    }
    
    private void EnqueueSetOriginSizeCommandSpec(string roleKey, string scaleArg)
    {
        if (YarnNumberParser.TryParseFloat(scaleArg, out float absoluteScale))
        {
            var absoluteScaleSpec = new SetOriginSizeCommandSpecCharR
            {
                slotKey = roleKey,
                target = CharacterRigTarget.CharSlot_Size,

                overrideScale = true,
                scaleOverride = new Vector3(absoluteScale, absoluteScale, absoluteScale),

                preset = CharScalePreset.None,
            };

            Collect(absoluteScaleSpec);
            return;
        }
        
        if (!CharScalePresetParser.TryParse(scaleArg, out CharScalePreset preset))
            Debug.LogWarning($"[YarnCommandBridge] Unknown scale preset '{scaleArg}'. Fallback to '{CharScalePreset.Normal}' roleKey='{roleKey}'.");
        
        var spec = new SetOriginSizeCommandSpecCharR
        {
            target = CharacterRigTarget.CharSlot_Size,
            slotKey = roleKey, preset = preset,
        };

        Collect(spec);
    }
    
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
    {
        var spec = new SetPortraitPoseCommandSpecCharR
        {
            slotKey = slotKey,
            variantKey = variantKey,
            defaultEmotionKey = PortraitResolver.DefaultEmotion
        };

        Collect(spec);
    }
    
    private void EnqueueSetPortraitFaceSpec(string slotKey, string emotionKey)
    {
        var spec = new SetPortraitSpriteCommandSpecCharR
        {
            slotKey = slotKey,
            portrait = new PortraitIdentity
            {
                character = "",
                variant = "",
                emotion = emotionKey
            }
        };

        Collect(spec);
    }
    
    private void EnqueueSetAnchorOffsetSpecs(string slotKey, int x = 0, int y = 0, float duration = 0.4f)
    {
        var slotOffsetSpec = new MoveByCommandSpecCharR
        {
            slotKey = slotKey,
            target = CharacterRigTarget.CharSlot_Track,
            useAbsolutePosition = false,
            delta = new Vector2(x, y),
            duration = duration
        };
        
        Collect(slotOffsetSpec);
    }
    
    private void EnqueueSizeBySpec(string roleKey, float multiplier, float duration = 0.4f)
    {
        var spec = new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharSlot_Scale,

            toScale = new Vector2(multiplier, multiplier),
            relativeToCurrent = true,

            duration = duration
        };

        Collect(spec);
    }

    private void EnqueueRotateBySpec(string roleKey, float degree, float duration = 0.4f)
    {
        var spec = new RotateToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharSlot_SwayPivot,

            toEuler = new Vector3(0f, 0f, degree),
            relativeToCurrent = true,

            duration = duration
        };

        Collect(spec);
    }
    
    private void EnqueueSetPlaceResetSpecs(string slotKey, float duration = 0.4f)
    {
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
    
    private void EnqueueRotateResetSpec(string roleKey, float duration = 0.4f)
    {
        var spec = new RotateToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharSlot_SwayPivot,

            toEuler = new Vector3(0f, 0f, 0f),

            duration = duration
        };

        Collect(spec);
    }
    
    private void EnqueueSizeResetSpec(string roleKey, float duration = 0.4f)
    {
        var spec = new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharSlot_Scale,

            toScale = new Vector2(1, 1),

            duration = duration
        };

        Collect(spec);
    }

    private void EnqueueCharacterSiblingFrontSpec(string roleKey)
    {
        var spec = new SetCharacterSiblingOrderCommandSpecCharR
        {
            slotKey = roleKey,
            mode = CharacterRigSiblingOrderMode.Front
        };

        Collect(spec);
    }

    private void EnqueueCharacterSiblingBackSpec(string roleKey)
    {
        var spec = new SetCharacterSiblingOrderCommandSpecCharR
        {
            slotKey = roleKey,
            mode = CharacterRigSiblingOrderMode.Back
        };

        Collect(spec);
    }
    
    private void EnqueueMoveCharacterRigToStageLayerSpec(
        string roleKey,
        string stageKey = "stage00",
        string layerKey = "mid")
    {
        var spec = new MoveCharacterRigToStageLayerCommandSpecCharR
        {
            slotKey = roleKey,

            stage = PresentationStageKeyParser.Parse(
                stageKey,
                PresentationStageKey.Stage00),

            layer = PresentationDepthLayerKeyParser.Parse(
                layerKey,
                PresentationDepthLayerKey.Mid),

            siblingMode = CharacterRigReparentSiblingMode.Front
        };

        Collect(spec);
    }

    private void EnqueueMoveCharacterRigToStage00LayerSpec(
        string roleKey,
        string layerKey = "mid")
    {
        EnqueueMoveCharacterRigToStageLayerSpec(
            roleKey,
            "stage00",
            layerKey);
    }

    private void EnqueueMoveCharacterRigToStage01LayerSpec(
        string roleKey,
        string layerKey = "mid")
    {
        EnqueueMoveCharacterRigToStageLayerSpec(
            roleKey,
            "stage01",
            layerKey);
    }

    private void EnqueueMoveCharacterRigToStage02LayerSpec(
        string roleKey,
        string layerKey = "mid")
    {
        EnqueueMoveCharacterRigToStageLayerSpec(
            roleKey,
            "stage02",
            layerKey);
    }
}