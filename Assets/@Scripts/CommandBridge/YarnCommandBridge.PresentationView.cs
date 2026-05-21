using DG.Tweening;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    public void RegisterPresentationCommands()
    {
        _dialogueRunner.AddCommandHandler("presentation_setup", EnqueueSetupPresentationViewSpec);

        _dialogueRunner.AddCommandHandler<string, string, string>("bg_spawn", EnqueueSpawnBackgroundSpec);
        _dialogueRunner.AddCommandHandler<string, string, string, string>(
            "bg_spawn_bound",
            EnqueueSpawnBackgroundBoundSpec);

        _dialogueRunner.AddCommandHandler<string, string>("bgsprite", EnqueueSetBackgroundSpriteSpec);
        _dialogueRunner.AddCommandHandler<string>("bg_destroy", EnqueueDestroyBackgroundSpec);
        _dialogueRunner.AddCommandHandler<string, float, float>("bg_fade", EnqueueFadeBackgroundSpec);


        // PresentationTarget direct transform
        _dialogueRunner.AddCommandHandler<string>("p_reset", EnqueueResetPresentationTargetSpec);

        _dialogueRunner.AddCommandHandler<string, float>("p_slant_cut_in", EnqueueSlantedMaskCutInSpec);
        _dialogueRunner.AddCommandHandler<string, float>("p_slant_cut_out", EnqueueSlantedMaskCutOutSpec);

        _dialogueRunner.AddCommandHandler<string, float>("p_strip_cover", EnqueueVerticalStripCoverSpec);
        _dialogueRunner.AddCommandHandler<string, float>("p_strip_clear", EnqueueVerticalStripClearSpec);

        _dialogueRunner.AddCommandHandler<string, float>("p_shutter_close", EnqueueSlantedShutterCloseSpec);
        _dialogueRunner.AddCommandHandler<string, float>("p_shutter_open", EnqueueSlantedShutterOpenSpec);

        _dialogueRunner.AddCommandHandler<string, float>("p_focus_fade_out", EnqueueFocusBlurFadeOutSpec);
        _dialogueRunner.AddCommandHandler<string, float>("p_focus_fade_in", EnqueueFocusBlurFadeInSpec);

        _dialogueRunner.AddCommandHandler<string, float>("p_focus_curtain_close", EnqueueFocusBlurCurtainCloseSpec);
        _dialogueRunner.AddCommandHandler<string, float>("p_focus_curtain_open", EnqueueFocusBlurCurtainOpenSpec);

        _dialogueRunner.AddCommandHandler<string, float>("p_daze_fade_close", EnqueueDazeFadeCloseSpec);
        _dialogueRunner.AddCommandHandler<string, float>("p_daze_fade_open", EnqueueDazeFadeOpenSpec);

        _dialogueRunner.AddCommandHandler<string, float>("p_light_sweep", EnqueueLightSweepSpec);
        _dialogueRunner.AddCommandHandler<string, string, float>("p_light_sweep_dir", EnqueueLightSweepDirSpec);

        _dialogueRunner.AddCommandHandler("box_hide", EnqueueHideDialogueBoxSpec);
    }

    private void EnqueueLightSweepSpec(
        string targetName,
        float duration = 0.72f)
    {
        EnqueueLightSweepDirSpec(targetName, "right", duration);
    }

    private void EnqueueLightSweepDirSpec(
        string targetName,
        string directionName,
        float duration = 0.72f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_light_sweep: Unknown target '{targetName}'.");
            return;
        }

        LightSweepDirection direction = ParseLightSweepDirection(directionName);

        var spec = new LightSweepCommandSpec
        {
            target = target,
            direction = direction,

            // 넓은 예고광.
            // 얇은 선이 아니라, 화면 한쪽에서 빛 덩어리가 밀려오는 느낌을 만든다.
            broadGlowWidth = 1040f,

            // 중앙의 강한 하이라이트.
            // 너무 얇으면 줄넘기처럼 보이고, 너무 넓으면 그냥 흰 패널처럼 보인다.
            coreWidth = 190f,

            // 지나간 뒤 남는 잔광.
            // 이 레이어가 있어야 장면이 "빛에 씻겨 나간" 느낌이 난다.
            trailGlowWidth = 720f,

            // 사선 기울기.
            // 값이 커질수록 평면 wipe가 아니라 빛이 비스듬히 베고 지나가는 느낌이 난다.
            slantPixels = 380f,

            // 화면 밖에서 충분히 시작하고 충분히 빠져나가게 한다.
            // 이 값이 작으면 화면 가장자리에서 갑자기 생기는 느낌이 난다.
            extraTravel = 820f,

            // 살짝 따뜻한 금빛.
            // 완전 흰색보다 VN 전환용으로 더 부드럽고 화사하다.
            color = new Color(1f, 0.945f, 0.74f, 1f),

            // 0.00 ~ 0.20 : 먼저 들어오는 부드러운 빛 안개.
            broadGlowAlpha = 0.38f,

            // 0.20 ~ 0.55 : 중앙을 훑는 강한 빛줄기.
            coreAlpha = 1f,

            // 0.55 ~ 1.00 : 뒤에 남는 잔광.
            trailGlowAlpha = 0.28f,

            // 0.42 ~ 0.62 : 화면 전체가 순간적으로 번지는 플래시.
            flashAlpha = 0.62f,

            // BroadGlow가 먼저 들어온다.
            broadStart = 0f,
            broadEnd = 0.20f,

            // Core가 화면 중앙을 훑는다.
            coreStart = 0.20f,
            coreEnd = 0.55f,

            // Fullscreen Flash가 순간적으로 올라왔다 내려간다.
            flashStart = 0.42f,
            flashPeak = 0.52f,
            flashEnd = 0.62f,

            // TrailGlow가 빠지면서 장면이 드러난다.
            trailStart = 0.55f,
            trailEnd = 1f,

            // 전체 tween은 시간축으로만 쓴다.
            // 각 레이어 ease는 Command 내부 ApplyTimeline에서 따로 처리한다.
            duration = duration,
            ease = Ease.Linear,

            wait = true,
            killTween = true,
            disableOnComplete = true,
            blockRaycastWhileSweeping = false,
            strict = true
        };

        Collect(spec);
    }

    private LightSweepDirection ParseLightSweepDirection(string directionName)
    {
        if (string.IsNullOrWhiteSpace(directionName))
            return LightSweepDirection.LeftToRight;

        string key = directionName.Trim().ToLowerInvariant();

        switch (key)
        {
            case "left":
            case "right_to_left":
            case "rtl":
                return LightSweepDirection.RightToLeft;

            case "right":
            case "left_to_right":
            case "ltr":
            default:
                return LightSweepDirection.LeftToRight;
        }
    }

    private void EnqueueDazeFadeCloseSpec(
        string targetName,
        float duration = 0.85f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_daze_fade_close: Unknown target '{targetName}'.");
            return;
        }

        var spec = new FocusBlurCurtainCommandSpec
        {
            target = target,

            mode = FocusBlurCurtainMode.Close,

            // 화면 중앙이 오래 남도록 gap을 크게 둔다.
            openGapHeight = 680f,
            finalGapHeight = 0f,

            // 셔터 느낌을 줄이기 위해 사선은 약하게.
            slantPixels = 36f,

            // 위아래 경계가 딱 잘리는 느낌보다, 부드럽게 번지는 느낌.
            edgeFeatherHeight = 240f,
            edgeFeatherAlpha = 0.42f,

            // 중앙 흐림 영역을 넓게 잡아서 "멍해지는" 느낌을 만든다.
            centerBlurHeight = 520f,
            centerStartAlpha = 0.08f,
            centerEndAlpha = 0.72f,
            centerBlurSlices = 28,

            color = Color.black,

            // 천천히 멍해지는 전환.
            duration = duration,
            ease = Ease.InOutSine,

            wait = true,
            killTween = true,
            disableWhenOpen = true,
            blockRaycastWhenClosed = false,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueDazeFadeOpenSpec(
        string targetName,
        float duration = 0.65f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_daze_fade_open: Unknown target '{targetName}'.");
            return;
        }

        var spec = new FocusBlurCurtainCommandSpec
        {
            target = target,

            mode = FocusBlurCurtainMode.Open,

            openGapHeight = 680f,
            finalGapHeight = 0f,
            slantPixels = 36f,

            edgeFeatherHeight = 240f,
            edgeFeatherAlpha = 0.42f,

            centerBlurHeight = 520f,
            centerStartAlpha = 0.08f,
            centerEndAlpha = 0.72f,
            centerBlurSlices = 28,

            color = Color.black,

            duration = duration,
            ease = Ease.InOutSine,

            wait = true,
            killTween = true,
            disableWhenOpen = true,
            blockRaycastWhenClosed = false,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueFocusBlurCurtainCloseSpec(
        string targetName,
        float duration = 0.55f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_focus_curtain_close: Unknown target '{targetName}'.");
            return;
        }

        var spec = new FocusBlurCurtainCommandSpec
        {
            target = target,

            mode = FocusBlurCurtainMode.Close,

            openGapHeight = 520f,
            finalGapHeight = 0f,
            slantPixels = 90f,

            edgeFeatherHeight = 140f,
            edgeFeatherAlpha = 0.55f,

            centerBlurHeight = 320f,
            centerStartAlpha = 0.12f,
            centerEndAlpha = 0.82f,
            centerBlurSlices = 18,

            color = Color.black,

            duration = duration,
            ease = Ease.InOutCubic,

            wait = true,
            killTween = true,
            disableWhenOpen = true,
            blockRaycastWhenClosed = false,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueFocusBlurCurtainOpenSpec(
        string targetName,
        float duration = 0.42f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_focus_curtain_open: Unknown target '{targetName}'.");
            return;
        }

        var spec = new FocusBlurCurtainCommandSpec
        {
            target = target,

            mode = FocusBlurCurtainMode.Open,

            openGapHeight = 520f,
            finalGapHeight = 0f,
            slantPixels = 90f,

            edgeFeatherHeight = 140f,
            edgeFeatherAlpha = 0.55f,

            centerBlurHeight = 320f,
            centerStartAlpha = 0.12f,
            centerEndAlpha = 0.82f,
            centerBlurSlices = 18,

            color = Color.black,

            duration = duration,
            ease = Ease.InOutCubic,

            wait = true,
            killTween = true,
            disableWhenOpen = true,
            blockRaycastWhenClosed = false,
            strict = true
        };

        Collect(spec);
    }


    private void EnqueueFocusBlurFadeOutSpec(
        string targetName,
        float duration = 0.45f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_focus_fade_out: Unknown target '{targetName}'.");
            return;
        }

        var spec = new FocusBlurFadeCommandSpec
        {
            target = target,

            mode = FocusBlurFadeMode.FadeOut,

            color = Color.black,
            maxAlpha = 1f,
            zoomAmount = 0.035f,

            duration = duration,
            ease = Ease.InOutCubic,

            wait = true,
            killTween = true,
            disableWhenClear = true,
            blockRaycastWhenVisible = false,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueFocusBlurFadeInSpec(
        string targetName,
        float duration = 0.35f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_focus_fade_in: Unknown target '{targetName}'.");
            return;
        }

        var spec = new FocusBlurFadeCommandSpec
        {
            target = target,

            mode = FocusBlurFadeMode.FadeIn,

            color = Color.black,
            maxAlpha = 1f,
            zoomAmount = 0.035f,

            duration = duration,
            ease = Ease.InOutCubic,

            wait = true,
            killTween = true,
            disableWhenClear = true,
            blockRaycastWhenVisible = false,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueSlantedShutterCloseSpec(
        string targetName,
        float duration = 0.38f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_shutter_close: Unknown target '{targetName}'.");
            return;
        }

        var spec = new SlantedShutterCommandSpec
        {
            target = target,

            mode = SlantedShutterMode.Close,

            slantPixels = 140f,
            openGapHeight = 460f,
            finalGapHeight = 0f,

            centerBandHeight = 280f,
            centerStartAlpha = 0.25f,
            centerEndAlpha = 1f,

            color = Color.black,

            duration = duration,
            ease = Ease.OutCubic,

            wait = true,
            killTween = true,
            disableWhenOpen = true,
            blockRaycastWhileClosed = false,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueSlantedShutterOpenSpec(
        string targetName,
        float duration = 0.32f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_shutter_open: Unknown target '{targetName}'.");
            return;
        }

        var spec = new SlantedShutterCommandSpec
        {
            target = target,

            mode = SlantedShutterMode.Open,

            slantPixels = 140f,
            openGapHeight = 460f,
            finalGapHeight = 0f,

            centerBandHeight = 280f,
            centerStartAlpha = 0.25f,
            centerEndAlpha = 1f,

            color = Color.black,

            duration = duration,
            ease = Ease.InCubic,

            wait = true,
            killTween = true,
            disableWhenOpen = true,
            blockRaycastWhileClosed = false,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueVerticalStripCoverSpec(
        string targetName,
        float duration = 0f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_strip_cover: Unknown target '{targetName}'.");
            return;
        }

        var spec = new VerticalStripWipeCommandSpec
        {
            target = target,

            mode = VerticalStripWipeMode.Cover,
            order = VerticalStripWipeOrder.LeftToRight,

            stripCount = 20,
            stripDelay = 0.02f,
            stripFillDuration = 0.08f,

            color = Color.black,

            duration = duration,
            ease = Ease.Linear,

            wait = true,
            killTween = true,
            disableWhenClear = true,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueVerticalStripClearSpec(
        string targetName,
        float duration = 0f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_strip_clear: Unknown target '{targetName}'.");
            return;
        }

        var spec = new VerticalStripWipeCommandSpec
        {
            target = target,

            mode = VerticalStripWipeMode.Clear,
            order = VerticalStripWipeOrder.RightToLeft,

            stripCount = 20,
            stripDelay = 0.02f,
            stripFillDuration = 0.08f,

            color = Color.black,

            duration = duration,
            ease = Ease.Linear,

            wait = true,
            killTween = true,
            disableWhenClear = true,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueSetupPresentationViewSpec()
    {
        var spec = new SetupPresentationViewCommandSpec
        {
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueHideDialogueBoxSpec()
    {
        var spec = new HideDialogueBoxCommandSpec
        {
            hideAll = true,
            targetKind = DialogueBoxKind.Speaker,
            duration = 0.18f,
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueSpawnBackgroundSpec(string bgKey, string viewPrefabKey, string stageKey)
    {
        if (string.IsNullOrWhiteSpace(bgKey))
        {
            Debug.LogError("[YarnCommandBridge] bg_spawn: bgKey is null or empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(viewPrefabKey))
        {
            Debug.LogError("[YarnCommandBridge] bg_spawn: viewPrefabKey is null or empty.");
            return;
        }

        if (!PresentationTargetParser.TryParseBackgroundStageContent(stageKey, out PresentationTarget parentTarget))
        {
            Debug.LogError($"[YarnCommandBridge] bg_spawn: Unknown stage key '{stageKey}'. Use 's0', 's1', or 's2'.");
            return;
        }

        var spec = new SpawnBackgroundCommandSpec
        {
            bgKey = bgKey.Trim(),
            viewPrefabKey = viewPrefabKey.Trim(),
            parentTarget = parentTarget
        };

        Collect(spec);
    }

    private void EnqueueSpawnBackgroundBoundSpec(
        string bgKey,
        string viewPrefabKey,
        string parentTargetName,
        string profileName)
    {
        if (string.IsNullOrWhiteSpace(bgKey))
        {
            Debug.LogError("[YarnCommandBridge] bg_spawn_bound: bgKey is null or empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(viewPrefabKey))
        {
            Debug.LogError("[YarnCommandBridge] bg_spawn_bound: viewPrefabKey is null or empty.");
            return;
        }

        if (!PresentationTargetParser.TryParseBackgroundStageContent(parentTargetName,
                out PresentationTarget parentTarget) &&
            !PresentationTargetParser.TryParse(parentTargetName, out parentTarget))
        {
            Debug.LogError($"[YarnCommandBridge] bg_spawn_bound: Unknown parent target '{parentTargetName}'.");
            return;
        }

        if (!TryParsePresentationResponseProfile(profileName, out PresentationResponseProfile responseProfile))
        {
            Debug.LogError($"[YarnCommandBridge] bg_spawn_bound: Unknown response profile '{profileName}'.");
            return;
        }

        var spec = new SpawnBackgroundCommandSpec
        {
            bgKey = bgKey.Trim(),
            viewPrefabKey = viewPrefabKey.Trim(),
            parentTarget = parentTarget,
            responseProfile = responseProfile
        };

        Collect(spec);
    }

    private void EnqueueSetBackgroundSpriteSpec(string bgKey, string spritePath)
    {
        if (string.IsNullOrWhiteSpace(bgKey))
        {
            Debug.LogError("[YarnCommandBridge] bg_sprite: bgKey is null or empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(spritePath))
        {
            Debug.LogError("[YarnCommandBridge] bg_sprite: spritePath is null or empty.");
            return;
        }

        Sprite sprite = Resources.Load<Sprite>(spritePath);

        if (sprite == null)
        {
            Debug.LogError($"[YarnCommandBridge] bg_sprite: Sprite not found. path='{spritePath}'");
            return;
        }

        var spec = new SetBackgroundSpriteCommandSpec
        {
            bgKey = bgKey.Trim(),
            sprite = sprite,
            setPreserveAspect = true,
            preserveAspect = true,
            setNativeSize = false,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueFadeBackgroundSpec(string bgKey, float alpha, float duration = 0.35f)
    {
        if (string.IsNullOrWhiteSpace(bgKey))
        {
            Debug.LogError("[YarnCommandBridge] bg_fade: bgKey is null or empty.");
            return;
        }

        var spec = new FadeBackgroundCommandSpec
        {
            bgKey = bgKey.Trim(),
            targetAlpha = Mathf.Clamp01(alpha),
            duration = duration,
            wait = false,
            killTween = true,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueDestroyBackgroundSpec(string bgKey = "current")
    {
        var spec = new DestroyBackgroundCommandSpec
        {
            bgKey = string.IsNullOrWhiteSpace(bgKey) ? "current" : bgKey.Trim(),
            killTween = true,
            removeRefEntry = true,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueResetPresentationTargetSpec(string targetName)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_reset: Unknown target '{targetName}'.");
            return;
        }

        var spec = new ResetPresentationTargetTransformCommandSpec
        {
            target = target,
            resetAnchoredPosition = true,
            anchoredPosition = Vector2.zero,
            resetRotation = true,
            localEulerAngles = Vector3.zero,
            resetScale = true,
            localScale = Vector3.one,
            resetSizeDelta = false,
            killTween = true,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueSlantedMaskCutInSpec(
        string targetName,
        float duration = 0.65f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_slant_mask_in: Unknown target '{targetName}'.");
            return;
        }

        var spec = new SlantedMaskSlideInCommandSpec
        {
            target = target,

            fromOffset = new Vector2(-2200f, 0f),
            toOffset = new Vector2(-770f, 0f),

            slantToRight = false,
            flipVertical = true,

            duration = duration,
            ease = Ease.OutCubic,

            overshootPixels = 72f,
            overshootStart = 0.72f,

            wait = false,
            killTween = true,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueSlantedMaskCutOutSpec(
        string targetName,
        float duration = 0.45f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_slant_cut_out: Unknown target '{targetName}'.");
            return;
        }

        var spec = new SlantedMaskSlideOutCommandSpec
        {
            target = target,

            fromOffset = new Vector2(-770f, 0f),
            toOffset = new Vector2(-2200f, 0f),

            slantToRight = false,
            flipVertical = true,

            duration = duration,
            ease = Ease.InCubic,

            pullPixels = 0f,
            pullEnd = 0.28f,

            wait = false,
            killTween = true,
            strict = true
        };

        Collect(spec);
    }

    private bool TryParsePresentationResponseProfile(string raw, out PresentationResponseProfile profile)
    {
        profile = null;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string s = raw.Trim().ToLowerInvariant();

        switch (s)
        {
            case "background":
            case "bg":
                profile = PresentationResponseProfile.Background;
                return true;

            case "prop":
                profile = PresentationResponseProfile.Prop;
                return true;

            case "characterslot":
            case "character_slot":
            case "char":
            case "slot":
                profile = PresentationResponseProfile.CharacterSlot;
                return true;

            case "foreground":
            case "fg":
                profile = PresentationResponseProfile.Foreground;
                return true;
        }

        return false;
    }
}