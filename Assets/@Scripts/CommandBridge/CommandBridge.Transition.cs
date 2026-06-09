using DG.Tweening;
using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueDazeFadeCloseSpec(float duration = 0.85f)
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
            disableWhenOpen = true,
            blockRaycastWhenClosed = false,
        };

        Collect(spec);
    }

    private void EnqueueDazeFadeOpenSpec(float duration = 0.65f)
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
            disableWhenOpen = true,
            blockRaycastWhenClosed = false,
        };

        Collect(spec);
    }

    private void EnqueueFocusBlurCurtainCloseSpec(float duration = 0.55f)
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
            disableWhenOpen = true,
            blockRaycastWhenClosed = false,
        };

        Collect(spec);
    }

    private void EnqueueFocusBlurCurtainOpenSpec(float duration = 0.42f)
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
            disableWhenOpen = true,
            blockRaycastWhenClosed = false,
        };

        Collect(spec);
    }


    private void EnqueueFocusBlurFadeOutSpec(float duration = 0.45f)
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
            disableWhenClear = true,
            blockRaycastWhenVisible = false,
        };

        Collect(spec);
    }

    private void EnqueueFocusBlurFadeInSpec(float duration = 0.35f)
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
            disableWhenClear = true,
            blockRaycastWhenVisible = false,
        };

        Collect(spec);
    }

    private void EnqueueSlantedShutterCloseSpec(float duration = 0.38f)
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
            disableWhenOpen = true,
            blockRaycastWhileClosed = false,
        };

        Collect(spec);
    }

    private void EnqueueSlantedShutterOpenSpec(float duration = 0.32f)
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
            disableWhenOpen = true,
            blockRaycastWhileClosed = false,
        };

        Collect(spec);
    }

    private void EnqueueVerticalStripCoverSpec(float duration = 0.4f)
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

            wait = true
        };

        Collect(spec);
    }

    private void EnqueueVerticalStripClearSpec(float duration = 0.4f)
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

            wait = true
        };

        Collect(spec);
    }
    
    private void EnqueueSlantedMaskCutInSpec(float duration = 0.65f)
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

            wait = false
        };

        Collect(spec);
    }

    private void EnqueueSlantedMaskCutOutSpec(float duration = 0.45f)
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

            wait = false
        };

        Collect(spec);
    }
    
    private void EnqueueClearAllTransitionsSpec()
    {
        Collect(new ClearAllTransitionsCommandSpec
        {
            wait = false
        });
    }

    private void EnqueueRevealWithTransitionSpec(string kind, float duration = 0.4f)
    {
        var spec = new RevealWithTransitionCommandSpec
        {
            kind = ParseRevealTransitionKind(kind),
            duration = duration,
            wait = true,
            killTween = true,
            clearOthersBeforeReveal = true,
            clearAllAfterReveal = true
        };

        Collect(spec);
    }

    private static PresentationRevealTransitionKind ParseRevealTransitionKind(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return PresentationRevealTransitionKind.VerticalStrip;

        string s = raw.Trim().ToLowerInvariant();
        s = s.Replace("-", "_");
        s = s.Replace(".", "_");

        switch (s)
        {
            case "strip":
            case "vertical_strip":
            case "wipe":
                return PresentationRevealTransitionKind.VerticalStrip;

            case "shutter":
            case "slanted_shutter":
                return PresentationRevealTransitionKind.SlantedShutter;

            case "fade":
            case "blur_fade":
            case "focus_fade":
                return PresentationRevealTransitionKind.FocusBlurFade;

            case "curtain":
            case "blur_curtain":
            case "focus_curtain":
                return PresentationRevealTransitionKind.FocusBlurCurtain;

            default:
                return PresentationRevealTransitionKind.VerticalStrip;
        }
    }
    
    private void EnqueueTransitionOutSlantSpec(float duration = 0.45f)
{
    var spec = new TransitionOutSlantCommandSpec
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
        clearOthersBeforeOut = true,
        clearAllAfterOut = true,
    };

    Collect(spec);
}

private void EnqueueTransitionOutFocusFadeSpec(float duration = 0.35f)
{
    var spec = new TransitionOutFocusFadeCommandSpec
    {
        color = Color.black,
        maxAlpha = 1f,
        zoomAmount = 0.035f,

        duration = duration,
        ease = Ease.InOutCubic,

        wait = true,
        killTween = true,
        clearOthersBeforeOut = true,
        clearAllAfterOut = true,
        blockRaycastWhenVisible = false,
    };

    Collect(spec);
}

private void EnqueueTransitionOutFocusCurtainSpec(float duration = 0.42f)
{
    var spec = new TransitionOutFocusCurtainCommandSpec
    {
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
        clearOthersBeforeOut = true,
        clearAllAfterOut = true,
        blockRaycastWhenClosed = false,
    };

    Collect(spec);
}

private void EnqueueTransitionOutDazeFadeSpec(float duration = 0.65f)
{
    var spec = new TransitionOutFocusCurtainCommandSpec
    {
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
        clearOthersBeforeOut = true,
        clearAllAfterOut = true,
        blockRaycastWhenClosed = false,
    };

    Collect(spec);
}

private void EnqueueTransitionOutShutterSpec(float duration = 0.32f)
{
    var spec = new TransitionOutShutterCommandSpec
    {
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
        clearOthersBeforeOut = true,
        clearAllAfterOut = true,
        blockRaycastWhileClosed = false,
    };

    Collect(spec);
}

private void EnqueueTransitionOutStripSpec(float duration = 0.4f)
{
    var spec = new TransitionOutStripCommandSpec
    {
        order = VerticalStripWipeOrder.RightToLeft,

        stripCount = 20,
        stripDelay = 0.02f,
        stripFillDuration = 0.08f,

        color = Color.black,

        duration = duration,
        ease = Ease.Linear,

        wait = true,
        killTween = true,
        clearOthersBeforeOut = true,
        clearAllAfterOut = true,
    };

    Collect(spec);
}
}