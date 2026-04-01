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
        _dialogueRunner.AddCommandHandler<string, string>("dip_inout", DipInOut);
        
        
        _dialogueRunner.AddCommandHandler<string, string>("hop_in", HopIn);
        
        _dialogueRunner.AddCommandHandler<string>("t1", test1);
        _dialogueRunner.AddCommandHandler<string>("t2", test2);
        _dialogueRunner.AddCommandHandler<string>("t3", test3);
        _dialogueRunner.AddCommandHandler<string>("t4", test4);
        
        

        _dialogueRunner.AddCommandHandler<string, string>("cast", SetPortrait);
    }
    
    private Coroutine test1(string roleKey)
    {

        var spec = new MoveInOutCommandSpecCharR
        {
            roleKey = roleKey,
        };

        return _runner.Run(spec);
    }
    private Coroutine test2(string roleKey)
    {

        var spec = new NudgeTapCommandSpecCharR
        {
            roleKey = roleKey,
        };

        return _runner.Run(spec);
    }
    private Coroutine test3(string roleKey)
    {

        var spec = new RichSlideInCommandSpecCharR
        {
            roleKey = roleKey,
        };

        return _runner.Run(spec);
    }
    private Coroutine test4(string roleKey)
    {

        var spec = new TapEaseCommandSpecCharR
        {
            roleKey = roleKey,
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
            from = from
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