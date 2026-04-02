using UnityEngine;
using Yarn.Unity;

public sealed class YarnCommandBridge : MonoBehaviour
{
    private DialogueRunner _dialogueRunner;
    private ImmediateCommandRunner _runner;
    
    public GameObject _rigPrefab;
    public CharStageTuningSO globalTuning;

    public void Initialize(DialogueRunner dialogueRunner, ImmediateCommandRunner runner)
    {
        _dialogueRunner = dialogueRunner;
        _runner = runner;
        
        _dialogueRunner.AddCommandHandler<string>("slot", SetCharRig);
        _dialogueRunner.AddCommandHandler<string, string>("place", SetAnchorPosition);
        _dialogueRunner.AddCommandHandler<string, float>("scale", SetOriginSize);
        
        
        _dialogueRunner.AddCommandHandler<string, string>("slide_in", SlideIn);
        _dialogueRunner.AddCommandHandler<string, string>("slide_out", SlideOut);
        _dialogueRunner.AddCommandHandler<string, string>("slide_in_bouncy", BouncySlideIn);

        _dialogueRunner.AddCommandHandler<string>("fade_in", FadeIn);
        _dialogueRunner.AddCommandHandler<string>("fade_out", FadeOut);
        
        _dialogueRunner.AddCommandHandler<string, float, float>("move_by", MoveBy);
        _dialogueRunner.AddCommandHandler<string, string>("dip", DipInOut);
        
        _dialogueRunner.AddCommandHandler<string, string>("hop_in", HopIn);
        
        _dialogueRunner.AddCommandHandler<string, string>("jolt", NudgeJolt);
        _dialogueRunner.AddCommandHandler<string, string>("shake", NudgeShake);
        _dialogueRunner.AddCommandHandler<string, string>("nudge", NudgeTap);
        _dialogueRunner.AddCommandHandler<string, string>("nudge_hard", NudgeTapHard);

        

        _dialogueRunner.AddCommandHandler<string, string>("cast", SetPortrait);
    }
    
    private Coroutine NudgeJolt(string roleKey, string direction = "right")
    {
        SlideFromCharR dir = ParseSlideDirection(direction, SlideFromCharR.Right);
        
        var spec = new NudgeTapCommandSpecCharR
        {
            roleKey = roleKey,
            target = CharacterRigTarget.Character_Track_Y,
            direction = dir,
            strength = 340f,
            duration = 0.6f,
            taps = 3,
            damping = 8,
            anticipation = -12,
            wait = true
        };

        return _runner.Run(spec);
    }
    
    private Coroutine NudgeShake(string roleKey, string direction = "right")
    {
        SlideFromCharR dir = ParseSlideDirection(direction, SlideFromCharR.Right);
        
        var spec = new NudgeTapCommandSpecCharR
        {
            roleKey = roleKey,
            direction = dir,
            strength = 44f,
            duration = 1.2f,
            taps = 4
        };

        return _runner.Run(spec);
    }
    
    private Coroutine NudgeTap(string roleKey, string direction = "right")
    {
        SlideFromCharR dir = ParseSlideDirection(direction, SlideFromCharR.Right);
        
        var spec = new NudgeTapCommandSpecCharR
        {
            roleKey = roleKey,
            target = CharacterRigTarget.Character_Track,
            direction = dir,
            strength = 340f,
            duration = 0.6f,
            taps = 1,
            damping = 9,
            anticipation = -12
        };

        return _runner.Run(spec);
    }
    
    private Coroutine NudgeTapHard(string roleKey, string direction = "down")
    {
        SlideFromCharR dir = ParseSlideDirection(direction, SlideFromCharR.Down);
        
        var spec = new NudgeTapCommandSpecCharR
        {
            roleKey = roleKey,
            direction = dir,
            strength = 1400f,
            duration = 0.7f,
            taps = 1,
            damping = 9,
            anticipation = 4
        };

        return _runner.Run(spec);
    }
    
    private Coroutine HopIn(string roleKey, string direction = "left")
    {
        SlideFromCharR dir = ParseSlideDirection(direction, SlideFromCharR.Down);

        var spec = new BounceArcInCommandSpecCharR
        {
            roleKey = roleKey,
            from = dir
        };

        return _runner.Run(spec);
    }
    

