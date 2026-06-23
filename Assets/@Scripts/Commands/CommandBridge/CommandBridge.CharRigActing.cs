public sealed partial class YarnCommandBridge
{
    private void EnqueueDipInOutSpec(string roleKey, 
        string direction = "down")
    {
        CharRigDirection dir = CharRigDirectionParser.ParseSlideDirection(direction, CharRigDirection.Down);

        var spec = new DipInOutCommandSpecCharR
        {
            slotKey = roleKey,
            dir = dir
        };

        Collect(spec);
    }
    
    private void EnqueueHopSpec(string roleKey,
        int hopCount = 2,
        float height = 48f,
        float airWidth = 0.85f,
        float duration = 0.4f)
    {
        var spec = new HopCommandSpecCharR
        {
            slotKey = roleKey,
            hopCount = hopCount,
            height = height,
            airWidth = airWidth,
            duration = duration
        };

        Collect(spec);
    }
    
    private void EnqueueJoltSpecShake(string roleKey,
        string direction = "right",
        float strength = 44f,
        float duration = 1.2f,
        int taps = 4)
    {
        CharRigDirection dir = CharRigDirectionParser.ParseSlideDirection(direction, CharRigDirection.Right);

        var spec = new JoltCommandSpec
        {
            target = CharacterRigTarget.CharacterPortrait_Shake,
            slotKey = roleKey,
            direction = dir,
            strength = strength,
            duration = duration,
            taps = taps
        };

        Collect(spec);
    }
    
    private void EnqueueTrembleSpec(string roleKey,
        float duration = 1.2f,
        float strength = 8f,
        float frequency = 24f,
        string direction = "right")
    {
        CharRigDirection dir = CharRigDirectionParser.ParseSlideDirection(direction, CharRigDirection.Right);

        var spec = new TrembleCommandSpecCharR
        {
            slotKey = roleKey.Trim(),
            target = CharacterRigTarget.CharacterPortrait_Shake,
            direction = dir,
            duration = duration,
            strength = strength,
            frequency = frequency,
            crossAxisRatio = 0.35f,
            noiseRatio = 0.25f,
            blendIn = 0.04f,
            blendOut = 0.08f,
            wait = false
        };

        Collect(spec);
    }
    
    private void EnqueueSwaySpec(string roleKey)
    {
        var spec = new SwayCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_SwayPivot,

            strength = 12f,
            duration = 1.15f,
            cycles = 2,
            damping = 1.9f,
            speed = 1.2f,
            anticipation = 0.45f,
            wait = false
        };

        Collect(spec);
    }
}
