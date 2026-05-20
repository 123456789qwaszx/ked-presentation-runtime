using System;
using System.Collections;
using System.Globalization;
using DG.Tweening;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge : MonoBehaviour
{
    private DialogueRunner _dialogueRunner;
    private YarnBridgePlaybackDriver _playbackDriver;

    [Header("Rig")] public RectTransform rigPrefab;
    [Header("Global Tuning")] public CharStageTuningSO globalTuning;
    [Header("Role Tuning")] public RoleAnchorTuningDBSO roleTuningDb;

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
        
        _dialogueRunner.AddCommandHandler<string, string>("slot", EnqueueSetupCharRigSpec);
        _dialogueRunner.AddCommandHandler<string, string>("place", EnqueueSetAnchorSpecs);
        _dialogueRunner.AddCommandHandler<string, int, int>("place_offset", EnqueueSetAnchorOffsetSpecs);
        _dialogueRunner.AddCommandHandler<string, string>("size", EnqueueSetOriginSizeSpec);
        
        _dialogueRunner.AddCommandHandler<string, float, float>("move_by", EnqueueMoveBySpec);
        _dialogueRunner.AddCommandHandler<string, float, float>("scale_to", EnqueueScaleToSpec);

        _dialogueRunner.AddCommandHandler<string, string>("slide_in", EnqueueSlideInSpec);
        _dialogueRunner.AddCommandHandler<string, string>("slide_out", EnqueueSlideOutSpec);

        _dialogueRunner.AddCommandHandler<string>("fade_in", EnqueueFadeInSpec);
        _dialogueRunner.AddCommandHandler<string>("fade_out", EnqueueFadeOutSpec);

        
        _dialogueRunner.AddCommandHandler<string, int, float, float>("hop_in", EnqueueArcHopInSpec);
        _dialogueRunner.AddCommandHandler<string, float, float, float, float>("walk_in_place", EnqueueWalkInPlaceSpec);
        _dialogueRunner.AddCommandHandler<string, float, float, float, float>("bounce_in_place", EnqueueBounceInPlaceSpec);
        _dialogueRunner.AddCommandHandler<string, float, float, float>("breathe", EnqueueBreathInPlaceSpec);
        

        _dialogueRunner.AddCommandHandler<string, string>("jolt", EnqueueJoltSpec);
        _dialogueRunner.AddCommandHandler<string, string, float, float, int>("shake", EnqueueJoltSpecShake);
        _dialogueRunner.AddCommandHandler<string, string>("nudge", EnqueueJoltSpecTap);
        _dialogueRunner.AddCommandHandler<string, string>("nudge_hard", EnqueueJoltSpecTapHard);
        _dialogueRunner.AddCommandHandler<string, string>("slide_in_nudge", EnqueueSlideInJoltCombo);
        
        _dialogueRunner.AddCommandHandler<string, string>("dip", EnqueueDipInOutSpec);
        
        _dialogueRunner.AddCommandHandler<string, float, float, float, string>("tremble", EnqueueTrembleSpec);
        _dialogueRunner.AddCommandHandler<string, float, float, float, float, float, string>("tremble_pulse", EnqueueTremblePulseSpec);

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

        // _dialogueRunner.AddCommandHandler<string, string, string, string>("emotion_wipe",
        //     EnqueueSetEmotionPortraitWipeSpec);
        _dialogueRunner.AddCommandHandler<string, string>("emotion", EnqueueSetEmotionPortraitWipeSpec);

        _dialogueRunner.AddCommandHandler<string>("sway", EnqueueSwaySpecGentle);
        _dialogueRunner.AddCommandHandler<string>("sway_hard", EnqueueSwaySpecPendulum);
        _dialogueRunner.AddCommandHandler<string>("sway_fast", EnqueueSwaySpecFast);
        _dialogueRunner.AddCommandHandler<string>("sway_away", EnqueueSwaySpecAway);
        _dialogueRunner.AddCommandHandler<string, int>("sway_to", EnqueuePivotRotateToSpec);
        _dialogueRunner.AddCommandHandler<string>("slide_in_sway", EnqueueSlideInSwayCombo);


        // slot <-> character binding
        _dialogueRunner.AddCommandHandler<string, string, string>("cast", EnqueueCastCharacterSpec);
        _dialogueRunner.AddCommandHandler<string>("uncast", EnqueueUncastCharacterSpec);
        
        _dialogueRunner.AddCommandHandler<float>("pause", EnqueueWaitSpec);
        
        // clear character rigs
        _dialogueRunner.AddCommandHandler("clearall_rigs", EnqueueClearAllCharRigRefsSpec);
        _dialogueRunner.AddCommandHandler<string>("clear_rig", EnqueueClearCharRigRefSpec);
    }

    private void EnqueueClearAllCharRigRefsSpec()
    {
        var spec = new ClearCharRigRefsCommandSpec
        {
            removeKeys = true,
            destroyRigObjects = true,
            killTweensBeforeDestroy = true,
            onlyRoleKeys = null
        };

        Collect(spec);
    }

    private void EnqueueClearCharRigRefSpec(string roleKey)
    {
        var spec = new ClearCharRigRefsCommandSpec
        {
            removeKeys = true,
            destroyRigObjects = true,
            killTweensBeforeDestroy = true,
            onlyRoleKeys = new[] { roleKey }
        };

        Collect(spec);
    }
    
    private void EnqueueWaitSpec(float duration)
    {
        var spec = new WaitCommandSpec()
        {
            seconds = duration,
        };
        
        Collect(spec);
    }

    private void EnqueuePivotRotateToSpec(string roleKey, int angle)
    {
        var spec = new PivotRotateToCommandSpecCharR()
        {
            targetKey = roleKey,
            degree = angle
        };

        Collect(spec);
    }

    private void EnqueueSlideInSwayCombo(string roleKey)
    {
        var spec = new HideRootLayersCommandSpecCharR
        {
            targetKey = roleKey,
            targetMask = CharRigRootMask.CharacterPortrait_Root
        };

        var spec1 = new FadeInCommandSpecCharR()
        {
            targetKey = roleKey,
            targetMask = CharRigRootMask.CharacterPortrait_Root,
            duration = 0.28f
        };

        var spec2 = new SlideInCommandSpecCharR
        {
            targetKey = roleKey,
            distance = 550f,
            duration = 0.45f
        };

        var spec3 = new DipInOutCommandSpecCharR()
        {
            targetKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_Track_Y,
            dir = CharRigDirection.Right,
            distance = 22f,
            duration = 0.8f
        };

        var spec4 = new PunchScaleCommandSpecCharR()
        {
            targetKey = roleKey,
            strength = -0.03f,
            duration = 0.55f,
            vibrato = 3,
            elasticity = 0.45f
        };

        var spec5 = new DipInOutCommandSpecCharR()
        {
            targetKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_Track_Y,
            dir = CharRigDirection.Down,
            distance = 12f,
            duration = 0.8f
        };

        var spec6 = new SwayCommandSpecCharR()
        {
            targetKey = roleKey,
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
            seconds = 0.4f,
        };

        var spec8 = new JoltCommandSpec()
        {
            targetKey = roleKey,
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

    private void EnqueueSwaySpecGentle(string roleKey)
    {
        var spec = new SwayCommandSpecCharR
        {
            targetKey = roleKey,
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
            targetKey = roleKey,
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
            targetKey = roleKey,
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
            targetKey = roleKey,
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
                spec.wait = true;
                break;

            case "uncover":
                spec.playMode = TransitionPlayMode.UncoverOnly;
                spec.wait = true;
                break;

            case "cover_then_uncover":
                spec.playMode = TransitionPlayMode.CoverThenUncover;
                spec.holdCoveredSeconds = 1.5f;
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
            targetKey = roleKey
        };

        Collect(spec);
    }

    private void EnqueueSlideInJoltCombo(string roleKey, string direction = "right")
    {
        CharRigDirection dir = ParseSlideDirection(direction, CharRigDirection.Right);

        var juicySlideIn = new SlideInCommandSpecCharR
        {
            targetKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_Track_X,
            direction = dir
        };

        var spec = new JoltCommandSpec
        {
            targetKey = roleKey,
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

    private void EnqueueJoltSpec(string roleKey, string direction = "right")
    {
        CharRigDirection dir = ParseSlideDirection(direction, CharRigDirection.Right);

        var spec = new JoltCommandSpec
        {
            targetKey = roleKey,
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

    private void EnqueueJoltSpecShake(
        string roleKey,
        string direction = "right",
        float strength = 44f,
        float duration = 1.2f,
        int taps = 4)
    {
        CharRigDirection dir = ParseSlideDirection(direction, CharRigDirection.Right);

        var spec = new JoltCommandSpec
        {
            target = CharacterRigTarget.CharacterPortrait_Shake,
            targetKey = roleKey,
            direction = dir,
            strength = strength,
            duration = duration,
            taps = taps
        };

        Collect(spec);
    }

    private void EnqueueJoltSpecTap(string roleKey, string direction = "right")
    {
        CharRigDirection dir = ParseSlideDirection(direction, CharRigDirection.Right);

        var spec = new JoltCommandSpec
        {
            targetKey = roleKey,
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
        CharRigDirection dir = ParseSlideDirection(direction, CharRigDirection.Down);

        var spec = new JoltCommandSpec
        {
            targetKey = roleKey,
            direction = dir,
            strength = 1400f,
            duration = 0.7f,
            taps = 1,
            damping = 9,
            anticipation = 4
        };

        Collect(spec);
    }
    
    private void EnqueueTrembleSpec(
        string roleKey,
        float duration = 1.2f,
        float strength = 8f,
        float frequency = 24f,
        string direction = "right")
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogError("[YarnCommandBridge] tremble: roleKey is null or empty.");
            return;
        }

        CharRigDirection dir = ParseSlideDirection(direction, CharRigDirection.Right);

        var spec = new TrembleCommandSpecCharR
        {
            targetKey = roleKey.Trim(),
            target = CharacterRigTarget.CharacterPortrait_Shake,
            direction = dir,
            duration = duration,
            strength = strength,
            frequency = frequency,
            crossAxisRatio = 0.35f,
            noiseRatio = 0.25f,
            blendIn = 0.04f,
            blendOut = 0.08f,
            wait = false,
            killTween = true
        };

        Collect(spec);
    }
    
    private void EnqueueTremblePulseSpec(
        string roleKey,
        float duration = 5.0f,
        float strength = 5f,
        float frequency = 28f,
        float pulseInterval = 1.0f,
        float pulseDuration = 0.16f,
        string direction = "right")
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogError("[YarnCommandBridge] tremble_pulse: roleKey is null or empty.");
            return;
        }

        CharRigDirection dir = ParseSlideDirection(direction, CharRigDirection.Right);

        var spec = new TrembleCommandSpecCharR
        {
            targetKey = roleKey.Trim(),
            target = CharacterRigTarget.CharacterPortrait_Shake,
            direction = dir,
            duration = duration,
            strength = strength,
            frequency = frequency,
            crossAxisRatio = 0.25f,
            noiseRatio = 0.25f,
            blendIn = 0.025f,
            blendOut = 0.06f,
            usePulse = true,
            pulseInterval = pulseInterval,
            pulseDuration = pulseDuration,
            wait = false,
            killTween = true
        };

        Collect(spec);
    }

    private void EnqueueArcHopInSpec(
        string roleKey,
        int hopCount = 2,
        float arcHeight = 48f,
        float airWidth = 0.85f)
    {
        var spec = new ArcHopInCommandSpecCharR
        {
            targetKey = roleKey,
            hopCount = hopCount,
            arcHeight = arcHeight,
            airWidth = airWidth
        };

        Collect(spec);
    }
    
    
    
    //<<walk_in_place Mercurio 99 2.2 28 0.9>> 터벅터벅
    //<<walk_in_place Mercurio 99 4.0 21 0.7>> 종종걸음
    private void EnqueueWalkInPlaceSpec(
        string roleKey,
        float duration = 99f,
        float stepsPerSecond = 1.9f,
        float arcHeight = 18f,
        float airWidth = 0.95f)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogError("[YarnCommandBridge] walk_in_place: roleKey is null or empty.");
            return;
        }

        var spec = new WalkInPlaceCommandSpecCharR
        {
            targetKey = roleKey.Trim(),
            duration = duration,
            stepsPerSecond = stepsPerSecond,
            arcHeight = arcHeight,
            airWidth = Mathf.Clamp(airWidth, 0.05f, 1f),
            sideSway = 0.3f,
            blendIn = 0.08f,
            blendOut = 0.08f,
            wait = false,
            killTween = true
        };

        Collect(spec);
    }
    
    private void EnqueueBounceInPlaceSpec(
        string roleKey,
        float duration = 99f,
        float bouncesPerSecond = 2.5f,
        float height = 32f,
        float riseRatio = 0.18f)
    {
        // Ease.InQuad 
        // 초반: 천천히 내려옴
        // 후반: 점점 빨라짐
        // 착지: 탁 떨어지는 느낌
        // 자주 쓸 만한 fallEase 감각
        // Ease.Linear
        //     = 일정한 속도로 내려옴.
        //     = 기계적이고 단순함.
        //
        //         Ease.InSine
        //     = 위에서 살짝 머물다가 자연스럽게 내려옴.
        //     = 부드러운 낙하.
        //
        //         Ease.InQuad
        //     = 위에서 천천히 내려오다가 후반에 빨라짐.
        //     = 지금 기본값. "톡 튀고 착지"에 적당함.
        //
        //         Ease.InCubic
        //     = InQuad보다 더 오래 위에 있다가 더 빠르게 떨어짐.
        //     = 더 만화적인 "탁!" 느낌.
        //
        //         Ease.InQuart / Ease.InQuint
        //     = 거의 공중에 멈춘 듯하다가 급하게 떨어짐.
        //     = 과장된 코믹/카툰 느낌.
        //
        //         Ease.OutQuad
        //     = 처음에 빨리 내려오고, 바닥에 가까워질수록 천천히 착지.
        //     = 부드럽게 내려앉는 느낌.
        //
        //         Ease.OutSine
        //     = 아주 부드러운 착지.
        //     = 말랑하고 가벼운 느낌.
        //
        //         Ease.InOutSine
        //     = 초반/후반 모두 부드럽고 중간이 빠름.
        //     = 자연스럽지만 "탁" 느낌은 약함.
        var spec = new BounceInPlaceCommandSpecCharR
        {
            targetKey = roleKey.Trim(),
            duration = duration,
            bouncesPerSecond = bouncesPerSecond,
            height = height,
            riseRatio = Mathf.Clamp(riseRatio, 0.05f, 0.8f),
            sideSway = 0.2f,
            riseEase = Ease.InQuart,
            fallEase = Ease.InOutSine,
            blendIn = 0.04f,
            blendOut = 0.08f,
            wait = false,
            killTween = true
        };

        Collect(spec);
    }
    
    private void EnqueueBreathInPlaceSpec(
        string roleKey,
        float duration = 2.4f,
        float height = 8f,
        float breathsPerSecond = 0.35f)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogError("[YarnCommandBridge] breathe: roleKey is null or empty.");
            return;
        }

        var spec = new BreathInPlaceCommandSpecCharR
        {
            targetKey = roleKey.Trim(),
            target = CharacterRigTarget.CharacterPortrait_Track_Y,
            duration = duration,
            breathsPerSecond = breathsPerSecond,
            height = height,
            sideSway = 0f,
            useScalePulse = false,
            scaleAmount = 0.015f,
            ease = Ease.InOutSine,
            phaseOffset = 0f,
            blendIn = 0.25f,
            blendOut = 0.25f,
            wait = false,
            killTween = true
        };

        Collect(spec);
    }

    private void EnqueueDipInOutSpec(string roleKey, string direction = "down")
    {
        CharRigDirection dir = ParseSlideDirection(direction, CharRigDirection.Down);

        var spec = new DipInOutCommandSpecCharR
        {
            targetKey = roleKey,
            dir = dir
        };

        Collect(spec);
    }

    private void EnqueueMoveBySpec(string roleKey, float x, float y)
    {
        var spec = new MoveByCommandSpecCharR
        {
            targetKey = roleKey,
            delta = new Vector2(x, y)
        };

        Collect(spec);
    }

    private void EnqueueFadeInSpec(string roleKey)
    {
        var spec = new FadeInCommandSpecCharR
        {
            targetKey = roleKey
        };

        Collect(spec);
    }

    private void EnqueueFadeOutSpec(string roleKey)
    {
        var spec = new FadeOutCommandSpecCharR
        {
            targetKey = roleKey
        };

        Collect(spec);
    }

    private void EnqueueSlideInSpec(string roleKey, string direction = "left")
    {
        CharRigDirection from = ParseSlideDirection(direction, CharRigDirection.Left);

        var spec = new SlideInCommandSpecCharR
        {
            targetKey = roleKey,
            direction = from
        };

        Collect(spec);
    }

    private void EnqueueSlideOutSpec(string roleKey, string direction = "right")
    {
        CharRigDirection to = ParseSlideDirection(direction, CharRigDirection.Right);

        var spec = new SlideOutCommandSpecCharR
        {
            targetKey = roleKey,
            to = to
        };

        Collect(spec);
    }
    private void EnqueueSetupCharRigSpec(string roleKey, string slotKey)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogError("[YarnCommandBridge] slot: roleKey is null or empty.");
            return;
        }

        if (!TryParseCharRigSlot(slotKey, out CharRigSlot parentSlot))
        {
            Debug.LogError($"[YarnCommandBridge] slot: Unknown slot key '{slotKey}'. Use 'a', 'b', 'c', or 'd'.");
            return;
        }

        var spec = new SetupCharRigCommandSpec
        {
            roleKey = roleKey.Trim(),
            parentSlot = parentSlot,
            rigPrefab = rigPrefab
        };

        Collect(spec);
    }
    
    private bool TryParseCharRigSlot(string raw, out CharRigSlot slot)
    {
        slot = default;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string s = raw.Trim().ToLowerInvariant();

        switch (s)
        {
            case "s0":
            case "0":
            case "stage00":
                slot = CharRigSlot.Stage00CharacterSlot;
                return true;

            case "s1":
            case "1":
            case "stage01":
                slot = CharRigSlot.Stage01CharacterSlot;
                return true;

            case "s2":
            case "2":
            case "stage02":
                slot = CharRigSlot.Stage02CharacterSlot;
                return true;

            case "s3":
            case "me":
            case "protagonist":
            case "protagonistslot":
            case "protagonist_slot":
            case "boxside":
                slot = CharRigSlot.ProtagonistSlot;
                return true;
        }

        return Enum.TryParse(s, true, out slot);
    }

    private void EnqueueSetAnchorOffsetSpecs(string roleKey, int x, int y)
    {
        var anchorSpec = new MoveByCommandSpecCharR
        {
            targetKey = roleKey,
            target = CharacterRigTarget.CharSlot_Anchor,
            delta = new Vector2(x, y),
            duration = 0f,
            killTween = false
        };

        var resetTrackSpec = new ApplyTrackOffsetCommandSpecCharR { targetKey = roleKey };

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
            targetKey = roleKey,
            preset = preset,
            globalTuning = globalTuning,
            roleTuningDb = roleTuningDb
        };

        var resetTrackSpec = new ApplyTrackOffsetCommandSpecCharR
        {
            targetKey = roleKey
        };

        Collect(anchorSpec);
        Collect(resetTrackSpec);
    }

    
    private void EnqueueScaleToSpec(string roleKey, float xyValue, float duration = 0.4f)
    {
        var spec = new ScaleToCommandSpecCharR
        {
            targetKey = roleKey,
            duration = duration,
            toScale = new Vector2(xyValue, xyValue)
        };

        Collect(spec);
    }

    private void EnqueueSetOriginSizeSpec(string roleKey, string scaleArg)
    {
        scaleArg = (scaleArg ?? "").Trim();

        if (TryParseFloat(scaleArg, out float absoluteScale))
        {
            EnqueueSetOriginSizeAbsoluteSpec(roleKey, absoluteScale);
            return;
        }

        CharScalePreset preset = ParseCharScalePreset(scaleArg);

        var spec = new SetOriginSizeCommandSpecCharR
        {
            targetKey = roleKey,
            preset = preset,
            globalTuning = globalTuning,
            roleTuningDb = roleTuningDb,
            multiplier = 1f
        };

        Collect(spec);
    }

    private void EnqueueSetOriginSizeAbsoluteSpec(string roleKey, float xyValue)
    {
        var spec = new SetOriginSizeCommandSpecCharR
        {
            targetKey = roleKey,

            overrideScale = true,
            scaleOverride = new Vector3(xyValue, xyValue, xyValue),

            // 아래 값들은 overrideScale=true면 실제 계산에는 사용되지 않지만,
            // Inspector/debug에서 의도를 보기 좋게 기본값을 넣어둔다.
            preset = CharScalePreset.None,
            multiplier = 1f,
            globalTuning = globalTuning,
            roleTuningDb = roleTuningDb
        };

        Collect(spec);
    }

    private static CharScalePreset ParseCharScalePreset(string value)
    {
        return value switch
        {
            "normal" => CharScalePreset.Normal,
            "small" => CharScalePreset.Small,
            "large" => CharScalePreset.Large,

            "far" => CharScalePreset.Far,
            "close" => CharScalePreset.Close,

            "exp1" => CharScalePreset.Exp1,
            "exp2" => CharScalePreset.Exp2,

            _ => CharScalePreset.Normal
        };
    }

    private static bool TryParseFloat(string value, out float result)
    {
        return float.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out result);
    }

    private void EnqueueSetPortraitCrossfadeSpec(string roleKey, string character)
    {
        var portraitIdentity = new PortraitIdentity
        {
            character = character
        };

        var spec = new SetPortraitCrossfadeCommandSpecCharR
        {
            targetKey = roleKey,
            portrait = portraitIdentity
        };

        Collect(spec);
    }

    private void EnqueueCastCharacterSpec(
        string roleKey,
        string characterKey,
        string variantKey = "")
    {
        string resolvedVariantKey = string.IsNullOrWhiteSpace(variantKey)
            ? "a"
            : variantKey.Trim();

        var castSpec = new CastCharacterCommandSpec
        {
            slotKey = roleKey,
            characterKey = characterKey,
            variantKey = resolvedVariantKey
        };

        var portraitSpec = new SetPortraitSpriteCommandSpecCharR
        {
            targetKey = roleKey,
            portrait = new PortraitIdentity
            { }
        };

        Collect(castSpec);
        Collect(portraitSpec);
    }

    private void EnqueueUncastCharacterSpec(string roleKey)
    {
        var spec = new UncastCharacterCommandSpec
        {
            targetKey = roleKey
        };

        Collect(spec);
    }

    private CharRigDirection ParseSlideDirection(string direction, CharRigDirection fallback)
    {
        switch (direction?.Trim().ToLowerInvariant())
        {
            case "left":
            case "l":
                return CharRigDirection.Left;

            case "right":
            case "r":
                return CharRigDirection.Right;

            case "up":
            case "u":
            case "top":
                return CharRigDirection.Up;

            case "down":
            case "d":
            case "bottom":
                return CharRigDirection.Down;

            default:
                return fallback;
        }
    }

    private void EnqueueSetEmotionPortraitWipeSpec(
        string targetKey,
        string emotion)
    {
        var spec = new SetEmotionPortraitWipeCommandSpec
        {
            targetKey = targetKey,
            portrait = new PortraitIdentity { }
        };

        Collect(spec);
    }
}