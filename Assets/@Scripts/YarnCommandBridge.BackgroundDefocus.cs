public sealed partial class YarnCommandBridge
{
    private void EnqueueBackgroundDefocusSpec(
        string rigKey,
        float alpha,
        float duration)
    {
        Collect(new BackgroundDefocusCommandSpecBgR
        {
            rigKey = rigKey,
            alpha = alpha,
            duration = duration,

            // Default quality preset
            blurRadius = 3f,
            iterations = 2,
            downsample = UIStageBlurDownsample.Quarter
        });
    }

    private void EnqueueBackgroundDefocusCustomSpec(
        string rigKey,
        float alpha,
        float blurRadius,
        int iterations,
        string downsample,
        float duration)
    {
        Collect(new BackgroundDefocusCommandSpecBgR
        {
            rigKey = rigKey,
            alpha = alpha,
            duration = duration,
            blurRadius = blurRadius,
            iterations = iterations,
            downsample = ParseBlurDownsample(downsample)
        });
    }

    private void EnqueueBackgroundDefocusClearSpec(
        string rigKey,
        float duration)
    {
        Collect(new BackgroundDefocusClearCommandSpecBgR
        {
            rigKey = rigKey,
            duration = duration
        });
    }

    private UIStageBlurDownsample ParseBlurDownsample(string value)
    {
        if (string.IsNullOrEmpty(value))
            return UIStageBlurDownsample.Quarter;

        switch (value.Trim().ToLowerInvariant())
        {
            case "full":
            case "1":
            case "x1":
                return UIStageBlurDownsample.Full;

            case "half":
            case "2":
            case "x2":
                return UIStageBlurDownsample.Half;

            case "quarter":
            case "4":
            case "x4":
                return UIStageBlurDownsample.Quarter;

            case "eighth":
            case "8":
            case "x8":
                return UIStageBlurDownsample.Eighth;

            default:
                UnityEngine.Debug.LogWarning(
                    $"[YarnCommandBridge] Unknown blur downsample '{value}'. Fallback to Quarter.");

                return UIStageBlurDownsample.Quarter;
        }
    }
}