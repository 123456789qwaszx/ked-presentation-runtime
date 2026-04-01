using UnityEngine;
using Yarn.Unity;

public sealed class YarnCommandBridge : MonoBehaviour
{
    private DialogueRunner _dialogueRunner;
    private ImmediateCommandRunner _runner;

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
        
        
        

        _dialogueRunner.AddCommandHandler<string, string>("cast", SetPortrait);
    }

    public Coroutine DipInOut(string roleKey, float x, float y)
    {
        var spec = new MoveByCommandSpecCharR
        {
            roleKey = roleKey,
            delta = new Vector2(x, y)
        };

        return _runner.Run(spec);
    }

    public Coroutine MoveBy(string roleKey, float x, float y)
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
        SlideFromCharR from = SlideFromCharR.Left;

        switch (direction?.Trim().ToLowerInvariant())
        {
            case "left":
            case "l":
                from = SlideFromCharR.Left;
                break;

            case "right":
            case "r":
                from = SlideFromCharR.Right;
                break;

            case "up":
            case "u":
            case "top":
                from = SlideFromCharR.Up;
                break;

            case "down":
            case "d":
            case "bottom":
                from = SlideFromCharR.Down;
                break;
        }

        var spec = new BouncySlideInCommandSpecCharR
        {
            roleKey = roleKey,
            from = from
        };

        return _runner.Run(spec);
    }
    
    public Coroutine FadeIn(string roleKey)
    {
        var spec = new FadeInCommandSpecCharR
        {
            roleKey = roleKey
        };
        
        return _runner.Run(spec, blocking: true);
    }
    
    public Coroutine FadeOut(string roleKey)
    {
        var spec = new FadeOutCommandSpecCharR
        {
            roleKey = roleKey
        };
        return _runner.Run(spec);
    }

    public Coroutine SlideIn(string roleKey, string direction = "left")
    {
        SlideFromCharR from = SlideFromCharR.Left;

        switch (direction?.Trim().ToLowerInvariant())
        {
            case "left":
            case "l":
                from = SlideFromCharR.Left;
                break;

            case "right":
            case "r":
                from = SlideFromCharR.Right;
                break;

            case "up":
            case "u":
            case "top":
                from = SlideFromCharR.Up;
                break;

            case "down":
            case "d":
            case "bottom":
                from = SlideFromCharR.Down;
                break;
        }

        var spec = new JuicySlideInCommandSpecCharR
        {
            roleKey = roleKey,
            from = from
        };

        return _runner.Run(spec);
    }

    public Coroutine SlideOut(string roleKey, string direction = "right")
    {
        SlideFromCharR to = SlideFromCharR.Right;

        switch (direction?.Trim().ToLowerInvariant())
        {
            case "left":
            case "l":
                to = SlideFromCharR.Left;
                break;

            case "right":
            case "r":
                to = SlideFromCharR.Right;
                break;

            case "up":
            case "u":
            case "top":
                to = SlideFromCharR.Up;
                break;

            case "down":
            case "d":
            case "bottom":
                to = SlideFromCharR.Down;
                break;
        }

        var spec = new JuicySlideOutCommandSpecCharR
        {
            roleKey = roleKey,
            to = to
        };

        return _runner.Run(spec, blocking: true);
    }

    public GameObject _rigPrefab;
    public void SetCharRig(string roleKey)
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
    
    public CharStageTuningSO globalTuning;

    public void SetAnchorPosition(string roleKey, string positionPreset)
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
    // ──────────────────────────────────────────────────
    // 포트레이트
    // ──────────────────────────────────────────────────

    public void SetOriginSize(string roleKey, float xyValue)
    {
        var spec = new SetOriginSizeCommandSpecCharR
        {
            roleKey = roleKey,
            toScale = new Vector2(xyValue,xyValue)
        };
        
        _runner.Run(spec);
    }

    // ──────────────────────────────────────────────────
    // 포트레이트
    // ──────────────────────────────────────────────────

    public void SetPortrait(string roleKey, string character)
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
}