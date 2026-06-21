using DG.Tweening;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private void BindBackgroundRig(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string>(
            "bg_spawn", EnqueueSpawnBackgroundRigSpec);
        
        runner.AddCommandHandler<string, string, string>(
            "bg_slot00", EnqueueSpawnBackgroundRigStage00Spec);
        runner.AddCommandHandler<string, string, string>(
            "bg_slot01", EnqueueSpawnBackgroundRigStage01Spec);
        runner.AddCommandHandler<string, string, string>(
            "bg_slot02", EnqueueSpawnBackgroundRigStage02Spec);

        runner.AddCommandHandler<string, string, string, float>(
            "bg_place", EnqueueSetBackgroundAnchorDslSpec);

        runner.AddCommandHandler<string, string, string>(
            "bg_sprite", EnqueueSetBackgroundSpriteSpec);

        runner.AddCommandHandler<string, string>(
            "bg_size", EnqueueSetBackgroundOriginSizeSpec);

        runner.AddCommandHandler<string, string>(
            "bg_fade_in", EnqueueFadeInBackgroundDslSpec);

        runner.AddCommandHandler<string, string>(
            "bg_fade_out", EnqueueFadeOutBackgroundDslSpec);

        runner.AddCommandHandler<string, string>(
            "bg_hide_layers", EnqueueHideBackgroundRootLayersSpec);

        runner.AddCommandHandler<string, string>(
            "bg_show_layers", EnqueueShowBackgroundRootLayersSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "bg_move", EnqueueMoveBackgroundDslSpec);

        runner.AddCommandHandler<string, float, string>(
            "bg_scale", EnqueueScaleBackgroundDslSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "bg_slide_in", EnqueueSlideInBackgroundDslSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "bg_slide_out", EnqueueSlideOutBackgroundDslSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "bg_jolt", EnqueueJoltBackgroundDslSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "bg_idle_tremble", EnqueueTrembleBackgroundDslSpec);

        runner.AddCommandHandler<string, string, string, float>(
            "bg_idle_breath", EnqueueBreathBackgroundDslSpec);
        
        runner.AddCommandHandler<string>(
            "bg_slot_cutin", EnqueueBackgroundCutInSpec);
        
        runner.AddCommandHandler<string, string, string, string>(
            "bg_cutin_in", EnqueueBackgroundCutInMotionSpec);
        
        runner.AddCommandHandler<string, string, string, string>(
            "bg_cutin_in2", EnqueueBackgroundCutInMotionSpec2);
    }
    
    private void EnqueueSpawnBackgroundRigSpec(
        string rigKey, 
        string spriteKey,
        string parentSlotKey = "stage00")
    {
        EnqueueSetupBackgroundRigSpec(rigKey, parentSlotKey);
        EnqueueSetBackgroundSpriteSpec(rigKey, spriteKey);
    }

    private void EnqueueSetupBackgroundRigSpec(string rigKey, string parentSlotKey)
    {
        var spec = new SetupBackgroundRigCommandSpec
        {
            rigKey = rigKey,
            rigPrefab = _backgroundRigPrefab,
            parentSlot = BackgroundRigSlotParser.Parse(parentSlotKey, BackgroundRigSlot.Stage00BackgroundSlot)
        };

        Collect(spec);
    }
    private void EnqueueSpawnBackgroundRigStage00Spec(
        string rigKey,
        string spriteKey,
        string layerKey = "far")
    {
        EnqueueSpawnBackgroundRigAtDepthSpec(
            rigKey,
            spriteKey,
            PresentationStageKey.Stage00,
            layerKey);
    }

    private void EnqueueSpawnBackgroundRigStage01Spec(
        string rigKey,
        string spriteKey,
        string layerKey = "far")
    {
        EnqueueSpawnBackgroundRigAtDepthSpec(
            rigKey,
            spriteKey,
            PresentationStageKey.Stage01,
            layerKey);
    }

    private void EnqueueSpawnBackgroundRigStage02Spec(
        string rigKey,
        string spriteKey,
        string layerKey = "far")
    {
        EnqueueSpawnBackgroundRigAtDepthSpec(
            rigKey,
            spriteKey,
            PresentationStageKey.Stage02,
            layerKey);
    }

    private void EnqueueSpawnBackgroundRigAtDepthSpec(
        string rigKey,
        string spriteKey,
        PresentationStageKey stage,
        string layerKey)
    {
        EnqueueSetupBackgroundRigAtDepthSpec(
            rigKey,
            stage,
            PresentationDepthLayerKeyParser.Parse(
                layerKey,
                PresentationDepthLayerKey.Far));

        EnqueueSetBackgroundSpriteSpec(rigKey, spriteKey);
    }

    private void EnqueueSetupBackgroundRigAtDepthSpec(
        string rigKey,
        PresentationStageKey stage,
        PresentationDepthLayerKey layer)
    {
        var spec = new SetupBackgroundRigCommandSpec
        {
            rigKey = rigKey,
            rigPrefab = _backgroundRigPrefab,

            useStageDepthSlot = true,
            stage = stage,
            layer = layer
        };

        Collect(spec);
    }

    private void EnqueueSetBackgroundSpriteSpec(string rigKey, string spriteKey = "", string layerKey = "back")
    {
        var spec = new SetBackgroundSpriteCommandSpecBgR
        {
            rigKey = rigKey,
            spriteKey = spriteKey,
            target = BackgroundRigLayerParser.ParseImageTarget(layerKey)
        };

        Collect(spec);
    }

    private void EnqueueSetBackgroundOriginSizeSpec(string rigKey, string scaleArg = "1")
    {
        if (!YarnNumberParser.TryParseFloat(scaleArg, out float absoluteScale))
            absoluteScale = 1f;

        var spec = new SetOriginSizeCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_CastTransform,

            overrideScale = true,
            scaleOverride = new Vector3(absoluteScale, absoluteScale, absoluteScale)
        };

        Collect(spec);
    }

    private void EnqueueSetBackgroundAnchorDslSpec(
        string rigKey,
        string xToken = "0u",
        string yToken = "0u",
        float rotationZ = 0f)
    {
        var spec = new SetAnchorCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_CastTransform,
            anchoredPosition = new Vector2(
                ParseSignedUnit(xToken, 0f),
                ParseSignedUnit(yToken, 0f)),
            rotationZ = rotationZ
        };

        Collect(spec);
    }

    private void EnqueueFadeInBackgroundDslSpec(
        string rigKey,
        string durationToken = "10fr")
    {
        var spec = new FadeInCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Root,
            duration = YarnDurationParser.Parse(durationToken, 0.4f),
        };

        Collect(spec);
    }

    private void EnqueueFadeOutBackgroundDslSpec(
        string rigKey,
        string durationToken = "10fr")
    {
        var spec = new FadeOutCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Root,
            duration = YarnDurationParser.Parse(durationToken, 0.4f),
        };

        Collect(spec);
    }
    
    private void EnqueueHideBackgroundRootLayersSpec(string rigKey, string mask = "visual")
    {
        var spec = new HideRootLayersCommandSpecBgR
        {
            rigKey = rigKey,
            targetMask = BackgroundRigRootMaskParser.Parse(mask),
        };

        Collect(spec);
    }

    private void EnqueueShowBackgroundRootLayersSpec(string rigKey, string mask = "visual")
    {
        var spec = new ShowRootLayersCommandSpecBgR
        {
            rigKey = rigKey,
            targetMask = BackgroundRigRootMaskParser.Parse(mask),
        };

        Collect(spec);
    }

    private void EnqueueMoveBackgroundDslSpec(
        string rigKey,
        string xToken,
        string yToken,
        string durationToken = "10fr")
    {
        var spec = new MoveByCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track,
            delta = new Vector2(
                ParseSignedUnit(xToken, 0f),
                ParseSignedUnit(yToken, 0f)),
            duration = YarnDurationParser.Parse(durationToken, 0.4f),
            ease = Ease.OutCubic
        };

        Collect(spec);
    }

    private void EnqueueScaleBackgroundDslSpec(
        string rigKey,
        float scale,
        string durationToken = "10fr")
    {
        var spec = new ScaleToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_ActingScale,
            toScale = new Vector2(scale, scale),
            duration = YarnDurationParser.Parse(durationToken, 0.4f)
        };

        Collect(spec);
    }

    private void EnqueueSlideInBackgroundDslSpec(
        string rigKey,
        string directionKey = "left",
        string distanceToken = "12u",
        string durationToken = "13fr")
    {
        var spec = new SlideInCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track,
            direction = BgRigDirectionParser.Parse(directionKey, CharRigDirection.Left),
            distance = YarnUnitParser.Parse(distanceToken, 12f),
            duration = YarnDurationParser.Parse(durationToken, 0.55f)
        };

        Collect(spec);
    }

    private void EnqueueSlideOutBackgroundDslSpec(
        string rigKey,
        string directionKey = "right",
        string distanceToken = "12u",
        string durationToken = "11fr")
    {
        var spec = new SlideOutCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track,
            to = BgRigDirectionParser.Parse(directionKey, CharRigDirection.Right),
            distance = YarnUnitParser.Parse(distanceToken, 12f),
            duration = YarnDurationParser.Parse(durationToken, 0.45f)
        };

        Collect(spec);
    }

    private void EnqueueJoltBackgroundDslSpec(
        string rigKey,
        string directionKey = "right",
        string strengthToken = "0.55u",
        string durationToken = "21fr")
    {
        var spec = new JoltCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track_Y,
            direction = BgRigDirectionParser.Parse(directionKey, CharRigDirection.Right),
            strength = YarnUnitParser.Parse(strengthToken, 0.55f),
            duration = YarnDurationParser.Parse(durationToken, 0.88f),
            taps = 3,
            damping = 6f,
            anticipation = 3f
        };

        Collect(spec);
    }

    private void EnqueueTrembleBackgroundDslSpec(
        string rigKey,
        string directionKey = "right",
        string strengthToken = "0.2u",
        string durationToken = "29fr")
    {
        var spec = new TrembleCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Shake,
            direction = BgRigDirectionParser.Parse(directionKey, CharRigDirection.Right),
            strength = YarnUnitParser.Parse(strengthToken, 0.2f),
            duration = YarnDurationParser.Parse(durationToken, 1.2f)
        };

        Collect(spec);
    }

    private void EnqueueBreathBackgroundDslSpec(
        string rigKey,
        string durationToken = "99s",
        string heightToken = "0.15u",
        float breathsPerSecond = 0.2f)
    {
        var spec = new BreathInPlaceCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track,
            duration = YarnDurationParser.Parse(durationToken, 99f),
            height = YarnUnitParser.Parse(heightToken, 0.15f),
            breathsPerSecond = breathsPerSecond
        };

        Collect(spec);
    }

    private static float ParseSignedUnit(
        string token,
        float fallbackUnits)
    {
        if (string.IsNullOrWhiteSpace(token))
            return YarnUnitParser.Parse(token, fallbackUnits);

        string trimmed = token.Trim();

        if (trimmed.StartsWith("-", System.StringComparison.Ordinal))
            return -YarnUnitParser.Parse(trimmed[1..], Mathf.Abs(fallbackUnits));

        if (trimmed.StartsWith("+", System.StringComparison.Ordinal))
            return YarnUnitParser.Parse(trimmed[1..], Mathf.Abs(fallbackUnits));

        return YarnUnitParser.Parse(trimmed, fallbackUnits);
    }
    
    private void EnqueueBackgroundCutInSpec(
        string backgroundRigKey)
    {
        Collect( new SetupBackgroundRigCommandSpec
        {
            rigKey = backgroundRigKey,
            rigPrefab = _backgroundRigPrefab,
            parentSlot = BackgroundRigSlotParser.Parse("stage02", BackgroundRigSlot.Stage02BackgroundSlot)
        });
        
        Collect(new ScaleToCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            target = BackgroundRigTarget.Background_CastTransform,
            toScale = new Vector2(0.5f, 0.5f),
            duration = 0
        });

        Collect(new ScaleToCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            target = BackgroundRigTarget.Background_ObjectSlotRoot,
            toScale = new Vector2(2f, 2f),
            duration = 0
        });

        Collect(new MoveByCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            target = BackgroundRigTarget.Background_ObjectSlotRoot,
            delta = new Vector2(0, -380),
            duration = 0
        });

        Collect(new SetBackgroundSpriteCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            spriteKey = "slot3bg",
            target = BackgroundRigTarget.Background_LayerRoot
        });

        Collect(new SetBackgroundSpriteCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            spriteKey = "slot3bg2",
            target = BackgroundRigTarget.Background_BackLayer_Image
        });

        // Cut-in slot visibility:
        // hide front/root layers, show object slot layer.
        Collect(new HideRootLayersCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            targetMask = BackgroundRigRootMask.Background_FrontLayer_Root,
            wait = false
        });

        Collect(new HideRootLayersCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            targetMask = BackgroundRigRootMask.Background_Root,
            wait = false
        });

        Collect(new ShowRootLayersCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            targetMask = BackgroundRigRootMask.Background_ObjectSlotRoot,
            wait = false
        });
    }
    
    private void EnqueueBackgroundCutInMotionSpec(
        string rigKey,
        string xToken = "1.2u",
        string yToken = "0u",
        string durationToken = "16fr")
    {
        Vector2 travel = new(
            ParseSignedUnit(xToken, 1.2f),
            ParseSignedUnit(yToken, 0f));

        float duration = YarnDurationParser.Parse(durationToken, 0.66f);

        float burstDuration = duration * 0.52f;
        float recoilDuration = duration * 0.20f;
        float settleDuration = duration * 0.28f;

        float fadeDuration = Mathf.Clamp(duration * 0.22f, 0.08f, 0.18f);

        Vector2 startOffset = -travel;
        Vector2 burstDelta = travel * 1.10f;
        Vector2 recoilDelta = -travel * 0.13f;
        Vector2 settleDelta = travel * 0.03f;

        // --------------------------------------------------------------------
        // 0. Initial pose
        // --------------------------------------------------------------------
        // bg_cutin이 만든 cut-in slot을 실제 연출 시작 전 숨겨둔다.
        // 현재 위치를 최종 착지 위치로 보고, Track만 뒤로 빼서 시작점을 만든다.

        Collect(new FadeOutCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_ObjectSlotRoot,
            duration = 0f,
            wait = true
        });

        Collect(new MoveByCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track,
            delta = startOffset,
            duration = 0f,
            wait = true
        });

        Collect(new RotateToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Rotation,
            toEuler = new Vector3(0f, 0f, -7.5f),
            duration = 0f,
            wait = true
        });

        Collect(new ScaleToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_ActingScale,
            toScale = new Vector2(0.035f, 0.035f),
            duration = 0f,
            wait = true
        });


        // --------------------------------------------------------------------
        // 1. Burst in
        // --------------------------------------------------------------------
        // 작은 점에서 커지면서 좌 -> 우로 튀어나온다.
        // scale은 살짝 가로로 늘어나는 느낌을 준다.

        Collect(new FadeInCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_ObjectSlotRoot,
            duration = fadeDuration,
            wait = false
        });

        Collect(new MoveByCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track,
            delta = burstDelta,
            duration = burstDuration,
            ease = Ease.OutCubic,
            wait = false
        });

        Collect(new RotateToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Rotation,
            toEuler = new Vector3(0f, 0f, 4f),
            duration = burstDuration * 0.86f,
            ease = Ease.OutCubic,
            wait = false
        });

        Collect(new ScaleToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_ActingScale,
            toScale = new Vector2(1.12f, 0.96f),
            duration = burstDuration,
            ease = Ease.OutBack,
            wait = true
        });


        // --------------------------------------------------------------------
        // 2. Sticky recoil
        // --------------------------------------------------------------------
        // 지나친 힘을 살짝 되감는다.
        // 여기서 쫀득쫀득한 느낌이 생긴다.

        Collect(new MoveByCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track,
            delta = recoilDelta,
            duration = recoilDuration,
            ease = Ease.InOutSine,
            wait = false
        });

        Collect(new RotateToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Rotation,
            toEuler = new Vector3(0f, 0f, -1.5f),
            duration = recoilDuration,
            ease = Ease.InOutSine,
            wait = false
        });

        Collect(new ScaleToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_ActingScale,
            toScale = new Vector2(0.985f, 1.035f),
            duration = recoilDuration,
            ease = Ease.InOutSine,
            wait = true
        });


        // --------------------------------------------------------------------
        // 3. Settle
        // --------------------------------------------------------------------
        // 최종 위치, 회전, 스케일로 부드럽게 착지한다.

        Collect(new MoveByCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track,
            delta = settleDelta,
            duration = settleDuration,
            ease = Ease.OutCubic,
            wait = false
        });

        Collect(new RotateToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Rotation,
            toEuler = Vector3.zero,
            duration = settleDuration,
            ease = Ease.OutCubic,
            wait = false
        });

        Collect(new ScaleToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_ActingScale,
            toScale = Vector2.one,
            duration = settleDuration,
            ease = Ease.OutCubic,
            wait = true
        });
    }
    
    private void EnqueueBackgroundCutInMotionSpec2(
        string rigKey,
        string xToken = "0.18u",
        string yToken = "9.65u",
        string durationToken = "12fr")
    {
        float sideDrift = ParseSignedUnit(xToken, 0.18f);

        float riseDistance = Mathf.Abs(ParseSignedUnit(yToken, 2.65f));
        if (riseDistance <= 0.001f)
            riseDistance = YarnUnitParser.Parse("2.65u", 2.65f);

        float duration = YarnDurationParser.Parse(durationToken, 0.75f);

        float burstDuration = duration * 0.50f;
        float recoilDuration = duration * 0.22f;
        float settleDuration = duration * 0.28f;

        float fadeDuration = Mathf.Clamp(duration * 0.18f, 0.07f, 0.16f);

        // 아래쪽의 작은 점에서 시작한다.
        // x는 크게 밀지 않고, 생각이 튀어나오면서 살짝 옆으로 새는 정도만 준다.
        Vector2 startOffset = new Vector2(-sideDrift * 0.45f, -riseDistance);

        // 최종 위치보다 살짝 위까지 튀어나온다.
        Vector2 burstDelta = new Vector2(sideDrift * 1.25f, riseDistance * 1.10f);

        // 착지 직전 살짝 아래로 되눌린다.
        Vector2 recoilDelta = new Vector2(-sideDrift * 0.55f, -riseDistance * 0.14f);

        // 누적 오차 없이 정확히 원래 위치로 돌아오게 한다.
        Vector2 settleDelta = -(startOffset + burstDelta + recoilDelta);

        // --------------------------------------------------------------------
        // 0. Initial pose
        // --------------------------------------------------------------------
        // 완전히 아래쪽의 작은 점.
        // 내면의 생각이 화면 아래에서 응축되어 있다가 튀어나오는 시작 상태.

        Collect(new FadeOutCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_ObjectSlotRoot,
            duration = 0f,
            wait = true
        });

        Collect(new MoveByCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track,
            delta = startOffset,
            duration = 0f,
            wait = true
        });

        Collect(new RotateToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Rotation,
            toEuler = new Vector3(0f, 0f, -3.5f),
            duration = 0f,
            wait = true
        });

        Collect(new ScaleToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_ActingScale,
            toScale = new Vector2(0.001f, 0.001f),
            duration = 0f,
            wait = true
        });


        // --------------------------------------------------------------------
        // 1. Pop up
        // --------------------------------------------------------------------
        // 아래쪽 점에서 위로 툭 솟아오른다.
        // 이동은 위쪽이 주축이고, x는 살짝만 섞는다.
        // scale은 살짝 세로로 늘어나며 튀어나오는 느낌.

        Collect(new FadeInCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_ObjectSlotRoot,
            duration = fadeDuration,
            wait = false
        });

        Collect(new MoveByCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track,
            delta = burstDelta,
            duration = burstDuration,
            ease = Ease.OutCubic,
            wait = false
        });

        Collect(new RotateToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Rotation,
            toEuler = new Vector3(0f, 0f, 2.2f),
            duration = burstDuration,
            ease = Ease.OutCubic,
            wait = false
        });

        Collect(new ScaleToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_ActingScale,
            toScale = new Vector2(0.94f, 1.16f),
            duration = burstDuration,
            ease = Ease.OutBack,
            wait = true
        });


        // --------------------------------------------------------------------
        // 2. Thought bubble squash
        // --------------------------------------------------------------------
        // 위로 튀어나온 힘이 살짝 눌리면서 말랑하게 반동한다.
        // "툭" 나온 뒤에 한 박자 씹히는 느낌.

        Collect(new MoveByCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track,
            delta = recoilDelta,
            duration = recoilDuration,
            ease = Ease.InOutSine,
            wait = false
        });

        Collect(new RotateToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Rotation,
            toEuler = new Vector3(0f, 0f, -0.8f),
            duration = recoilDuration,
            ease = Ease.InOutSine,
            wait = false
        });

        Collect(new ScaleToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_ActingScale,
            toScale = new Vector2(1.075f, 0.965f),
            duration = recoilDuration,
            ease = Ease.InOutSine,
            wait = true
        });


        // --------------------------------------------------------------------
        // 3. Settle
        // --------------------------------------------------------------------
        // 생각 컷인이 제자리에 부드럽게 안착한다.

        Collect(new MoveByCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track,
            delta = settleDelta,
            duration = settleDuration,
            ease = Ease.OutCubic,
            wait = false
        });

        Collect(new RotateToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Rotation,
            toEuler = Vector3.zero,
            duration = settleDuration,
            ease = Ease.OutCubic,
            wait = false
        });

        Collect(new ScaleToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_ActingScale,
            toScale = Vector2.one,
            duration = settleDuration,
            ease = Ease.OutCubic,
            wait = true
        });
    }
}