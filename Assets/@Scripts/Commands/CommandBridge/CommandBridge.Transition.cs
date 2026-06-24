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

    private void EnqueueVerticalStripCoverSpec(float duration = 0.4f)
    {
        var spec = new VerticalStripWipeCommandSpec
        {
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
        Collect(new ClearAllTransitionsCommandSpec { });
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
        };

        Collect(spec);
    }
}