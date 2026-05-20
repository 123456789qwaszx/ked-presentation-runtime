using System.Collections;
using DG.Tweening;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge : MonoBehaviour
{
    private DialogueRunner _dialogueRunner;
    private YarnBridgePlaybackDriver _playbackDriver;
    
    public void Initialize(DialogueRunner dialogueRunner, YarnBridgePlaybackDriver playbackDriver)
    {
        _dialogueRunner = dialogueRunner;
        _playbackDriver = playbackDriver;
        
        RegisterCharSlotSetCommands();
        RegisterPlaybackOrderCommands();
        RegisterAudioCommands();
        RegisterCharRigCommands();
        RegisterCharRigActingCommands();
        
        RegisterEmojiCommands();
        RegisterPresentationCommands();
        RegisterShotCommands();
    }

    private void RegisterPlaybackOrderCommands()
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
        
        
        _dialogueRunner.AddCommandHandler<string>("blackout", EnqueueBlackoutTransitionSpec);
        _dialogueRunner.AddCommandHandler<string>("uipatch", EnqueueUIPatchSpec);
        
        _dialogueRunner.AddCommandHandler<float>("pause", EnqueueWaitSpec);
        
        // clear character rigs
        _dialogueRunner.AddCommandHandler("clearall_rigs", EnqueueClearAllCharRigRefsSpec);
        _dialogueRunner.AddCommandHandler<string>("clear_rig", EnqueueClearCharRigRefSpec);
    }

    private void RegisterCharSlotSetCommands()
    {
        _dialogueRunner.AddCommandHandler<string, string>("slot", EnqueueSetupCharRigSpec);
        _dialogueRunner.AddCommandHandler<string, string,  string, string, bool, string, string>("cast", EnqueueCastCharacterSpec);
        
        _dialogueRunner.AddCommandHandler<string, string, string, string>("face", EnqueueSetPortraitSpriteSpec);
        _dialogueRunner.AddCommandHandler<string, string, bool, bool>("place", EnqueueSetAnchorSpecs);
        _dialogueRunner.AddCommandHandler<string, string>("size", EnqueueSetOriginSizeSpec);
        
        _dialogueRunner.AddCommandHandler<string, int, int>("place_offset", EnqueueSetAnchorOffsetSpecs);
        
        _dialogueRunner.AddCommandHandler<string>("uncast", EnqueueUncastCharacterSpec);
    }

    private void RegisterAudioCommands()
    {
        _dialogueRunner.AddCommandHandler<string, float>("bgm", EnqueuePlayBgmSpec);
        _dialogueRunner.AddCommandHandler<float>("stop_bgm", EnqueueStopBgmSpec);

        _dialogueRunner.AddCommandHandler<string>("voice", EnqueuePlayVoiceSpec);
        _dialogueRunner.AddCommandHandler("stop_voice", EnqueueStopVoiceSpec);

        _dialogueRunner.AddCommandHandler<string>("sfx", EnqueuePlaySfxSpec);
        _dialogueRunner.AddCommandHandler("stop_all_sfx", EnqueueStopAllSfxSpec);
    }

    private void RegisterCharRigCommands()
    {
        _dialogueRunner.AddCommandHandler<string>("fade_in", EnqueueFadeInSpec);
        _dialogueRunner.AddCommandHandler<string, float>("fade_out", EnqueueFadeOutSpec);
        
        _dialogueRunner.AddCommandHandler<string, string>("emotion", EnqueueSetEmotionPortraitWipeSpec);
        _dialogueRunner.AddCommandHandler<string, string>("emotion_crossfade", EnqueueSetPortraitCrossfadeSpec);
        
        _dialogueRunner.AddCommandHandler<string, string>("slide_in", EnqueueSlideInSpec);
        _dialogueRunner.AddCommandHandler<string, string>("slide_out", EnqueueSlideOutSpec);
        
        _dialogueRunner.AddCommandHandler<string, float, float>("move_by", EnqueueMoveBySpec);
        _dialogueRunner.AddCommandHandler<string, float, float>("scale_to", EnqueueScaleToSpec);
        
        _dialogueRunner.AddCommandHandler<string, string>("slide_in_nudge", EnqueueSlideInJoltCombo);
        _dialogueRunner.AddCommandHandler<string>("slide_in_sway", EnqueueSlideInSwayCombo);
    }

    private void RegisterCharRigActingCommands()
    {
        _dialogueRunner.AddCommandHandler<string, float, float, float>("breathe", EnqueueBreathInPlaceSpec);
        _dialogueRunner.AddCommandHandler<string, float, float, float, float>("walk_in_place", EnqueueWalkInPlaceSpec);
        _dialogueRunner.AddCommandHandler<string, float, float, float, float>("bounce_in_place", EnqueueBounceInPlaceSpec);
        
        _dialogueRunner.AddCommandHandler<string, float, float, float, float, float, string>("tremble_pulse", EnqueueTremblePulseSpec);
        
        _dialogueRunner.AddCommandHandler<string, int, float, float>("hop", EnqueueHopSpec);
        _dialogueRunner.AddCommandHandler<string, string>("jolt", EnqueueJoltSpec);
        _dialogueRunner.AddCommandHandler<string, string>("nudge", EnqueueJoltSpecTap);
        _dialogueRunner.AddCommandHandler<string, string>("nudge_hard", EnqueueJoltSpecTapHard);
        _dialogueRunner.AddCommandHandler<string, string>("dip", EnqueueDipInOutSpec);
        
        _dialogueRunner.AddCommandHandler<string, string, float, float, int>("shake", EnqueueJoltSpecShake);
        _dialogueRunner.AddCommandHandler<string, float, float, float, string>("tremble", EnqueueTrembleSpec);
        
        _dialogueRunner.AddCommandHandler<string>("sway", EnqueueSwaySpecGentle);
        _dialogueRunner.AddCommandHandler<string>("sway_hard", EnqueueSwaySpecPendulum);
        _dialogueRunner.AddCommandHandler<string>("sway_fast", EnqueueSwaySpecFast);
        _dialogueRunner.AddCommandHandler<string>("sway_away", EnqueueSwaySpecAway);
        _dialogueRunner.AddCommandHandler<string, int>("sway_to", EnqueuePivotRotateToSpec);
        
        
    }
    
    private void EnqueueSetupCharRigSpec(string slotKey, string parentKey)
    {
        var spec = new SetupCharRigCommandSpec { roleKey = slotKey, };
        
        if (CharRigSlotParser.TryParse(parentKey, out CharRigSlot parentSlot))
            spec.parentSlot = parentSlot;
        
        Collect(spec);
    }
    
    private void EnqueueCastCharacterSpec(string slotKey, string characterKey, 
        string variantKey = "a", string emotionKey = "02",
        bool applySetup = true,
        string positionPreset = "center",
        string scaleArg = "normal"
        )
    {
        var castSpec = new CastCharacterCommandSpec
        {
            slotKey = slotKey,
            characterKey = characterKey,
            variantKey = variantKey
        };
        
        Collect(castSpec);

        EnqueueSetPortraitSpriteSpec(slotKey, characterKey, variantKey, emotionKey);
        
        if (!applySetup)
            return;

        EnqueueSetAnchorSpecs(slotKey, positionPreset);
        EnqueueSetOriginSizeSpec(slotKey, scaleArg);
    }
    
    private void EnqueueSetPortraitSpriteSpec(string slotKey, 
        string characterKey, string variantKey, string emotionKey)
    {
        var spec = new SetPortraitSpriteCommandSpecCharR
        {
            slotKey = slotKey,
            portrait = new PortraitIdentity
            {
                character = characterKey,
                variant = variantKey,
                emotion = emotionKey
            }
        };

        Collect(spec);
    }
    
    private void EnqueueSetAnchorSpecs(string slotKey, string positionPreset, bool resetSlotPos = true, bool resetCharPos = true)
    {
        CharAnchorPreset preset = CharAnchorPresetParser.Parse(positionPreset);

        var anchorSpec = new SetAnchorCommandSpecCharR
        {
            slotKey = slotKey,
            target = CharacterRigTarget.Character_CastTransform,
            preset = preset,
            resetSlotPos = resetSlotPos,
            resetCharacterPos =  resetCharPos
        };

        Collect(anchorSpec);
    }
    
    private void EnqueueSetOriginSizeSpec(string roleKey, string scaleArg)
    {
        if (YarnNumberParser.TryParseFloat(scaleArg, out float absoluteScale))
        {
            var absoluteScaleSpec = new SetOriginSizeCommandSpecCharR
            {
                slotKey = roleKey,

                overrideScale = true,
                scaleOverride = new Vector3(absoluteScale, absoluteScale, absoluteScale),

                preset = CharScalePreset.None,
            };

            Collect(absoluteScaleSpec);
            return;
        }
        
        if (!CharScalePresetParser.TryParse(scaleArg, out CharScalePreset preset))
            Debug.LogWarning($"[YarnCommandBridge] Unknown scale preset '{scaleArg}'. Fallback to '{CharScalePreset.Normal}' roleKey='{roleKey}'.");
        
        var spec = new SetOriginSizeCommandSpecCharR
        {
            slotKey = roleKey, preset = preset,
        };

        Collect(spec);
    }
    
    private void EnqueueSetAnchorOffsetSpecs(string slotKey, int x = 0, int y = 0)
    {
        var slotOffsetSpec = new MoveByCommandSpecCharR
        {
            slotKey = slotKey,
            target = CharacterRigTarget.CharSlot_Anchor,
            delta = new Vector2(x, y),
            duration = 0f
        };
        
        Collect(slotOffsetSpec);
    }

    
    // Marks the next N collected commands as wait=true.
    // This only affects Presentation/Executor playback.
    private void AwaitFor(int count = 1)
    {
        _playbackDriver.WaitNextImmediateCommands(count);
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
            slotKey = roleKey,
            degree = angle
        };

        Collect(spec);
    }

    private void EnqueueSlideInSwayCombo(string roleKey)
    {
        var spec = new HideRootLayersCommandSpecCharR
        {
            slotKey = roleKey,
            targetMask = CharRigRootMask.CharacterPortrait_Root
        };

        var spec1 = new FadeInCommandSpecCharR()
        {
            slotKey = roleKey,
            targetMask = CharRigRootMask.CharacterPortrait_Root,
            duration = 0.28f
        };

        var spec2 = new SlideInCommandSpecCharR
        {
            slotKey = roleKey,
            distance = 550f,
            duration = 0.45f
        };

        var spec3 = new DipInOutCommandSpecCharR()
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_Track_Y,
            dir = CharRigDirection.Right,
            distance = 22f,
            duration = 0.8f
        };

        var spec4 = new PunchScaleCommandSpecCharR()
        {
            slotKey = roleKey,
            strength = -0.03f,
            duration = 0.55f,
            vibrato = 3,
            elasticity = 0.45f
        };

        var spec5 = new DipInOutCommandSpecCharR()
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_Track_Y,
            dir = CharRigDirection.Down,
            distance = 12f,
            duration = 0.8f
        };

        var spec6 = new SwayCommandSpecCharR()
        {
            slotKey = roleKey,
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

    private void EnqueueSwaySpecGentle(string roleKey)
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


    private void Collect(CommandSpecBase spec)
    {
        if (spec == null)
            return;

        // if (_importSink != null)
        // {
        //     _importSink.Enqueue(spec);
        //     return;
        // }

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
            slotKey = roleKey
        };

        Collect(spec);
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

    private void EnqueueJoltSpecShake(
        string roleKey,
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
            wait = false,
            killTween = true
        };

        Collect(spec);
    }
    
    private void EnqueueTremblePulseSpec(
        string roleKey,
        float duration = 99.0f,
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

        CharRigDirection dir = CharRigDirectionParser.ParseSlideDirection(direction, CharRigDirection.Right);

        var spec = new TrembleCommandSpecCharR
        {
            slotKey = roleKey.Trim(),
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

    private void EnqueueHopSpec(
        string roleKey,
        int hopCount = 2,
        float height = 48f,
        float airWidth = 0.85f)
    {
        var spec = new HopCommandSpecCharR
        {
            slotKey = roleKey,
            hopCount = hopCount,
            height = height,
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
            slotKey = roleKey.Trim(),
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
            slotKey = roleKey.Trim(),
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
            slotKey = roleKey.Trim(),
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
        CharRigDirection dir = CharRigDirectionParser.ParseSlideDirection(direction, CharRigDirection.Down);

        var spec = new DipInOutCommandSpecCharR
        {
            slotKey = roleKey,
            dir = dir
        };

        Collect(spec);
    }

    private void EnqueueMoveBySpec(string roleKey, float x, float y)
    {
        var spec = new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharSlot_Track_Move,
            delta = new Vector2(x, y)
        };

        Collect(spec);
    }

    private void EnqueueFadeInSpec(string roleKey)
    {
        var spec = new FadeInCommandSpecCharR
        {
            slotKey = roleKey
        };

        Collect(spec);
    }

    private void EnqueueFadeOutSpec(string roleKey, float duration= 0f)
    {
        var spec = new FadeOutCommandSpecCharR
        {
            slotKey = roleKey,
            duration = duration
        };

        Collect(spec);
    }

    private void EnqueueSlideInSpec(string roleKey, string direction = "left")
    {
        CharRigDirection from = CharRigDirectionParser.ParseSlideDirection(direction, CharRigDirection.Left);

        var spec = new SlideInCommandSpecCharR
        {
            slotKey = roleKey,
            direction = from
        };

        Collect(spec);
    }

    private void EnqueueSlideOutSpec(string roleKey, string direction = "right")
    {
        CharRigDirection to = CharRigDirectionParser.ParseSlideDirection(direction, CharRigDirection.Right);

        var spec = new SlideOutCommandSpecCharR
        {
            slotKey = roleKey,
            to = to
        };

        Collect(spec);
    }

    
    private void EnqueueScaleToSpec(string roleKey, float xyValue, float duration = 0.4f)
    {
        var spec = new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_ActingScale,
            duration = duration,
            toScale = new Vector2(xyValue, xyValue)
        };

        Collect(spec);
    }


    private void EnqueueSetPortraitCrossfadeSpec(string roleKey, string character)
    {
        var portraitIdentity = new PortraitIdentity
        {
            character = character
        };

        var spec = new SetPortraitCrossfadeCommandSpecCharR
        {
            slotKey = roleKey,
            portrait = portraitIdentity
        };

        Collect(spec);
    }


    private void EnqueueUncastCharacterSpec(string roleKey)
    {
        var spec = new UncastCharacterCommandSpec
        {
            slotKey = roleKey
        };

        Collect(spec);
    }

    private void EnqueueSetEmotionPortraitWipeSpec(string targetKey, string emotion)
    {
        var spec = new SetEmotionPortraitWipeCommandSpec
        {
            slotKey = targetKey,
            portrait = new PortraitIdentity
            {
                emotion = emotion
            }
        };

        Collect(spec);
    }
}