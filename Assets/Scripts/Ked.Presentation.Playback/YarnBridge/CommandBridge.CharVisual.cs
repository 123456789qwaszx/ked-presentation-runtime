public sealed partial class YarnCommandBridge
{
    private void EnqueueCharVisualPresetSpec(
        string roleKey,
        string presetKey,
        float intensity = 1f,
        string durationToken = "6fr")
        => Collect(new CharVisualFocusCommandSpecCharR 
        { 
            slotKey = roleKey,
            presetKey = CharacterVisualFocusPresetKeyParser.Parse(presetKey),
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueCharFocusSpec(
        string roleKey,
        float intensity = 1f,
        string durationToken = "10fr")
        => Collect(new CharVisualFocusCommandSpecCharR
        {
            presetKey = "focus",
            
            slotKey = roleKey,
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken),
        });

    private void EnqueueCharDefocusSpec(
        string roleKey,
        float intensity = 1f,
        string durationToken = "17fr")
        => Collect(new CharVisualFocusCommandSpecCharR
        {
            presetKey = "defocus",
            
            slotKey = roleKey,
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken),
        });

    private void EnqueueCharDimSpec(
        string roleKey,
        float intensity = 1f,
        string durationToken = "6fr")
        => Collect(new CharVisualFocusCommandSpecCharR
        {
            presetKey = "dim",
            
            slotKey = roleKey,
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken),
        });

    private void EnqueueCharSilhouetteSpec(
        string roleKey,
        float intensity = 1f,
        string durationToken = "6fr")
        => Collect(new CharVisualFocusCommandSpecCharR
        {
            presetKey = "silhouette",
            
            slotKey = roleKey,
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueCharInnerRimSpec(
        string roleKey,
        float intensity = 1f,
        string durationToken = "6fr")
        => Collect(new CharVisualFocusCommandSpecCharR
        {
            presetKey = "inner_rim",
            
            slotKey = roleKey,
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueCharOuterRimSpec(
        string roleKey,
        float intensity = 1f,
        string durationToken = "6fr")
        => Collect(new CharVisualFocusCommandSpecCharR
        {
            presetKey = "outer_rim",
            
            slotKey = roleKey,
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken)
        });
    
    private void EnqueueCharRigVisualClearSpec(
        string roleKey,
        string durationToken = "6fr")
        => Collect(new CharVisualFocusCommandSpecCharR
        {
            presetKey = "clear",
            
            slotKey = roleKey,
            intensity = 1f,
            duration = YarnDurationParser.Parse(durationToken),
        });
}