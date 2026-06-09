using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueFadeInSpec(string roleKey, float duration = 0.45f)
    {
        var spec = new FadeInCommandSpecCharR
        {
            target = CharacterRigTarget.CharacterPortraitSprite_Root,
            slotKey = roleKey,
            duration = duration
        };

        Collect(spec);
    }

    private void EnqueueFadeOutSpec(string roleKey, float duration= 0.45f)
    {
        var spec = new FadeOutCommandSpecCharR
        {
            target = CharacterRigTarget.CharacterPortraitSprite_Root,
            slotKey = roleKey,
            duration = duration
        };

        Collect(spec);
    }
    
    private void EnqueueColorToSpec(string roleKey, float r, float g, float b, float duration = 0.35f)
    {
        var spec = new SetColorCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortraitSprite_Image,
            color = new Color(r, g, b, 1f),
            keepAlpha = true,
            duration = duration
        };

        Collect(spec);
    }
    
    private void EnqueueSlideInSpec(string roleKey, string direction = "left", float duration = 0.4f)
    {
        CharRigDirection from = CharRigDirectionParser.ParseSlideDirection(direction, CharRigDirection.Left);

        var spec = new SlideInCommandSpecCharR
        {
            slotKey = roleKey,
            direction = from,
            duration = duration
        };

        Collect(spec);
    }

    private void EnqueueSlideOutSpec(string roleKey, string direction = "right", float duration = 0.4f)
    {
        CharRigDirection to = CharRigDirectionParser.ParseSlideDirection(direction, CharRigDirection.Right);

        var spec = new SlideOutCommandSpecCharR
        {
            slotKey = roleKey,
            to = to,
            duration = duration
        };

        Collect(spec);
    }
    
    private void EnqueueMoveByCharSpec(string roleKey, float x, float y, float duration = 0.4f)
    {
        var spec = new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_Track,
            delta = new Vector2(x, y),
            duration = duration
        };

        Collect(spec);
    }
    
    private void EnqueueScaleToSpec(string roleKey, float xyValue, float duration = 0.4f)
    {
        var spec = new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_ActingScale,
            duration = duration,
            toScale = new Vector2(xyValue, xyValue)
        };

        Collect(spec);
    }
    
    private void EnqueuePivotRotateToSpec(string roleKey, int angle)
    {
        var spec = new PivotRotateToCommandSpecCharR()
        {
            slotKey = roleKey,
            degree = angle
        };

        Collect(spec);
    }
    
    private void EnqueueSetEmotionPortraitWipeSpec(string targetKey, string emotion)
    {
        var spec = new SetEmotionPortraitWipeCommandSpec
        {
            slotKey = targetKey,
            portrait = new PortraitIdentity
            {
                emotion = emotion,
            }
        };

        Collect(spec);
    }
    
    private void EnqueueSetPortraitCrossfadeSpec(string roleKey, string character)
    {
        var portraitIdentity = new PortraitIdentity
        {
            character = character
        };

        var spec = new SetPortraitCrossfadeCommandSpecCharR
        {
            slotKey = roleKey,
            portrait = portraitIdentity
        };

        Collect(spec);
    }
    
    private void EnqueueFlipHorizontalSpec(string roleKey, int angle, float duration = 0.25f)
    {
        var spec = new RotateToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_Rotation,
            toEuler = new Vector3(0f, angle, 0f),
            duration = duration
        };

        Collect(spec);
    }

    private void EnqueueFlipVerticalSpec(string roleKey, int angle, float duration = 0.25f)
    {
        var spec = new RotateToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_Rotation,
            toEuler = new Vector3(angle, 0f, 0f),
            duration = duration
        };

        Collect(spec);
    }
    
    private void EnqueuePlaceCharacterFocusSpec(
        string roleKey,
        string focus = "face",
        string screenPoint = "center",
        float duration = 0.4f)
    {
        CharacterFocusPreset focusPreset =
            CharacterFocusPresetParser.Parse(focus, CharacterFocusPreset.Face);

        ScreenFocusPoint screen =
            ScreenFocusPointParser.TryParse(screenPoint, out ScreenFocusPoint parsed)
                ? parsed
                : ScreenFocusPoint.Center;

        var spec = new PlaceCharacterFocusCommandSpecCharR
        {
            slotKey = roleKey,
            focusPreset = focusPreset,
            screenPoint = screen,
            moveTarget = CharacterRigTarget.CharSlot_Track,
            duration = duration,
            wait = false,
            killTween = true
        };

        Collect(spec);
    }
    
    private void EnqueueSoloShotSpec(string roleKey, float duration = 0.45f)
    {
        var spec = new SoloShotCommandSpecCharR
        {
            slotKey = roleKey,

            focusPreset = CharacterFocusPreset.Face,
            screenPoint = ScreenFocusPoint.Center,
            screenOffset = new Vector2(0f, 80f),

            moveTarget = CharacterRigTarget.CharSlot_Track,
            scaleTarget = CharacterRigTarget.CharSlot_Scale,
            applyScale = true,
            targetScale = new Vector2(1.08f, 1.08f),

            duration = duration,
            wait = false,
            killTween = true
        };

        Collect(spec);
    }

    private void EnqueueDuoShotSpec(
        string leftRoleKey,
        string rightRoleKey,
        string presetName = "balanced",
        float duration = 0.45f)
    {
        CharacterDuoShotPreset preset =
            CharacterDuoShotPresetParser.Parse(presetName, CharacterDuoShotPreset.Balanced);

        var spec = new DuoShotCommandSpecCharR
        {
            leftRoleKey = leftRoleKey,
            rightRoleKey = rightRoleKey,
            preset = preset,
            moveTarget = CharacterRigTarget.CharSlot_Track,
            scaleTarget = CharacterRigTarget.CharSlot_Scale,
            applyScale = true,
            duration = duration,
            wait = false,
            killTween = true
        };

        Collect(spec);
    }
}