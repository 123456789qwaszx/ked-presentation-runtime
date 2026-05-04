using System;
using System.Collections;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge : MonoBehaviour
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
        
        RegisterEmojiCommands();
        RegisterPresentationCommands();
        RegisterShotCommands();
    }

    public void RegisterYarnCommands()
    {
        
        _dialogueRunner.AddCommandHandler<string>("destroy", EnqueueDestroySpec);

        // Marks the next N collected commands as wait=true inside Presentation/Executor.
        // This affects command playback order, but does NOT block Yarn by itself.
        _dialogueRunner.AddCommandHandler<int>("await_for", AwaitFor);

        // Starts a Yarn-level hold block.
        _dialogueRunner.AddCommandHandler("begin_hold", BeginHold);

        // Blocking Yarn command:
        // closes the hold block and pauses Yarn until the held commands
        // marked with wait=true finish inside Presentation/Executor.
        //_dialogueRunner.AddCommandHandler("end_hold", (Func<IEnumerator>)(() => PlayHeldCommands()));
        _dialogueRunner.AddCommandHandler("end_hold", PlayHeldCommands);

        _dialogueRunner.AddCommandHandler<string>("slot_boxside", EnqueueSetupCharRigSpecProtagonistSlot);
        _dialogueRunner.AddCommandHandler<string>("slot", EnqueueSetupCharRigSpec);
        _dialogueRunner.AddCommandHandler<string, string>("place", EnqueueSetAnchorSpecs);
        _dialogueRunner.AddCommandHandler<string, int, int>("place_offset", EnqueueSetAnchorOffsetSpecs);
        _dialogueRunner.AddCommandHandler<string, float>("size", EnqueueSetOriginSizeSpec);
        _dialogueRunner.AddCommandHandler<string, float, float>("to_scale", EnqueueScaleToSpec);

        _dialogueRunner.AddCommandHandler<string, string>("slide_in", EnqueueSlideInSpec);
        _dialogueRunner.AddCommandHandler<string, string>("slide_out", EnqueueSlideOutSpec);

        _dialogueRunner.AddCommandHandler<string>("fade_in", EnqueueFadeInSpec);
        _dialogueRunner.AddCommandHandler<string>("fade_out", EnqueueFadeOutSpec);

        _dialogueRunner.AddCommandHandler<string, float, float>("move_by", EnqueueMoveBySpec);
        _dialogueRunner.AddCommandHandler<string, string>("dip", EnqueueDipInOutSpec);

        _dialogueRunner.AddCommandHandler<string, float, string>("hop_in", EnqueueArcHopInSpec);

        _dialogueRunner.AddCommandHandler<string, string>("jolt", EnqueueJoltSpec);
        _dialogueRunner.AddCommandHandler<string, string>("shake", EnqueueJoltSpecShake);
        _dialogueRunner.AddCommandHandler<string, string>("nudge", EnqueueJoltSpecTap);
        _dialogueRunner.AddCommandHandler<string, string>("nudge_hard", EnqueueJoltSpecTapHard);
        _dialogueRunner.AddCommandHandler<string, string>("slide_in_nudge", EnqueueSlideInJoltCombo);

        _dialogueRunner.AddCommandHandler<string, string>("portrait_cross", EnqueueSetPortraitCrossfadeSpec);
        _dialogueRunner.AddCommandHandler<string, string>("portrait_swap", EnqueueSetEmotionPortraitWipeSpec);

        _dialogueRunner.AddCommandHandler<string>("blackout", EnqueueBlackoutTransitionSpec);
        _dialogueRunner.AddCommandHandler<string>("uipatch", EnqueueUIPatchSpec);

        _dialogueRunner.AddCommandHandler<string, float>("bgm", EnqueuePlayBgmSpec);
        _dialogueRunner.AddCommandHandler<float>("stop_bgm", EnqueueStopBgmSpec);

        _dialogueRunner.AddCommandHandler<string>("voice", EnqueuePlayVoiceSpec);
        _dialogueRunner.AddCommandHandler("stop_voice", EnqueueStopVoiceSpec);

        _dialogueRunner.AddCommandHandler<string>("sfx", EnqueuePlaySfxSpec);
        _dialogueRunner.AddCommandHandler("stop_all_sfx", EnqueueStopAllSfxSpec);

        _dialogueRunner.AddCommandHandler<string, string, string, string>("emotion_wipe",
            EnqueueSetEmotionPortraitWipeSpec);

        _dialogueRunner.AddCommandHandler<string>("sway", EnqueueSwaySpecGentle);
        _dialogueRunner.AddCommandHandler<string>("sway_hard", EnqueueSwaySpecPendulum);
        _dialogueRunner.AddCommandHandler<string>("sway_fast", EnqueueSwaySpecFast);
        _dialogueRunner.AddCommandHandler<string>("sway_away", EnqueueSwaySpecAway);
        _dialogueRunner.AddCommandHandler<string, int>("sway_to", EnqueuePivotRotateToSpec);
        _dialogueRunner.AddCommandHandler<string>("slide_in_sway", EnqueueSlideInSwayCombo);


        // slot <-> character binding
        _dialogueRunner.AddCommandHandler<string, string>("cast", EnqueueCastCharAndSetPortraitCommandSpec);
        _dialogueRunner.AddCommandHandler<string>("uncast", EnqueueUncastCharacterSpec);

        // character-target commands
        _dialogueRunner.AddCommandHandler<string, string>("jolt_char", EnqueueJoltByCharacterSpec);
        _dialogueRunner.AddCommandHandler<string, string>("shake_char", EnqueueJoltByCharacterSpecShake);
        _dialogueRunner.AddCommandHandler<string, string>("nudge_char", EnqueueJoltByCharacterSpecTap);
        _dialogueRunner.AddCommandHandler<string, string>("nudge_hard_char", EnqueueJoltByCharacterSpecTapHard);

        _dialogueRunner.AddCommandHandler<string, string>("portrait_cross_char", EnqueueSetPortraitCrossfadeByCharacterSpec);
        _dialogueRunner.AddCommandHandler<string, string>("portrait_swap_char", EnqueueSetEmotionPortraitWipeByCharacterSpec);
        _dialogueRunner.AddCommandHandler<string, string, string, string>("emotion_wipe_char", EnqueueSetEmotionPortraitWipeByCharacterSpec);
        
        _dialogueRunner.AddCommandHandler<float>("await", EnqueueWaitSpec);
    }

    private void EnqueueWaitSpec(float duration)
    {
        var spec = new WaitCommandSpec()
        {
            roleKey = "",
            seconds = duration,
        };
        
        Collect(spec);
    }

    private void EnqueuePivotRotateToSpec(string roleKey, int angle)
    {
        var spec = new PivotRotateToCommandSpecCharR()
        {
            roleKey = roleKey,
            degree = angle
        };

        Collect(spec);
    }

    private void EnqueueSlideInSwayCombo(string roleKey)
    {
        var spec = new HideRootLayersCommandSpecCharR
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

        var spec2 = new SlideInCommandSpecCharR
        {
            roleKey = roleKey,
            distance = 550f,
            duration = 0.45f
        };

        var spec3 = new DipInOutCommandSpecCharR()
        {
            roleKey = roleKey,
            target = CharacterRigTarget.Character_Track_Y,
            dir = CharRDirection.Right,
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
            dir = CharRDirection.Down,
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

        var spec8 = new JoltCommandSpecCharR()
        {
            roleKey = roleKey,
            target = CharacterRigTarget.Character_Track,
            strength = 45f,
            direction = CharRDirection.Up,
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

    private void EnqueueSwaySpecGentle(string roleKey)
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

    private void EnqueueSwaySpecPendulum(string roleKey)
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

    private void EnqueueSwaySpecFast(string roleKey)
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

    private void EnqueueSwaySpecAway(string roleKey)
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

    private void EnqueueSetEmotionPortraitWipeSpec(
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

    private void EnqueuePlayBgmSpec(string clipKey, float fadeDuration = 1f)
    {
        var spec = new PlayBgmCommandSpec
        {
            clipKey = clipKey,
            fadeDuration = fadeDuration
        };

        Collect(spec);
    }

    private void EnqueueStopBgmSpec(float fadeDuration = 1f)
    {
        var spec = new StopBgmCommandSpec
        {
            fadeDuration = fadeDuration
        };

        Collect(spec);
    }

    private void EnqueuePlayVoiceSpec(string clipKey)
    {
        var spec = new PlayVoiceCommandSpec
        {
            clipKey = clipKey
        };

        Collect(spec);
    }

    private void EnqueueStopVoiceSpec()
    {
        var spec = new StopVoiceCommandSpec();
        Collect(spec);
    }

    private void EnqueuePlaySfxSpec(string clipKey)
    {
        var spec = new PlaySfxCommandSpec
        {
            clipKey = clipKey
        };

        Collect(spec);
    }

    private void EnqueueStopAllSfxSpec()
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
        if (spec == null)
            return;

        if (_importSink != null)
        {
            _importSink.Enqueue(spec);
            return;
        }

        _playbackDriver.Enqueue(spec);
    }

    private void EnqueueUIPatchSpec(string themeId)
    {
        var spec = new UIPatchCommandSpec
        {
            themeId = themeId,
        };

        Collect(spec);
    }

    private void EnqueueBlackoutTransitionSpec(string transitionMode)
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
                    $"[EnqueueBlackoutTransitionSpec] Unknown transitionMode '{transitionMode}'. Fallback to CoverThenUncover.");
                spec.playMode = TransitionPlayMode.CoverThenUncover;
                break;
        }

        Collect(spec);
    }

    private void EnqueueDestroySpec(string roleKey)
    {
        var spec = new DestroyCommandSpec
        {
            roleKey = roleKey
        };

        Collect(spec);
    }

    private void EnqueueSlideInJoltCombo(string roleKey, string direction = "right")
    {
        CharRDirection dir = ParseSlideDirection(direction, CharRDirection.Right);

        var juicySlideIn = new SlideInCommandSpecCharR
        {
            roleKey = roleKey,
            target = CharacterRigTarget.Character_Track_X,
            direction = dir
        };

        var spec = new JoltCommandSpecCharR
        {
            roleKey = roleKey,
            target = CharacterRigTarget.Character_Track_Y,
            direction = CharRDirection.Up,
            strength = 340f,
            duration = 0.6f,
            taps = 4,
            damping = 9,
            anticipation = -12
        };

        Collect(spec);
        Collect(juicySlideIn);
    }

    private void EnqueueJoltSpec(string roleKey, string direction = "right")
    {
        CharRDirection dir = ParseSlideDirection(direction, CharRDirection.Right);

        var spec = new JoltCommandSpecCharR
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

    private void EnqueueJoltSpecShake(string roleKey, string direction = "right")
    {
        CharRDirection dir = ParseSlideDirection(direction, CharRDirection.Right);

        var spec = new JoltCommandSpecCharR
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

    private void EnqueueJoltSpecTap(string roleKey, string direction = "right")
    {
        CharRDirection dir = ParseSlideDirection(direction, CharRDirection.Right);

        var spec = new JoltCommandSpecCharR
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

    private void EnqueueJoltSpecTapHard(string roleKey, string direction = "down")
    {
        CharRDirection dir = ParseSlideDirection(direction, CharRDirection.Down);

        var spec = new JoltCommandSpecCharR
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

    private void EnqueueArcHopInSpec(string roleKey, float distance, string direction = "left")
    {
        CharRDirection dir = ParseSlideDirection(direction, CharRDirection.Down);

        var spec = new ArcHopInCommandSpecCharR
        {
            roleKey = roleKey,
            distance = distance,
            from = dir
        };

        Collect(spec);
    }

    private void EnqueueDipInOutSpec(string roleKey, string direction = "down")
    {
        CharRDirection dir = ParseSlideDirection(direction, CharRDirection.Down);

        var spec = new DipInOutCommandSpecCharR
        {
            roleKey = roleKey,
            dir = dir
        };

        Collect(spec);
    }

    private void EnqueueMoveBySpec(string roleKey, float x, float y)
    {
        var spec = new MoveByCommandSpecCharR
        {
            roleKey = roleKey,
            delta = new Vector2(x, y)
        };

        Collect(spec);
    }

    private void EnqueueFadeInSpec(string roleKey)
    {
        var spec = new FadeInCommandSpecCharR
        {
            roleKey = roleKey
        };

        Collect(spec);
    }

    private void EnqueueFadeOutSpec(string roleKey)
    {
        var spec = new FadeOutCommandSpecCharR
        {
            roleKey = roleKey
        };

        Collect(spec);
    }

    private void EnqueueSlideInSpec(string roleKey, string direction = "left")
    {
        CharRDirection from = ParseSlideDirection(direction, CharRDirection.Left);

        var spec = new SlideInCommandSpecCharR
        {
            roleKey = roleKey,
            direction = from
        };

        Collect(spec);
    }

    private void EnqueueSlideOutSpec(string roleKey, string direction = "right")
    {
        CharRDirection to = ParseSlideDirection(direction, CharRDirection.Right);

        var spec = new SlideOutCommandSpecCharR
        {
            roleKey = roleKey,
            to = to
        };

        Collect(spec);
    }

    private void EnqueueSetupCharRigSpec(string roleKey)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogError("[YarnCommandBridge] slot: roleKey is null or empty.");
            return;
        }

        var spec = new SetupCharRigCommandSpec
        {
            roleKey = roleKey,
            rigPrefab = rigPrefab
        };

        Collect(spec);
    }

    private void EnqueueSetupCharRigSpecProtagonistSlot(string roleKey)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogError("[YarnCommandBridge] slot: roleKey is null or empty.");
            return;
        }

        var spec = new SetupCharRigCommandSpec
        {
            roleKey = roleKey,
            parentSlot = CharRigSlot.ProtagonistSlot,
            rigPrefab = rigPrefab
        };

        Collect(spec);
    }

    private void EnqueueSetAnchorOffsetSpecs(string roleKey, int x, int y)
    {
        var anchorSpec = new MoveByCommandSpecCharR
        {
            roleKey = roleKey,
            target = CharacterRigTarget.Character_Anchor,
            delta = new Vector2(x, y),
            duration = 0f,
            killTween = false
        };

        var resetTrackSpec = new ApplyTrackOffsetCommandSpecCharR { roleKey = roleKey };

        Collect(anchorSpec);
        Collect(resetTrackSpec);
    }

    private void EnqueueSetAnchorSpecs(string roleKey, string positionPreset)
    {
        CharAnchorPreset preset = positionPreset switch
        {
            "left" => CharAnchorPreset.Left,
            "center" => CharAnchorPreset.Center,
            "right" => CharAnchorPreset.Right,

            "duo_left" => CharAnchorPreset.DuoLeft,
            "duo_right" => CharAnchorPreset.DuoRight,

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

        var resetTrackSpec = new ApplyTrackOffsetCommandSpecCharR
        {
            roleKey = roleKey
        };

        Collect(anchorSpec);
        Collect(resetTrackSpec);
    }

    private void EnqueueScaleToSpec(string roleKey, float xyValue, float duration = 0.4f)
    {
        var spec = new ScaleToCommandSpecCharR
        {
            roleKey = roleKey,
            duration = duration,
            toScale = new Vector2(xyValue, xyValue)
        };

        Collect(spec);
    }

    private void EnqueueSetOriginSizeSpec(string roleKey, float xyValue)
    {
        var spec = new SetOriginSizeCommandSpecCharR
        {
            roleKey = roleKey,
            toScale = new Vector2(xyValue, xyValue)
        };

        Collect(spec);
    }

    private void EnqueueSetEmotionPortraitWipeSpec(string roleKey, string character)
    {
        var portraitIdentity = new PortraitIdentity
        {
            character = character,
            variant = "a",
            emotion = "1"
        };

        var spec = new SetEmotionPortraitWipeCommandSpecCharR
        {
            roleKey = roleKey,
            portrait = portraitIdentity
        };

        Collect(spec);
    }

    private void EnqueueSetPortraitCrossfadeSpec(string roleKey, string character)
    {
        var portraitIdentity = new PortraitIdentity
        {
            character = character,
            variant = "a",
            emotion = "1"
        };

        var spec = new SetPortraitCrossfadeCommandSpecCharR
        {
            roleKey = roleKey,
            portrait = portraitIdentity
        };

        Collect(spec);
    }

    private void EnqueueCastCharAndSetPortraitCommandSpec(string roleKey, string character)
    {
        var spec = new CastCharacterCommandSpec
        {
            roleKey = roleKey,
            characterKey = character,
            requireExistingRig = true,
            strict = true
        };

        var portraitIdentity = new PortraitIdentity
        {
            character = character,
            variant = "a",
            emotion = "1"
        };

        var spec2 = new SetPortraitSpriteCommandSpecCharR
        {
            roleKey = roleKey,
            portrait = portraitIdentity
        };

        Collect(spec);
        Collect(spec2);
    }

    // private void EnqueueCastCharacterSpec(string roleKey, string characterKey)
    // {
    //     var spec = new CastCharacterCommandSpec
    //     {
    //         roleKey = roleKey,
    //         characterKey = characterKey,
    //         requireExistingRig = true,
    //         strict = true
    //     };
    //
    //     Collect(spec);
    // }

    private void EnqueueUncastCharacterSpec(string roleKey)
    {
        var spec = new UncastCharacterCommandSpec
        {
            roleKey = roleKey,
            strict = true
        };

        Collect(spec);
    }

    private CharRDirection ParseSlideDirection(string direction, CharRDirection fallback)
    {
        switch (direction?.Trim().ToLowerInvariant())
        {
            case "left":
            case "l":
                return CharRDirection.Left;

            case "right":
            case "r":
                return CharRDirection.Right;

            case "up":
            case "u":
            case "top":
                return CharRDirection.Up;

            case "down":
            case "d":
            case "bottom":
                return CharRDirection.Down;

            default:
                return fallback;
        }
    }

    #region Character0target Commands

    private void EnqueueJoltByCharacterSpec(string characterKey, string direction = "right")
    {
        CharRDirection dir = ParseSlideDirection(direction, CharRDirection.Right);

        var spec = new JoltByCharacterCommandSpec
        {
            characterKey = characterKey,
            target = CharacterRigTarget.Character_Track_Y,
            direction = dir,
            strength = 340f,
            duration = 0.6f,
            taps = 3,
            damping = 8,
            anticipation = -12,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueJoltByCharacterSpecShake(string characterKey, string direction = "right")
    {
        CharRDirection dir = ParseSlideDirection(direction, CharRDirection.Right);

        var spec = new JoltByCharacterCommandSpec
        {
            characterKey = characterKey,
            target = CharacterRigTarget.CharacterPortrait_Shake,
            direction = dir,
            strength = 44f,
            duration = 1.2f,
            taps = 4,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueJoltByCharacterSpecTap(string characterKey, string direction = "right")
    {
        CharRDirection dir = ParseSlideDirection(direction, CharRDirection.Right);

        var spec = new JoltByCharacterCommandSpec
        {
            characterKey = characterKey,
            target = CharacterRigTarget.Character_Track,
            direction = dir,
            strength = 340f,
            duration = 0.6f,
            taps = 1,
            damping = 9,
            anticipation = -12,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueJoltByCharacterSpecTapHard(string characterKey, string direction = "down")
    {
        CharRDirection dir = ParseSlideDirection(direction, CharRDirection.Down);

        var spec = new JoltByCharacterCommandSpec
        {
            characterKey = characterKey,
            direction = dir,
            strength = 1400f,
            duration = 0.7f,
            taps = 1,
            damping = 9,
            anticipation = 4,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueSetPortraitCrossfadeByCharacterSpec(string characterKey, string character)
    {
        var portraitIdentity = new PortraitIdentity
        {
            character = character,
            variant = "a",
            emotion = "1"
        };

        var spec = new SetPortraitCrossfadeByCharacterCommandSpec
        {
            characterKey = characterKey,
            portrait = portraitIdentity,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueSetEmotionPortraitWipeByCharacterSpec(string characterKey, string character)
    {
        var portraitIdentity = new PortraitIdentity
        {
            character = character,
            variant = "a",
            emotion = "1"
        };

        var spec = new SetEmotionPortraitWipeByCharacterCommandSpec
        {
            characterKey = characterKey,
            portrait = portraitIdentity,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueSetEmotionPortraitWipeByCharacterSpec(
        string characterKey,
        string character,
        string variant,
        string emotion)
    {
        var spec = new SetEmotionPortraitWipeByCharacterCommandSpec
        {
            characterKey = characterKey,
            portrait = new PortraitIdentity
            {
                character = character,
                variant = variant,
                emotion = emotion
            },
            strict = true
        };

        Collect(spec);
    }

    #endregion
}