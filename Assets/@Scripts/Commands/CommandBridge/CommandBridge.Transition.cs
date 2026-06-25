using DG.Tweening;
using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueSlantedMaskCutInSpec(string stage = "01", float duration = 0.65f)
    {
        var spec = new StageMaskMotionCommandSpec
        {
            stage = PresentationStageKeyParser.Parse(stage, PresentationStageKey.Stage01),
            kind = StageMaskKind.Slanted,

            fromOffset = new Vector2(-2200f, 0f),
            toOffset = new Vector2(-770f, 0f),

            slantPixels = 220f,
            slantToRight = false,
            flipVertical = true,

            showEdge = true,
            edgeMode = StageMaskEdgeMode.Leading,
            edgeColor = new Color(1f, 1f, 1f, 0.92f),
            edgeThickness = 6f,
            hideEdgeOnComplete = false,

            duration = duration,
            ease = Ease.OutCubic,

            rubberMode = StageMaskRubberMode.OvershootEnd,
            overshootPixels = 72f,
            overshootStart = 0.72f,

            wait = false
        };

        Collect(spec);
    }

    private void EnqueueSlantedMaskCutOutSpec(string stage = "01", float duration = 0.45f)
    {
        var spec = new StageMaskMotionCommandSpec
        {
            stage = PresentationStageKeyParser.Parse(stage, PresentationStageKey.Stage01),
            kind = StageMaskKind.Slanted,

            fromOffset = new Vector2(-770f, 0f),
            toOffset = new Vector2(-2200f, 0f),

            slantPixels = 220f,
            slantToRight = false,
            flipVertical = true,

            showEdge = true,
            edgeMode = StageMaskEdgeMode.Leading,
            edgeColor = new Color(1f, 1f, 1f, 0.86f),
            edgeThickness = 6f,
            hideEdgeOnComplete = true,

            duration = duration,
            ease = Ease.InCubic,

            rubberMode = StageMaskRubberMode.PullStart,
            pullPixels = 24f,
            pullEnd = 0.28f,

            wait = false
        };

        Collect(spec);
    }

    private void EnqueueHorizontalStripCutInSpec(string stage = "01", float duration = 0.45f)
    {
        var spec = new StageMaskMotionCommandSpec
        {
            stage = PresentationStageKeyParser.Parse(stage, PresentationStageKey.Stage01),
            kind = StageMaskKind.HorizontalStrip,

            fromOffset = new Vector2(-2200f, 0f),
            toOffset = Vector2.zero,

            stripHeightPixels = 360f,
            horizontalBleedPixels = 96f,

            showEdge = true,
            edgeMode = StageMaskEdgeMode.Both,
            edgeColor = new Color(1f, 1f, 1f, 0.82f),
            edgeThickness = 4f,
            hideEdgeOnComplete = false,

            duration = duration,
            ease = Ease.OutCubic,

            rubberMode = StageMaskRubberMode.OvershootEnd,
            overshootPixels = 48f,
            overshootStart = 0.74f,

            wait = false
        };

        Collect(spec);
    }

    private void EnqueueHorizontalStripCutOutSpec(string stage = "01", float duration = 0.34f)
    {
        var spec = new StageMaskMotionCommandSpec
        {
            stage = PresentationStageKeyParser.Parse(stage, PresentationStageKey.Stage01),
            kind = StageMaskKind.HorizontalStrip,

            fromOffset = Vector2.zero,
            toOffset = new Vector2(2200f, 0f),

            stripHeightPixels = 360f,
            horizontalBleedPixels = 96f,

            showEdge = true,
            edgeMode = StageMaskEdgeMode.Both,
            edgeColor = new Color(1f, 1f, 1f, 0.72f),
            edgeThickness = 4f,
            hideEdgeOnComplete = true,

            duration = duration,
            ease = Ease.InCubic,

            rubberMode = StageMaskRubberMode.PullStart,
            pullPixels = 18f,
            pullEnd = 0.25f,

            wait = false
        };

        Collect(spec);
    }

    private void EnqueueVerticalStripCutInSpec(string stage = "01", float duration = 0.42f)
    {
        var spec = new StageMaskMotionCommandSpec
        {
            stage = PresentationStageKeyParser.Parse(stage, PresentationStageKey.Stage01),
            kind = StageMaskKind.VerticalStrip,

            fromOffset = new Vector2(2200f, 0f),
            toOffset = Vector2.zero,

            verticalStripWidthPixels = 520f,
            verticalBleedPixels = 96f,

            showEdge = true,
            edgeMode = StageMaskEdgeMode.Both,
            edgeColor = new Color(1f, 1f, 1f, 0.78f),
            edgeThickness = 4f,
            hideEdgeOnComplete = false,

            duration = duration,
            ease = Ease.OutCubic,

            rubberMode = StageMaskRubberMode.OvershootEnd,
            overshootPixels = 42f,
            overshootStart = 0.72f,

            wait = false
        };

        Collect(spec);
    }

    private void EnqueueVerticalStripCutOutSpec(string stage = "01", float duration = 0.32f)
    {
        var spec = new StageMaskMotionCommandSpec
        {
            stage = PresentationStageKeyParser.Parse(stage, PresentationStageKey.Stage01),
            kind = StageMaskKind.VerticalStrip,

            fromOffset = Vector2.zero,
            toOffset = new Vector2(2200f, 0f),

            verticalStripWidthPixels = 520f,
            verticalBleedPixels = 96f,

            showEdge = true,
            edgeMode = StageMaskEdgeMode.Both,
            edgeColor = new Color(1f, 1f, 1f, 0.72f),
            edgeThickness = 4f,
            hideEdgeOnComplete = true,

            duration = duration,
            ease = Ease.InCubic,

            rubberMode = StageMaskRubberMode.PullStart,
            pullPixels = 18f,
            pullEnd = 0.25f,

            wait = false
        };

        Collect(spec);
    }

    private void EnqueueDiagonalBandCutInSpec(string stage = "01", float duration = 0.38f)
    {
        var spec = new StageMaskMotionCommandSpec
        {
            stage = PresentationStageKeyParser.Parse(stage, PresentationStageKey.Stage01),
            kind = StageMaskKind.DiagonalBand,

            fromOffset = new Vector2(-2600f, 0f),
            toOffset = Vector2.zero,

            diagonalBandWidthPixels = 760f,
            diagonalBandSlantPixels = 520f,
            diagonalBandBleedPixels = 320f,
            diagonalBandToRight = true,

            showEdge = true,
            edgeMode = StageMaskEdgeMode.Both,
            edgeColor = new Color(1f, 1f, 1f, 0.88f),
            edgeThickness = 5f,
            hideEdgeOnComplete = false,

            duration = duration,
            ease = Ease.OutQuart,

            rubberMode = StageMaskRubberMode.OvershootEnd,
            overshootPixels = 64f,
            overshootStart = 0.70f,

            wait = false
        };

        Collect(spec);
    }

    private void EnqueueDiagonalBandCutOutSpec(string stage = "01", float duration = 0.28f)
    {
        var spec = new StageMaskMotionCommandSpec
        {
            stage = PresentationStageKeyParser.Parse(stage, PresentationStageKey.Stage01),
            kind = StageMaskKind.DiagonalBand,

            fromOffset = Vector2.zero,
            toOffset = new Vector2(2600f, 0f),

            diagonalBandWidthPixels = 760f,
            diagonalBandSlantPixels = 520f,
            diagonalBandBleedPixels = 320f,
            diagonalBandToRight = true,

            showEdge = true,
            edgeMode = StageMaskEdgeMode.Both,
            edgeColor = new Color(1f, 1f, 1f, 0.78f),
            edgeThickness = 5f,
            hideEdgeOnComplete = true,

            duration = duration,
            ease = Ease.InQuart,

            rubberMode = StageMaskRubberMode.PullStart,
            pullPixels = 18f,
            pullEnd = 0.22f,

            wait = false
        };

        Collect(spec);
    }

    private void EnqueueCircleIrisInSpec(string stage = "01", float duration = 0.5f)
    {
        var spec = new StageMaskMotionCommandSpec
        {
            stage = PresentationStageKeyParser.Parse(stage, PresentationStageKey.Stage01),
            kind = StageMaskKind.CircleIris,

            fromOffset = Vector2.zero,
            toOffset = Vector2.zero,

            fromIrisRadiusPixels = 0f,
            toIrisRadiusPixels = 1280f,
            irisAspect = 1.777f,
            irisSegments = 72,

            showEdge = true,
            edgeMode = StageMaskEdgeMode.Outline,
            edgeColor = new Color(1f, 1f, 1f, 0.66f),
            edgeThickness = 3f,
            hideEdgeOnComplete = true,

            duration = duration,
            ease = Ease.OutCubic,

            rubberMode = StageMaskRubberMode.None,

            wait = false
        };

        Collect(spec);
    }

    private void EnqueueCircleIrisOutSpec(string stage = "01", float duration = 0.42f)
    {
        var spec = new StageMaskMotionCommandSpec
        {
            stage = PresentationStageKeyParser.Parse(stage, PresentationStageKey.Stage01),
            kind = StageMaskKind.CircleIris,

            fromOffset = Vector2.zero,
            toOffset = Vector2.zero,

            fromIrisRadiusPixels = 1280f,
            toIrisRadiusPixels = 0f,
            irisAspect = 1.777f,
            irisSegments = 72,

            showEdge = true,
            edgeMode = StageMaskEdgeMode.Outline,
            edgeColor = new Color(1f, 1f, 1f, 0.66f),
            edgeThickness = 3f,
            hideEdgeOnComplete = true,

            duration = duration,
            ease = Ease.InCubic,

            rubberMode = StageMaskRubberMode.None,

            wait = false
        };

        Collect(spec);
    }

    private void EnqueueStageMaskClearSpec(string stage = "01")
    {
        Collect(new StageMaskClearCommandSpec
        {
            stage = PresentationStageKeyParser.Parse(stage, PresentationStageKey.Stage01),
            mode = StageMaskClearMode.FullVisible,
            hideEdge = true
        });
    }


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