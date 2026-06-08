public interface IBackgroundRigBlurRuntime
{
    void Bind(string rigKey, BackgroundRigRefs refs);
    void ClearBindings();

    void ShowDefocus(
        string rigKey,
        float alpha,
        float duration,
        float blurRadius,
        int iterations,
        UIStageBlurDownsample downsample);

    void HideDefocus(string rigKey, float duration);
}