using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueCharFocusSpec(
        string roleKey,
        float intensity = 1f,
        float duration = 0.4f)
    {
        var spec = new CharVisualFocusCommandSpecCharR
        {
            slotKey = roleKey.Trim(),
            mode = CharacterVisualFocusMode.Focus,
            intensity = intensity,
            duration = duration,
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueCharDefocusSpec(
        string roleKey,
        float intensity = 1f,
        float blur = 0.5f,
        float duration = 0.7f)
    {
        var spec = new CharVisualFocusCommandSpecCharR
        {
            slotKey = roleKey.Trim(),
            mode = CharacterVisualFocusMode.Defocus,
            intensity = intensity,
            defocusBlurAmount = blur,
            duration = duration,
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueCharClearFocusSpec(
        string roleKey,
        float duration = 0.25f)
    {
        var spec = new CharVisualFocusCommandSpecCharR
        {
            slotKey = roleKey.Trim(),
            mode = CharacterVisualFocusMode.Clear,
            duration = duration,
            wait = false
        };

        Collect(spec);
    }

    // Existing compatibility:
    // char_visual role dim rim blur duration
    // Here "rim" maps to Outer Rim.
    private void EnqueueCharVisualSpec(
        string roleKey,
        float dim,
        float rim,
        float blur,
        float duration = 0.25f)
    {
        var spec = new CharVisualFocusCommandSpecCharR
        {
            slotKey = roleKey.Trim(),
            mode = CharacterVisualFocusMode.Custom,
            dim = dim,
            rim = rim,
            innerRim = 0f,
            blur = blur,
            rimColor = Color.white,
            innerRimColor = new Color(1f, 0.96f, 0.86f, 1f),
            duration = duration,
            wait = false
        };

        Collect(spec);
    }

    // Existing compatibility:
    // char_visual_color role dim rim blur r g b duration
    // Here color maps to Outer Rim color.
    private void EnqueueCharVisualRimColorSpec(
        string roleKey,
        float dim,
        float rim,
        float blur,
        float r,
        float g,
        float b,
        float duration = 0.25f)
    {
        var spec = new CharVisualFocusCommandSpecCharR
        {
            slotKey = roleKey.Trim(),
            mode = CharacterVisualFocusMode.Custom,
            dim = dim,
            rim = rim,
            innerRim = 0f,
            blur = blur,
            rimColor = new Color(r, g, b, 1f),
            innerRimColor = new Color(1f, 0.96f, 0.86f, 1f),
            duration = duration,
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueCharDimSpec(
        string roleKey,
        float dim,
        float duration = 0.25f)
    {
        var spec = new CharVisualFocusCommandSpecCharR
        {
            slotKey = roleKey.Trim(),
            mode = CharacterVisualFocusMode.Custom,
            dim = dim,
            dimTintColor = new Color(0.45f, 0.48f, 0.55f, 1f),
            rim = 0f,
            innerRim = 0f,
            blur = 0f,
            rimColor = Color.white,
            innerRimColor = new Color(1f, 0.96f, 0.86f, 1f),
            duration = duration,
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueCharInnerRimSpec(
        string roleKey,
        float amount,
        float duration = 0.25f)
    {
        var spec = new CharVisualFocusCommandSpecCharR
        {
            slotKey = roleKey.Trim(),
            mode = CharacterVisualFocusMode.Custom,
            dim = 0f,
            rim = 0f,
            innerRim = amount,
            blur = 0f,
            rimColor = Color.white,
            innerRimColor = new Color(1f, 0.96f, 0.86f, 1f),
            duration = duration,
            wait = false
        };

        Collect(spec);
    }
    
    private void EnqueueCharSilhouetteSpec(
        string roleKey,
        float dim = 1f,
        float duration = 0.25f)
    {
        var spec = new CharVisualFocusCommandSpecCharR
        {
            slotKey = roleKey.Trim(),
            mode = CharacterVisualFocusMode.Custom,

            // 실루엣 전용.
            // _DimTintColor를 black으로 바꿔 정보량을 강하게 줄인다.
            dim = dim,
            dimTintColor = Color.black,

            // 실루엣에서는 rim 계열을 끈다.
            rim = 0f,
            innerRim = 0f,

            // 형태는 남기되 디테일만 살짝 뭉개는 정도.
            blur = 0.08f,

            rimColor = Color.white,
            innerRimColor = new Color(1f, 0.96f, 0.86f, 1f),

            duration = duration,
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueCharOuterRimSpec(
        string roleKey,
        float amount,
        float duration = 0.25f)
    {
        var spec = new CharVisualFocusCommandSpecCharR
        {
            slotKey = roleKey.Trim(),
            mode = CharacterVisualFocusMode.Custom,
            dim = 0f,
            rim = amount,
            innerRim = 0f,
            blur = 0f,
            rimColor = Color.white,
            innerRimColor = new Color(1f, 0.96f, 0.86f, 1f),
            duration = duration,
            wait = false
        };

        Collect(spec);
    }
}