    private Coroutine DipInOut(string roleKey, string direction = "down")
    {
        SlideFromCharR dir = ParseSlideDirection(direction, SlideFromCharR.Down);

        var spec = new DipInOutCommandSpecCharR
        {
            roleKey = roleKey,
            dir = dir
        };

        return _runner.Run(spec);
    }

    private Coroutine MoveBy(string roleKey, float x, float y)
    {
        var spec = new MoveByCommandSpecCharR
        {
            roleKey = roleKey,
            delta = new Vector2(x, y)
        };

        return _runner.Run(spec);
    }
    
    private Coroutine BouncySlideIn(string roleKey, string direction = "left")
    {
        SlideFromCharR from = ParseSlideDirection(direction, SlideFromCharR.Left);

        var spec = new BouncySlideInCommandSpecCharR
        {
            roleKey = roleKey,
            from = from
        };

        return _runner.Run(spec);
    }
    
    private Coroutine FadeIn(string roleKey)
    {
        var spec = new FadeInCommandSpecCharR
        {
            roleKey = roleKey
        };
        
        return _runner.Run(spec, blocking: true);
    }
    
    private Coroutine FadeOut(string roleKey)
    {
        var spec = new FadeOutCommandSpecCharR
        {
            roleKey = roleKey
        };
        return _runner.Run(spec);
    }

    private Coroutine SlideIn(string roleKey, string direction = "left")
    {
        SlideFromCharR from = ParseSlideDirection(direction, SlideFromCharR.Left);

        var spec = new JuicySlideInCommandSpecCharR
        {
            roleKey = roleKey,
            direction = from
        };

        return _runner.Run(spec);
    }

    private Coroutine SlideOut(string roleKey, string direction = "right")
    {
        SlideFromCharR to = ParseSlideDirection(direction, SlideFromCharR.Right);

        var spec = new JuicySlideOutCommandSpecCharR
        {
            roleKey = roleKey,
            to = to
        };

        return _runner.Run(spec, blocking: true);
    }

    private void SetCharRig(string roleKey)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogError("[YarnCommandBridge] char_rig: roleKey is null or empty.");
            return;
        }

        var spec = new SetCharRigCommandSpec
        {
            roleKey = roleKey,
            rigPrefab = _rigPrefab
        };

        _runner.Run(spec);
    }
    
    private void SetAnchorPosition(string roleKey, string positionPreset)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogError("[YarnCommandBridge] anchor: roleKey is null or empty.");
            return;
        }

        RectAnchorPreset3CharR preset = positionPreset switch
        {
            "left" => RectAnchorPreset3CharR.Left,
            "center" => RectAnchorPreset3CharR.Center,
            "right" => RectAnchorPreset3CharR.Right,
            _ => RectAnchorPreset3CharR.Center
        };

        var spec = new SetAnchorCommandSpecCharR
        {
            roleKey = roleKey,
            preset = preset,
            globalTuning = globalTuning
        };
        
        var spec2 = new SetPosOffsetCommandSpecCharR{
            roleKey = roleKey,
        };

        _runner.Run(spec);
        _runner.Run(spec2);
    }

    private void SetOriginSize(string roleKey, float xyValue)
    {
        var spec = new SetOriginSizeCommandSpecCharR
        {
            roleKey = roleKey,
            toScale = new Vector2(xyValue,xyValue)
        };
        
        _runner.Run(spec);
    }


    private void SetPortrait(string roleKey, string character)
    {
        var portraitIdentity = new PortraitIdentity
        {
            character = character,
            variant = "a",
            emotion = "1"
        };
        
        var spec = new SetPortraitSpriteCommandSpecCharR
        {
            roleKey = roleKey,
            portrait = portraitIdentity
        };
        
        _runner.Run(spec);
    }
    
    
    private SlideFromCharR ParseSlideDirection(string direction, SlideFromCharR fallback)
    {
        switch (direction?.Trim().ToLowerInvariant())
        {
            case "left":
            case "l":
                return SlideFromCharR.Left;

            case "right":
            case "r":
                return SlideFromCharR.Right;

            case "up":
            case "u":
            case "top":
                return SlideFromCharR.Up;

            case "down":
            case "d":
            case "bottom":
                return SlideFromCharR.Down;

            default:
                return fallback;
        }
    }
}