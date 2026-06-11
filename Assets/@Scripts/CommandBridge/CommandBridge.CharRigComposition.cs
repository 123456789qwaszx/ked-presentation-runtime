using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueuePlaceCharacterFocusSpec(
        string roleKey,
        string focus = "face",
        string screenPoint = "center",
        float duration = 0.4f)
    {
        CharacterFocusPreset focusPreset = CharacterFocusPresetParser.Parse(focus, CharacterFocusPreset.Face);

        ScreenFocusPoint screen = 
            ScreenFocusPointParser.TryParse(screenPoint, out ScreenFocusPoint parsed) 
            ? parsed 
            : ScreenFocusPoint.Center;

        var spec = new PlaceCharacterFocusCommandSpecCharR
        {
            slotKey = roleKey,
            focusPreset = focusPreset,
            screenPoint = screen,
            moveTarget = CharacterRigTarget.CharSlot_Track_Focus,
            duration = duration,
            wait = false
        };

        Collect(spec);
    }
    
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

            dim = dim,
            dimTintColor = Color.black,

            rim = 0f,
            innerRim = 0f,

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