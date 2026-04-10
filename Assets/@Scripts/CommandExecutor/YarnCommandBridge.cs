using System;
using System.Collections;
using UnityEngine;
using Yarn.Unity;

public sealed class YarnCommandBridge : MonoBehaviour
{
    private DialogueRunner _dialogueRunner;
    private YarnBridgePlaybackDriver _playbackDriver;

    [Header("Rig")] public GameObject rigPrefab;
    [Header("Global Tuning")] public CharStageTuningSO globalTuning;

    public void Initialize(
        DialogueRunner dialogueRunner,
        YarnBridgePlaybackDriver playbackDriver)
    {
        _dialogueRunner = dialogueRunner;
        _playbackDriver = playbackDriver;
        RegisterYarnCommands();
    }

    public void RegisterYarnCommands()
    {
        _dialogueRunner.AddCommandHandler<string>("destroy", DestroyCommand);
        
        // Marks the next N collected commands as wait=true inside Presentation/Executor.
        // This affects command playback order, but does NOT block Yarn by itself.
        _dialogueRunner.AddCommandHandler<int>("await_for", AwaitFor);

        // Starts a Yarn-level hold block.
        _dialogueRunner.AddCommandHandler("begin_hold", BeginHold);
        
        // Blocking Yarn command:
        // closes the hold block and pauses Yarn until the held commands
        // marked with wait=true finish inside Presentation/Executor.
        _dialogueRunner.AddCommandHandler("end_hold", (Func<IEnumerator>)(() => PlayHeldCommands()));

        _dialogueRunner.AddCommandHandler<string>("slot_boxside", SetSpeakerSlot);
        _dialogueRunner.AddCommandHandler<string>("slot", SetCharSlot);
        _dialogueRunner.AddCommandHandler<string, string>("place", SetAnchorPosition);
        _dialogueRunner.AddCommandHandler<string, int, int>("place_offset", SetAnchorOffset);
        _dialogueRunner.AddCommandHandler<string, float>("size", SetOriginSize);
        _dialogueRunner.AddCommandHandler<string, float, float>("to_scale", ScaleFromTo);

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

        _dialogueRunner.AddCommandHandler<string>("blackout", ScreedBlackout);
        _dialogueRunner.AddCommandHandler<string>("uipatch", UIPatch);
        
        _dialogueRunner.AddCommandHandler<string, float>("bgm", PlayBgm);
        _dialogueRunner.AddCommandHandler<float>("stop_bgm", StopBgm);

        _dialogueRunner.AddCommandHandler<string>("voice", PlayVoice);
        _dialogueRunner.AddCommandHandler("stop_voice", StopVoice);

        _dialogueRunner.AddCommandHandler<string>("sfx", PlaySfx);
        _dialogueRunner.AddCommandHandler("stop_all_sfx", StopAllSfx);
        
        _dialogueRunner.AddCommandHandler<string, string, string, string>("emotion_wipe", SetEmotionPortraitWipe);
        
        _dialogueRunner.AddCommandHandler<string>("sway", SwayGentle);
        _dialogueRunner.AddCommandHandler<string>("sway_hard", SwayPendulum);
        _dialogueRunner.AddCommandHandler<string>("sway_fast", SwayFast);
        _dialogueRunner.AddCommandHandler<string>("sway_away", SwayAway);
        _dialogueRunner.AddCommandHandler<string, int>("sway_to", SwayRotateTo);
        _dialogueRunner.AddCommandHandler<string>("slide_in_sway", SlideInSway);
    }

    private void SwayRotateTo(string roleKey, int angle)
    {
        var spec = new SwayRotateToCommandSpecCharR()
        {
            roleKey = roleKey,
            degree = angle
        };
        
        Collect(spec);
    }

