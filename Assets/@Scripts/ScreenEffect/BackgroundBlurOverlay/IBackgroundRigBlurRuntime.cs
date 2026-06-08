public interface IBackgroundRigBlurRuntime
{
    void ShowDefocus(
        string rigKey,
        BackgroundRigRefs refs,
        float alpha,
        float duration,
        float blurRadius,
        int iterations,
        UIStageBlurDownsample downsample);

    void HideDefocus(string rigKey, float duration);
}