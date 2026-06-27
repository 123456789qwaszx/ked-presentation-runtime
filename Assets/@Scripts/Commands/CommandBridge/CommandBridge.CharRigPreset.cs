public sealed partial class YarnCommandBridge
{
    private void EnqueueJoltSpec(string roleKey, string direction = "right")
    {
        CharRigDirection dir = CharRigDirectionParser.ParseSlideDirection(direction, CharRigDirection.Right);

        var spec = new JoltCommandSpec
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_Track_Y,
            direction = dir,
            strength = 340f,
            duration = 0.6f,
            taps = 3,
            damping = 8,
            anticipation = -12
        };

        Collect(spec);
    }
    
    private void EnqueueJoltSpecTap(string roleKey, string direction = "right")
    {
        CharRigDirection dir = CharRigDirectionParser.ParseSlideDirection(direction, CharRigDirection.Right);

        var spec = new JoltCommandSpec
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_Track,
            direction = dir,
            strength = 340f,
            duration = 0.6f,
            taps = 1,
            damping = 9,
            anticipation = -12
        };

        Collect(spec);
    }
    
    private void EnqueueJoltSpecTapHard(string roleKey, string direction = "down")
    {
        CharRigDirection dir = CharRigDirectionParser.ParseSlideDirection(direction, CharRigDirection.Down);

        var spec = new JoltCommandSpec
        {
            slotKey = roleKey,
            direction = dir,
            strength = 1400f,
            duration = 0.7f,
            taps = 1,
            damping = 9,
            anticipation = 4
        };

        Collect(spec);
    }
    
    private void EnqueueSlideInSwayCombo(string roleKey)
    {
        var spec = new FadeInCommandSpecCharR()
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortraitSprite_Root,
            duration = -1f
        };

        var spec1 = new FadeInCommandSpecCharR()
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortraitSprite_Root,
            duration = 0.28f
        };

        var spec2 = new SlideInCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharSlot_Track,
            distance = 550f,
            duration = 0.45f
        };
        var spec3 = new DipInOutCommandSpecCharR()
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharSlot_Track_X,
            dir = CharRigDirection.Right,
            distance = 22f,
            duration = 0.8f
        };

        var spec4 = new PunchScaleCommandSpecCharR()
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharSlot_Scale,
            strength = -0.03f,
            duration = 0.55f,
            vibrato = 3,
            elasticity = 0.45f
        };

        var spec5 = new DipInOutCommandSpecCharR()
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharSlot_Track_Y,
            dir = CharRigDirection.Down,
            distance = 12f,
            duration = 0.8f
        };

        var spec6 = new SwayCommandSpecCharR()
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharSlot_SwayPivot,
            strength = 11.5f,
            duration = 1.28f,
            cycles = 1,
            damping = 26,
            speed = 1.8f,
            finalOvershoot = 0.8f,
            anticipation = -15,
            startPositive = false
        };

        var spec7 = new WaitCommandSpec()
        {
            duration = 0.4f,
        };
        
        var spec8 = new JoltCommandSpec()
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_Track,
            strength = 45f,
            direction = CharRigDirection.Up,
            duration = 0.55f,
            taps = 3,
            damping = 11,
            anticipation = 3
        };

        Collect(spec);
        Collect(spec1);
        Collect(spec2);
        Collect(spec3);
        Collect(spec4);
        Collect(spec5);
        Collect(spec6);
        Collect(spec7);
        Collect(spec8);
    }
    
    private void EnqueueSlideInJoltCombo(string roleKey, string direction = "right")
    {
        CharRigDirection dir = CharRigDirectionParser.ParseSlideDirection(direction, CharRigDirection.Right);

        var juicySlideIn = new SlideInCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_Track_X,
            direction = dir
        };

        var spec = new JoltCommandSpec
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_Track_Y,
            direction = CharRigDirection.Up,
            strength = 340f,
            duration = 0.6f,
            taps = 4,
            damping = 9,
            anticipation = -12
        };

        Collect(spec);
        Collect(juicySlideIn);
    }
    
    private void EnqueueSwaySpecPendulum(string roleKey)
    {
        var spec = new SwayCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_SwayPivot,

            strength = 13f,
            duration = 1.35f,
            cycles = 2,
            damping = 4.2f,
            speed = 0.88f,
            anticipation = 0.02f,
            wait = false
        };

        Collect(spec);
    }
    
    private void EnqueueSwaySpecFast(string roleKey)
    {
        var spec = new SwayCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_SwayPivot,

            strength = 6.5f,
            duration = 0.94f,
            cycles = 3,
            damping = 2.8f,
            speed = 1.26f,
            finalOvershoot = 0.4f,
            anticipation = -0.5f,
        };

        Collect(spec);
    }
    
    private void EnqueueSwaySpecAway(string roleKey)
    {
        var spec = new SwayCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_SwayPivot,

            strength = 15f,
            duration = 0.74f,
            cycles = 1,
            damping = 5f,
            speed = 1.2f,
            finalOvershoot = 0.2f,
            anticipation = -0.5f
        };

        Collect(spec);
    }
}