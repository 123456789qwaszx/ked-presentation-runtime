using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public sealed class YarnCommandBridge : MonoBehaviour
{
    private DialogueRunner _dialogueRunner;

    [Header("Rig")] public GameObject rigPrefab;
    [Header("Global Tuning")] public CharStageTuningSO globalTuning;

    private int _pendingImmediateWaitCount;
    private readonly List<CommandSpecBase> _collectedSpecs = new();
    
    #region collected specs

    private void WaitNextImmediateCommands(int count = 1) => _pendingImmediateWaitCount = Mathf.Max(0, count);
    public void ResetImmediateWaitForNewLine() => _pendingImmediateWaitCount = 0;
    
    public List<CommandSpecBase> ConsumeCollectedSpecs()
    {
        var result = new List<CommandSpecBase>(_collectedSpecs);
        _collectedSpecs.Clear();
        return result;
    }

    public void ClearCollectedSpecs()
    {
        _collectedSpecs.Clear();
    }

    private void ApplyImmediateWait(CommandSpecBase spec)
    {
        if (spec == null)
            return;

        bool shouldWait = _pendingImmediateWaitCount > 0;

        switch (spec)
        {
            case NudgeTapCommandSpecCharR nudgeTap:
                nudgeTap.wait = shouldWait;
                break;

            case BounceArcInCommandSpecCharR bounceArcIn:
                bounceArcIn.wait = shouldWait;
                break;

            case DipInOutCommandSpecCharR dipInOut:
                dipInOut.wait = shouldWait;
                break;

            case MoveByCommandSpecCharR moveBy:
                moveBy.wait = shouldWait;
                break;

            case BouncySlideInCommandSpecCharR bouncySlideIn:
                bouncySlideIn.wait = shouldWait;
                break;

            case FadeInCommandSpecCharR fadeIn:
                fadeIn.wait = shouldWait;
                break;

            case FadeOutCommandSpecCharR fadeOut:
                fadeOut.wait = shouldWait;
                break;

            case JuicySlideInCommandSpecCharR slideIn:
                slideIn.wait = shouldWait;
                break;

            case JuicySlideOutCommandSpecCharR slideOut:
                slideOut.wait = shouldWait;
                break;
        }

        if (shouldWait)
            _pendingImmediateWaitCount--;
    }

    private void Collect(CommandSpecBase spec)
    {
        if (spec == null) return;

        ApplyImmediateWait(spec);
        _collectedSpecs.Add(spec);
    }
    
    private void NudgeSlideIn(string roleKey, string direction = "right")
    {
        SlideFromCharR dir = ParseSlideDirection(direction, SlideFromCharR.Right);

        var juicySlideIn = new JuicySlideInCommandSpecCharR
        {
            roleKey = roleKey,
            target = CharacterRigTarget.Character_Track_X,
            direction = dir
        };

        var spec = new NudgeTapCommandSpecCharR
        {
            roleKey = roleKey,
            target = CharacterRigTarget.Character_Track_Y,
            direction = SlideFromCharR.Up,
            strength = 340f,
            duration = 0.6f,
            taps = 4,
            damping = 9,
            anticipation = -12
        };

        Collect(spec);
        Collect(juicySlideIn);
    }

    private void NudgeJolt(string roleKey, string direction = "right")
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
            anticipation = -12
        };

        Collect(spec);
    }
    
    #endregion
    
    public void RegisterYarnCommands(DialogueRunner dialogueRunner)
    {
        _dialogueRunner = dialogueRunner;

        _dialogueRunner.AddCommandHandler<string>("destroy", DestroyCommand);
        _dialogueRunner.AddCommandHandler<int>("await_for", WaitNextImmediateCommands);
        
        _dialogueRunner.AddCommandHandler<string>("slot_boxside", SetSpeakerSlot);
        _dialogueRunner.AddCommandHandler<string>("slot", SetCharSlot);
        _dialogueRunner.AddCommandHandler<string, string>("place", SetAnchorPosition);
        _dialogueRunner.AddCommandHandler<string, int, int>("place_offset", SetAnchorOffset);
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
        
        _dialogueRunner.AddCommandHandler<string, string>("slide_in_nudge", NudgeSlideIn);

        _dialogueRunner.AddCommandHandler<string, string>("cast", SetPortrait);
        
        
        _dialogueRunner.AddCommandHandler("blackout", ScreedBlackout);
        _dialogueRunner.AddCommandHandler<string>("uipatch", UIPatch);
    }
    
    private void UIPatch(string themeId)
    {
        var spec = new UIPatchCommandSpec()
        {
            themeId = themeId,
        };

        Collect(spec);
    }
    
    private void ScreedBlackout()
    {
        var spec = new TransitionCommandSpec
        {
            targetKind = TransitionTargetKind.Blackout
        };

        Collect(spec);
    }
    
    private void DestroyCommand(string roleKey)
    {
        var spec = new DestroyCommandSpec
        {
            roleKey = roleKey
        };

        Collect(spec);
    }
    
    private void NudgeShake(string roleKey, string direction = "right")
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

        Collect(spec);
    }

    private void NudgeTap(string roleKey, string direction = "right")
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

        Collect(spec);
    }

    private void NudgeTapHard(string roleKey, string direction = "down")
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

        Collect(spec);
    }

    private void HopIn(string roleKey, string direction = "left")
    {
        SlideFromCharR dir = ParseSlideDirection(direction, SlideFromCharR.Down);

        var spec = new BounceArcInCommandSpecCharR
        {
            roleKey = roleKey,
            from = dir
        };

        Collect(spec);
    }

    private void DipInOut(string roleKey, string direction = "down")
    {
        SlideFromCharR dir = ParseSlideDirection(direction, SlideFromCharR.Down);

        var spec = new DipInOutCommandSpecCharR
        {
            roleKey = roleKey,
            dir = dir
        };

        Collect(spec);
    }

    private void MoveBy(string roleKey, float x, float y)
    {
        var spec = new MoveByCommandSpecCharR
        {
            roleKey = roleKey,
            delta = new Vector2(x, y)
        };

        Collect(spec);
    }

    private void BouncySlideIn(string roleKey, string direction = "left")
    {
        SlideFromCharR from = ParseSlideDirection(direction, SlideFromCharR.Left);

        var spec = new BouncySlideInCommandSpecCharR
        {
            roleKey = roleKey,
            from = from
        };

        Collect(spec);
    }

    private void FadeIn(string roleKey)
    {
        var spec = new FadeInCommandSpecCharR
        {
            roleKey = roleKey
        };

        Collect(spec);
    }

    private void FadeOut(string roleKey)
    {
        var spec = new FadeOutCommandSpecCharR
        {
            roleKey = roleKey
        };

        Collect(spec);
    }

    private void SlideIn(string roleKey, string direction = "left")
    {
        SlideFromCharR from = ParseSlideDirection(direction, SlideFromCharR.Left);

        var spec = new JuicySlideInCommandSpecCharR
        {
            roleKey = roleKey,
            direction = from
        };

        Collect(spec);
    }

    private void SlideOut(string roleKey, string direction = "right")
    {
        SlideFromCharR to = ParseSlideDirection(direction, SlideFromCharR.Right);

        var spec = new JuicySlideOutCommandSpecCharR
        {
            roleKey = roleKey,
            to = to
        };

        Collect(spec);
    }

    private void SetCharSlot(string roleKey)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogError("[YarnCommandBridge] slot: roleKey is null or empty.");
            return;
        }

        var spec = new SetCharRigCommandSpec
        {
            roleKey = roleKey,
            rigPrefab = rigPrefab
        };

        Collect(spec);
    }
    
    private void SetSpeakerSlot(string roleKey)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogError("[YarnCommandBridge] slot: roleKey is null or empty.");
            return;
        }

        var spec = new SetCharRigCommandSpec
        {
            roleKey = roleKey,
            parentSlot = CharRigSlot.ProtagonistSlot,
            rigPrefab = rigPrefab
        };

        Collect(spec);
    }
    
    private void SetAnchorOffset(string roleKey, int x, int y)
    {
        var anchorSpec = new MoveByCommandSpecCharR
        {
            roleKey = roleKey,
            target = CharacterRigTarget.Character_Anchor,
            delta = new Vector2(x, y),
            duration = 0f,
            killTween = false
        };
        
        var resetTrackSpec = new ResetTrackOffsetsCommandSpec { roleKey = roleKey };

        Collect(anchorSpec);
        Collect(resetTrackSpec);
    }

    private void SetAnchorPosition(string roleKey, string positionPreset)
    {
        CharAnchorPreset preset = positionPreset switch
        {
            "left" => CharAnchorPreset.Left,
            "center" => CharAnchorPreset.Center,
            "right" => CharAnchorPreset.Right,
            "boxside" => CharAnchorPreset.BoxSide,
            
            "exp1" => CharAnchorPreset.Exp1,
            "exp2" => CharAnchorPreset.Exp2,
            _ => CharAnchorPreset.None
        };

        var anchorSpec = new SetAnchorCommandSpecCharR
        {
            roleKey = roleKey,
            preset = preset,
            globalTuning = globalTuning
        };

        var resetTrackSpec = new ResetTrackOffsetsCommandSpec { roleKey = roleKey };

        Collect(anchorSpec);
        Collect(resetTrackSpec);
    }

    private void SetOriginSize(string roleKey, float xyValue)
    {
        var spec = new SetOriginSizeCommandSpecCharR
        {
            roleKey = roleKey,
            toScale = new Vector2(xyValue, xyValue)
        };

        Collect(spec);
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

        Collect(spec);
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