    private void SlideInSway(string roleKey)
    {
        var spec = new HideRootsCommandSpecCharR
        { 
                roleKey = roleKey,
                targetMask = CharRigRootLayerMask.CharacterPortrait_Root
        };
        
        var spec1 = new FadeInCommandSpecCharR()
        { 
            roleKey = roleKey,
            targetMask = CharRigRootLayerMask.CharacterPortrait_Root,
            duration = 0.28f
        };

        var spec2 = new JuicySlideInCommandSpecCharR
        {
            roleKey = roleKey,
            distance = 550f,
            duration = 0.45f
        };

        var spec3 = new DipInOutCommandSpecCharR()
        {
            roleKey = roleKey,
            target = CharacterRigTarget.Character_Track_Y,
            dir = SlideFromCharR.Right,
            distance = 22f,
            duration = 0.8f
        };

        var spec4 = new PunchScaleCommandSpecCharR()
        {
            roleKey = roleKey,
            strength = -0.03f,
            duration = 0.55f,
            vibrato = 3,
            elasticity = 0.45f
        };

        var spec5 = new DipInOutCommandSpecCharR()
        {
            roleKey = roleKey,
            target = CharacterRigTarget.Character_Track_Y,
            dir = SlideFromCharR.Down,
            distance = 12f,
            duration = 0.8f
        };
        
        var spec6 = new SwayCommandSpecCharR()
        {
            roleKey = roleKey,
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
            roleKey = roleKey,
            seconds = 0.4f,
        };

        var spec8 = new NudgeTapCommandSpecCharR()
        {
            roleKey = roleKey,
            target = CharacterRigTarget.Character_Track,
            strength = 45f,
            direction = SlideFromCharR.Up,
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
    
    
    private void SwayGentle(string roleKey)
    {
        var spec = new SwayCommandSpecCharR
        {
            roleKey = roleKey,
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
    private void SwayPendulum(string roleKey)
    {
        var spec = new SwayCommandSpecCharR
        {
            roleKey = roleKey,
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
    private void SwayFast(string roleKey)
    {
        var spec = new SwayCommandSpecCharR
        {
            roleKey = roleKey,
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
    private void SwayAway(string roleKey)
    {
        var spec = new SwayCommandSpecCharR
        {
            roleKey = roleKey,
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
    
    private void SetEmotionPortraitWipe(
        string roleKey,
        string character,
        string variant,
        string emotion)
    {
        var spec = new SetEmotionPortraitWipeCommandSpecCharR
        {
            roleKey = roleKey,
            portrait = new PortraitIdentity
            {
                character = character,
                variant = variant,
                emotion = emotion
            },
        };

        Collect(spec);
    }
    
    private void PlayBgm(string clipKey, float fadeDuration = 1f)
    {
        var spec = new PlayBgmCommandSpec
        {
            clipKey = clipKey,
            fadeDuration = fadeDuration
        };

        Collect(spec);
    }

    private void StopBgm(float fadeDuration = 1f)
    {
        var spec = new StopBgmCommandSpec
        {
            fadeDuration = fadeDuration
        };

        Collect(spec);
    }

    private void PlayVoice(string clipKey)
    {
        var spec = new PlayVoiceCommandSpec
        {
            clipKey = clipKey
        };

        Collect(spec);
    }

    private void StopVoice()
    {
        var spec = new StopVoiceCommandSpec();
        Collect(spec);
    }

    private void PlaySfx(string clipKey)
    {
        var spec = new PlaySfxCommandSpec
        {
            clipKey = clipKey
        };

        Collect(spec);
    }

    private void StopAllSfx()
    {
        var spec = new StopAllSfxCommandSpec();
        Collect(spec);
    }

    private void BeginHold()
    {
        _playbackDriver.BeginHold();
    }

    // Closes the active hold block and blocks Yarn until held wait=true commands finish.
    private IEnumerator PlayHeldCommands()
    {
        yield return _playbackDriver.EndHoldBlocking();
    }

    // Marks the next N collected commands as wait=true.
    // This only affects Presentation/Executor playback.
    private void AwaitFor(int count = 1)
    {
        _playbackDriver.WaitNextImmediateCommands(count);
    }

    private void Collect(CommandSpecBase spec)
    {
        _playbackDriver.Enqueue(spec);
    }

    private void UIPatch(string themeId)
    {
        var spec = new UIPatchCommandSpec
        {
            themeId = themeId,
        };

        Collect(spec);
    }

    private void ScreedBlackout(string transitionMode)
    {
        var spec = new TransitionCommandSpec
        {
            targetKind = TransitionTargetKind.Blackout
        };

        switch (transitionMode)
        {
            case "cover":
                spec.playMode = TransitionPlayMode.CoverOnly;
                spec.holdCoveredSeconds = 0.2f;
                spec.wait = true;
                break;

            case "uncover":
                spec.playMode = TransitionPlayMode.UncoverOnly;
                spec.wait = true;
                break;

            case "cover_then_uncover":
                spec.playMode = TransitionPlayMode.CoverThenUncover;
                break;

            default:
                Debug.LogWarning(
                    $"[ScreedBlackout] Unknown transitionMode '{transitionMode}'. Fallback to CoverThenUncover.");
                spec.playMode = TransitionPlayMode.CoverThenUncover;
                break;
        }

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

    private void NudgeShake(string roleKey, string direction = "right")
    {
        SlideFromCharR dir = ParseSlideDirection(direction, SlideFromCharR.Right);

        var spec = new NudgeTapCommandSpecCharR
        {
            target = CharacterRigTarget.CharacterPortrait_Shake,
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

    private void ScaleFromTo(string roleKey, float xyValue, float duration = 0.4f)
    {
        var spec = new ScaleFromToCommandSpecCharR
        {
            roleKey = roleKey,
            duration = duration,
            toScale = new Vector2(xyValue, xyValue)
        };

        Collect(spec);
    }
    
    private void SetOriginSize(string roleKey, float xyValue)
    {
        var spec = new SetOriginSizeCommandSpecCharR
        {
            roleKey = roleKey,
            toScale = new Vector2(xyValue, xyValue)
        };

        // var spec1 = new SetScaleCommandSpecCharR
        // {
        //     roleKey = roleKey
        // };

        Collect(spec);
        //Collect(spec1);
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