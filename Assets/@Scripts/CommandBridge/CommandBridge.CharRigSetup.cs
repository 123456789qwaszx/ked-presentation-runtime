using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueSetupCharRigSpec(string slotKey, string parentKey)
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
    
    private void EnqueueCastCharacterSpec(string slotKey, string characterKey, 
        string variantKey = "a", string emotionKey = "02",
        bool applySetup = true,
        string positionPreset = "center",
        string scaleArg = "normal"
        )
    {
        var castSpec = new CastCharacterCommandSpec
        {
            slotKey = slotKey,
            characterKey = characterKey,
            variantKey = variantKey
        };
        
        Collect(castSpec);

        EnqueueSetPortraitSpriteSpec(slotKey, characterKey, variantKey, emotionKey);
        
        if (!applySetup)
            return;

        EnqueueSetAnchorSpecs(slotKey, positionPreset);
        EnqueueSetOriginSizeSpec(slotKey, scaleArg);
        EnqueueFadeInSpec(slotKey);
    }
    
    private void EnqueueSetPortraitSpriteSpec(string slotKey, 
        string characterKey, string variantKey, string emotionKey)
    {
        var spec = new SetPortraitSpriteCommandSpecCharR
        {
            slotKey = slotKey,
            portrait = new PortraitIdentity
            {
                character = characterKey,
                variant = variantKey,
                emotion = emotionKey
            }
        };

        Collect(spec);
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
    
    private void EnqueueSetOriginSizeSpec(string roleKey, string scaleArg)
    {
        if (YarnNumberParser.TryParseFloat(scaleArg, out float absoluteScale))
        {
            var absoluteScaleSpec = new SetOriginSizeCommandSpecCharR
            {
                slotKey = roleKey,

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
            slotKey = roleKey, preset = preset,
        };

        Collect(spec);
    }
    
    private void EnqueueSetAnchorOffsetSpecs(string slotKey, int x = 0, int y = 0)
    {
        var slotOffsetSpec = new MoveByCommandSpecCharR
        {
            slotKey = slotKey,
            target = CharacterRigTarget.CharSlot_Anchor,
            delta = new Vector2(x, y),
            duration = 0f
        };
        
        Collect(slotOffsetSpec);
    }
}