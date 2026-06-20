using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueSetupCharRigSpec(string slotKey, string parentKey = "0")
    {
        var spec = new SetupCharRigCommandSpec
        {
            roleKey = slotKey,
            rigPrefab =_charRigPrefab
        };
        
        if (CharRigSlotParser.TryParse(parentKey, out CharRigSlot parentSlot))
            spec.parentSlot = parentSlot;
        
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
}