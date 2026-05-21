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


        _dialogueRunner.AddCommandHandler("box_hide", EnqueueHideDialogueBoxSpec);
    }

    private void EnqueueDazeFadeCloseSpec(
        string targetName,
        float duration = 0.85f)
    {

        var spec = new FocusBlurCurtainCommandSpec
        {
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
        };

        Collect(spec);
    }

    private void EnqueueDazeFadeOpenSpec(
        string targetName,
        float duration = 0.65f)
    {

        var spec = new FocusBlurCurtainCommandSpec
        {

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
        };

        Collect(spec);
    }

    private void EnqueueFocusBlurCurtainCloseSpec(
        string targetName,
        float duration = 0.55f)
    {
        var spec = new FocusBlurCurtainCommandSpec
        {
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
        };

        Collect(spec);
    }

    private void EnqueueFocusBlurCurtainOpenSpec(
        string targetName,
        float duration = 0.42f)
    {

        var spec = new FocusBlurCurtainCommandSpec
        {
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
        };

        Collect(spec);
    }


    private void EnqueueFocusBlurFadeOutSpec(
        string targetName,
        float duration = 0.45f)
    {
        var spec = new FocusBlurFadeCommandSpec
        {
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
        };

        Collect(spec);
    }

    private void EnqueueFocusBlurFadeInSpec(
        string targetName,
        float duration = 0.35f)
    {

        var spec = new FocusBlurFadeCommandSpec
        {

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
        };

        Collect(spec);
    }

    private void EnqueueSlantedShutterCloseSpec(
        string targetName,
        float duration = 0.38f)
    {

        var spec = new SlantedShutterCommandSpec
        {

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
        };

        Collect(spec);
    }

    private void EnqueueSlantedShutterOpenSpec(
        string targetName,
        float duration = 0.32f)
    {

        var spec = new SlantedShutterCommandSpec
        {
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
        };

        Collect(spec);
    }

    private void EnqueueVerticalStripCoverSpec(
        string targetName,
        float duration = 0f)
    {

        var spec = new VerticalStripWipeCommandSpec
        {

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
        };

        Collect(spec);
    }

    private void EnqueueVerticalStripClearSpec(
        string targetName,
        float duration = 0f)
    {

        var spec = new VerticalStripWipeCommandSpec
        {

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
        };

        Collect(spec);
    }

    private void EnqueueSetupPresentationViewSpec()
    {
        var spec = new SetupPresentationViewCommandSpec
        {
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


        var spec = new SpawnBackgroundCommandSpec
        {
            bgKey = bgKey.Trim(),
            viewPrefabKey = viewPrefabKey.Trim(),
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
        
        if (!TryParsePresentationResponseProfile(profileName, out PresentationResponseProfile responseProfile))
        {
            Debug.LogError($"[YarnCommandBridge] bg_spawn_bound: Unknown response profile '{profileName}'.");
            return;
        }

        var spec = new SpawnBackgroundCommandSpec
        {
            bgKey = bgKey.Trim(),
            viewPrefabKey = viewPrefabKey.Trim(),
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

        // var spec = new ResetPresentationTargetTransformCommandSpec
        // {
        //     resetAnchoredPosition = true,
        //     anchoredPosition = Vector2.zero,
        //     resetRotation = true,
        //     localEulerAngles = Vector3.zero,
        //     resetScale = true,
        //     localScale = Vector3.one,
        //     resetSizeDelta = false,
        //     killTween = true,
        // };

        //Collect(spec);
    }

    private void EnqueueSlantedMaskCutInSpec(
        string targetName,
        float duration = 0.65f)
    {

        var spec = new SlantedMaskSlideInCommandSpec
        {
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
        };

        Collect(spec);
    }

    private void EnqueueSlantedMaskCutOutSpec(
        string targetName,
        float duration = 0.45f)
    {

        var spec = new SlantedMaskSlideOutCommandSpec
        {
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