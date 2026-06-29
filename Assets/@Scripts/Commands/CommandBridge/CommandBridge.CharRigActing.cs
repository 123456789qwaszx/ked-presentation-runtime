public sealed partial class YarnCommandBridge
{
    private void EnqueueDipInOutSpec(
        string roleKey,
        string direction = "down")
        => Collect(new DipInOutCommandSpecCharR
        {
            target = CharacterRigTarget.CharacterPortrait_Track_Move_Y,
            
            slotKey = roleKey,
            dir = CharRigDirectionParser.ParseSlideDirection(direction)
        });

    private void EnqueueHopSpec(
        string roleKey)
        => Collect(new HopCommandSpecCharR
        {
            target = CharacterRigTarget.CharacterPortrait_Track_Move_Y,
            
            slotKey = roleKey,
            
            hopCount = 1,
            height = 22,
            airWidth = 0.88f,
            duration = 0.6f
        });

    private void EnqueueShakeJoltSpec(
        string roleKey, 
        string direction = "right")
        => Collect(new JoltCommandSpec
        {
            target = CharacterRigTarget.CharacterPortrait_Shake,
            
            direction = CharRigDirectionParser.ParseSlideDirection(direction),
            slotKey = roleKey,
            
            strength = 44f,
            taps = 4,
            duration = 1.2f
        });

    private void EnqueueTrembleSpec(
        string roleKey,
        string direction = "right")
        => Collect(new TrembleCommandSpecCharR
        {
            target = CharacterRigTarget.CharacterPortrait_Shake,
            
            slotKey = roleKey,
            direction = CharRigDirectionParser.ParseSlideDirection(direction),
            
            strength = 8f,
            frequency = 24f,
            crossAxisRatio = 0.35f,
            noiseRatio = 0.25f,
            blendIn = 0.04f,
            blendOut = 0.08f,
            duration = 1.2f
        });

    private void EnqueueSwaySpec(
        string roleKey)
        => Collect(new SwayCommandSpecCharR
        {
            target = CharacterRigTarget.CharacterPortrait_SwayPivot,
            
            slotKey = roleKey,

            strength = 12f,
            cycles = 2,
            damping = 1.9f,
            speed = 1.2f,
            anticipation = 0.45f,
            duration = 1.15f,
        });
}