public interface IScreenEffectHost
{
    ScreenVignetteEffectController Vignette { get; }
    ScreenNoiseEffectController Noise { get; }
    ScreenFlashEffectController Flash { get; }

    void KillAllTweens(bool complete);
    void ClearAllImmediate();
    void ResetToBaselineImmediate();
}