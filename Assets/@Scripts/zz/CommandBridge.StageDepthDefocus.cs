using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private void BindStageDepthDefocus(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, float>(
            "blur", EnqueueStage00DepthBlurSpec);

        runner.AddCommandHandler<string, float>(
            "blur_s1", EnqueueStage01DepthBlurSpec);

        runner.AddCommandHandler<string, float>(
            "blur_s2", EnqueueStage02DepthBlurSpec);

        runner.AddCommandHandler(
            "blur_clear", EnqueueStageDepthBlurClearSpec);
    }

    private void EnqueueStage00DepthBlurSpec(
        string layerKey = "mid",
        float blurRadius = 1f)
    {
        if (!PresentationCommandKeyParser.TryParseDepthLayerKey(
                layerKey,
                out PresentationDepthLayerKey layer))
            return;

        var spec = new StageDepthDefocusCommandSpec
        {
            stage = PresentationStageKey.Stage00,
            layer = layer,
            visible = blurRadius > 0f,
            blurRadius = blurRadius
        };

        Collect(spec);
    }

    private void EnqueueStage01DepthBlurSpec(
        string layerKey = "mid",
        float blurRadius = 1f)
    {
        if (!PresentationCommandKeyParser.TryParseDepthLayerKey(
                layerKey,
                out PresentationDepthLayerKey layer))
            return;

        var spec = new StageDepthDefocusCommandSpec
        {
            stage = PresentationStageKey.Stage01,
            layer = layer,
            visible = blurRadius > 0f,
            blurRadius = blurRadius
        };

        Collect(spec);
    }

    private void EnqueueStage02DepthBlurSpec(
        string layerKey = "mid",
        float blurRadius = 1f)
    {
        if (!PresentationCommandKeyParser.TryParseDepthLayerKey(
                layerKey,
                out PresentationDepthLayerKey layer))
            return;

        var spec = new StageDepthDefocusCommandSpec
        {
            stage = PresentationStageKey.Stage02,
            layer = layer,
            visible = blurRadius > 0f,
            blurRadius = blurRadius
        };

        Collect(spec);
    }

    private void EnqueueStageDepthBlurClearSpec()
    {
        Collect(new StageDepthDefocusCommandSpec
        {
            stage = PresentationStageKey.Stage00,
            layer = PresentationDepthLayerKey.Far,
            visible = false
        });

        Collect(new StageDepthDefocusCommandSpec
        {
            stage = PresentationStageKey.Stage00,
            layer = PresentationDepthLayerKey.Back,
            visible = false
        });

        Collect(new StageDepthDefocusCommandSpec
        {
            stage = PresentationStageKey.Stage00,
            layer = PresentationDepthLayerKey.Mid,
            visible = false
        });

        Collect(new StageDepthDefocusCommandSpec
        {
            stage = PresentationStageKey.Stage00,
            layer = PresentationDepthLayerKey.Front,
            visible = false
        });

        Collect(new StageDepthDefocusCommandSpec
        {
            stage = PresentationStageKey.Stage00,
            layer = PresentationDepthLayerKey.Close,
            visible = false
        });


        Collect(new StageDepthDefocusCommandSpec
        {
            stage = PresentationStageKey.Stage01,
            layer = PresentationDepthLayerKey.Far,
            visible = false
        });

        Collect(new StageDepthDefocusCommandSpec
        {
            stage = PresentationStageKey.Stage01,
            layer = PresentationDepthLayerKey.Back,
            visible = false
        });

        Collect(new StageDepthDefocusCommandSpec
        {
            stage = PresentationStageKey.Stage01,
            layer = PresentationDepthLayerKey.Mid,
            visible = false
        });

        Collect(new StageDepthDefocusCommandSpec
        {
            stage = PresentationStageKey.Stage01,
            layer = PresentationDepthLayerKey.Front,
            visible = false
        });

        Collect(new StageDepthDefocusCommandSpec
        {
            stage = PresentationStageKey.Stage01,
            layer = PresentationDepthLayerKey.Close,
            visible = false
        });


        Collect(new StageDepthDefocusCommandSpec
        {
            stage = PresentationStageKey.Stage02,
            layer = PresentationDepthLayerKey.Far,
            visible = false
        });

        Collect(new StageDepthDefocusCommandSpec
        {
            stage = PresentationStageKey.Stage02,
            layer = PresentationDepthLayerKey.Back,
            visible = false
        });

        Collect(new StageDepthDefocusCommandSpec
        {
            stage = PresentationStageKey.Stage02,
            layer = PresentationDepthLayerKey.Mid,
            visible = false
        });

        Collect(new StageDepthDefocusCommandSpec
        {
            stage = PresentationStageKey.Stage02,
            layer = PresentationDepthLayerKey.Front,
            visible = false
        });

        Collect(new StageDepthDefocusCommandSpec
        {
            stage = PresentationStageKey.Stage02,
            layer = PresentationDepthLayerKey.Close,
            visible = false
        });
    }
}