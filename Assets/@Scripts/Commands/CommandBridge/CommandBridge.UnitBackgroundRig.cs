using DG.Tweening;
using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueSpawnBackgroundRigSpec(
        string rigKey, 
        string spriteKey)
    {
        EnqueueSetupBackgroundRigSpec(rigKey);
        EnqueueSetBackgroundSpriteSpec(rigKey, spriteKey);
    }

    private void EnqueueSetupBackgroundRigSpec(string rigKey)
        => Collect(new SetupBackgroundRigCommandSpec
        {
            rigKey = rigKey,
            rigPrefab = _backgroundRigPrefab,
            
            stage = PresentationStageKey.Stage00,
            layer = PresentationDepthLayerKey.Far,
        });
    private void EnqueueSpawnBackgroundRigStage00Spec(
        string rigKey,
        string spriteKey,
        string layerKey = "far")
        => EnqueueSpawnBackgroundRigAtDepthSpec(rigKey, spriteKey, PresentationStageKey.Stage00, layerKey);

    private void EnqueueSpawnBackgroundRigStage01Spec(
        string rigKey,
        string spriteKey,
        string layerKey = "far")
        => EnqueueSpawnBackgroundRigAtDepthSpec(rigKey, spriteKey, PresentationStageKey.Stage01, layerKey);

    private void EnqueueSpawnBackgroundRigStage02Spec(
        string rigKey,
        string spriteKey,
        string layerKey = "far")
        => EnqueueSpawnBackgroundRigAtDepthSpec(rigKey, spriteKey, PresentationStageKey.Stage02, layerKey);

    private void EnqueueSpawnBackgroundRigAtDepthSpec(
        string rigKey,
        string spriteKey,
        PresentationStageKey stage,
        string layerKey)
    {
        EnqueueSetupBackgroundRigAtDepthSpec(
            rigKey,
            stage,
            PresentationDepthLayerKeyParser.Parse(layerKey));

        EnqueueSetBackgroundSpriteSpec(rigKey, spriteKey);
    }

    private void EnqueueSetupBackgroundRigAtDepthSpec(
        string rigKey,
        PresentationStageKey stage,
        PresentationDepthLayerKey layer)
        => Collect(new SetupBackgroundRigCommandSpec
        {
            rigKey = rigKey,
            rigPrefab = _backgroundRigPrefab,

            stage = stage,
            layer = layer
        });

    private void EnqueueSetBackgroundSpriteSpec(string rigKey, string spriteKey = "", string layerKey = "back")
        => Collect(new SetBackgroundSpriteCommandSpecBgR
        {
            rigKey = rigKey,
            spriteKey = spriteKey,
        });

    private void EnqueueSetBackgroundOriginSizeSpec(string rigKey, string scaleArg = "1")
    {
        if (!YarnNumberParser.TryParseFloat(scaleArg, out float absoluteScale))
            absoluteScale = 1f;

        var spec = new SetOriginSizeCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Size,

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
        => Collect(new SetAnchorCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Anchor,
            anchoredPosition = new Vector2(ParseSignedUnit(xToken), ParseSignedUnit(yToken)),
            rotationZ = rotationZ
        });

    private void EnqueueFadeInBackgroundDslSpec(
        string rigKey,
        string durationToken = "10fr")
        => Collect(new FadeInCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Root,
            duration = YarnDurationParser.Parse(durationToken, 0.4f),
        });

    private void EnqueueFadeOutBackgroundDslSpec(
        string rigKey,
        string durationToken = "10fr")
        => Collect(new FadeOutCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Root,
            duration = YarnDurationParser.Parse(durationToken, 0.4f),
        });
    
    private void EnqueueMoveBackgroundDslSpec(
        string rigKey,
        string xToken,
        string yToken,
        string durationToken = "10fr")
        => Collect(new MoveByCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track_Move,
            delta = new Vector2(ParseSignedUnit(xToken), ParseSignedUnit(yToken)),
            duration = YarnDurationParser.Parse(durationToken, 0.4f),
            ease = Ease.OutCubic
        });

    private void EnqueueScaleBackgroundDslSpec(
        string rigKey,
        float scale,
        string durationToken = "10fr")
        => Collect(new ScaleToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Scale,
            toScale = new Vector2(scale, scale),
            duration = YarnDurationParser.Parse(durationToken, 0.4f)
        });

    private void EnqueueSlideInBackgroundDslSpec(
        string rigKey,
        string directionKey = "left",
        string distanceToken = "12u",
        string durationToken = "13fr")
        => Collect(new SlideInCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track_Move,
            direction = BgRigDirectionParser.Parse(directionKey, CharRigDirection.Left),
            distance = YarnUnitParser.Parse(distanceToken, 12f),
            duration = YarnDurationParser.Parse(durationToken, 0.55f)
        });

    private void EnqueueSlideOutBackgroundDslSpec(
        string rigKey,
        string directionKey = "right",
        string distanceToken = "12u",
        string durationToken = "11fr")
        => Collect(new SlideOutCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track_Move,
            to = BgRigDirectionParser.Parse(directionKey, CharRigDirection.Right),
            distance = YarnUnitParser.Parse(distanceToken, 12f),
            duration = YarnDurationParser.Parse(durationToken, 0.45f)
        });

    private void EnqueueJoltBackgroundDslSpec(
        string rigKey,
        string directionKey = "right",
        string strengthToken = "0.55u",
        string durationToken = "21fr")
        => Collect(new JoltCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track_Y,
            direction = BgRigDirectionParser.Parse(directionKey, CharRigDirection.Right),
            strength = YarnUnitParser.Parse(strengthToken, 0.55f),
            duration = YarnDurationParser.Parse(durationToken, 0.88f),
            taps = 3,
            damping = 6f,
            anticipation = 3f
        });

    private void EnqueueTrembleBackgroundDslSpec(
        string rigKey,
        string directionKey = "right",
        string strengthToken = "0.2u",
        string durationToken = "29fr")
        => Collect(new TrembleCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Shake,
            direction = BgRigDirectionParser.Parse(directionKey, CharRigDirection.Right),
            strength = YarnUnitParser.Parse(strengthToken, 0.2f),
            duration = YarnDurationParser.Parse(durationToken, 1.2f)
        });

    private void EnqueueBreathBackgroundDslSpec(
        string rigKey,
        string durationToken = "99s",
        string heightToken = "0.15u",
        float breathsPerSecond = 0.2f)
        => Collect(new BreathInPlaceCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track_Move,
            duration = YarnDurationParser.Parse(durationToken, 99f),
            height = YarnUnitParser.Parse(heightToken, 0.15f),
            breathsPerSecond = breathsPerSecond
        });

    private static float ParseSignedUnit(
        string token,
        float fallbackUnits = 0f)
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
            stage = PresentationStageKey.Stage02,
            layer = PresentationDepthLayerKey.Mid
        });
        
        Collect(new ScaleToCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            target = BackgroundRigTarget.Background_Scale,
            toScale = new Vector2(0.68f, 0.68f),
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
            target = BackgroundRigTarget.Background_Mask
        });

        Collect(new SetBackgroundSpriteCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            spriteKey = "slot3bg2",
            target = BackgroundRigTarget.BackgroundSprite_Image
        });
    }
    
    private void EnqueueBackgroundCutInMotionSpec(
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
        Vector2 burstCharDelta = new Vector2(-sideDrift * 0.25f, -riseDistance * 0.22f);

        // 착지 직전 살짝 아래로 되눌린다.
        Vector2 recoilDelta = new Vector2(-sideDrift * 0.55f, -riseDistance * 0.14f);
        Vector2 recoilCharDelta = new Vector2(sideDrift * 0.11f, riseDistance * 0.028f);

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
            wait = false
        });

        Collect(new MoveByCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track_Move,
            delta = startOffset,
            duration = 0f,
            wait = false
        });

        Collect(new RotateToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Rotation,
            toEuler = new Vector3(0f, 0f, -3.5f),
            duration = 0f,
            wait = false
        });

        Collect(new ScaleToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_ActingScale,
            toScale = new Vector2(0.001f, 1f),
            duration = 0f,
            wait = false
        });
        
        Collect(new ScaleToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_ActingScale_Y,
            toScale = new Vector2(1f, 0.001f),
            duration = 0f,
            wait = false
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
            target = BackgroundRigTarget.Background_Track_Move,
            delta = burstDelta,
            duration = burstDuration,
            ease = Ease.OutCubic,
            wait = false
        });
        
        Collect(new MoveByCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_ObjectSlotRoot,
            delta = burstCharDelta,
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
            target = BackgroundRigTarget.Background_ActingScale_Y,
            toScale = new Vector2(1.02f, 1.24f),
            duration = burstDuration,
            ease = Ease.OutBack,
            wait = false
        });

        Collect(new ScaleToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_ActingScale,
            toScale = new Vector2(0.88f, 1.02f),
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
            target = BackgroundRigTarget.Background_Track_Move,
            delta = recoilDelta,
            duration = recoilDuration,
            ease = Ease.InOutSine,
            wait = false
        });
        
        Collect(new MoveByCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_ObjectSlotRoot,
            delta = recoilCharDelta,
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
            target = BackgroundRigTarget.Background_ActingScale_Y,
            toScale = new Vector2(1f, 0.93f),
            duration = burstDuration,
            ease = Ease.InOutSine,
            wait = false
        });

        Collect(new ScaleToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_ActingScale,
            toScale = new Vector2(1.075f, 1f),
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
            target = BackgroundRigTarget.Background_ObjectSlotRoot,
            delta = settleDelta,
            duration = settleDuration,
            ease = Ease.OutCubic,
            wait = false
        });
        
        Collect(new MoveByCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track_Move,
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
            target = BackgroundRigTarget.Background_ActingScale_Y,
            toScale = Vector2.one,
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