using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueFadeInSpec(string roleKey, float duration = 0.45f)
    {
        var spec = new FadeInCommandSpecCharR
        {
            slotKey = roleKey,
            duration = duration
        };

        Collect(spec);
    }

    private void EnqueueFadeOutSpec(string roleKey, float duration= 0.45f)
    {
        var spec = new FadeOutCommandSpecCharR
        {
            slotKey = roleKey,
            duration = duration
        };

        Collect(spec);
    }
    
    private void EnqueueSlideInSpec(string roleKey, string direction = "left")
    {
        CharRigDirection from = CharRigDirectionParser.ParseSlideDirection(direction, CharRigDirection.Left);

        var spec = new SlideInCommandSpecCharR
        {
            slotKey = roleKey,
            direction = from
        };

        Collect(spec);
    }

    private void EnqueueSlideOutSpec(string roleKey, string direction = "right")
    {
        CharRigDirection to = CharRigDirectionParser.ParseSlideDirection(direction, CharRigDirection.Right);

        var spec = new SlideOutCommandSpecCharR
        {
            slotKey = roleKey,
            to = to
        };

        Collect(spec);
    }
    
    private void EnqueueMoveByCharSpec(string roleKey, float x, float y, float duration = 1.2f)
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
                emotion = emotion
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
}