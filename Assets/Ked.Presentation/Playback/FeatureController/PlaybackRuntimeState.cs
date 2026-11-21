using UnityEngine;

public sealed class PlaybackRuntimeState
{
    public PlaybackSettings PlaybackSettings { get; set; } = new();
    
    private bool _isAutoMode;
    private bool _isSpeedUpMode;
    private bool _isRapidSkipMode;
    private float _timeScale = 1f;

    // 스텝 서두르기 — 전경(pause)이 도는 동안의 클릭이 켠다. 그 스텝의 남은 명령이 압축된다(PresentationSessionContext).
    // 다음 줄에 들어설 때(StepHead.Enter) 내린다 — 다음 줄로 새지 않는다. 줄의 앞 앵커를 서두른 것은 start 앞에서 내린다.
    private bool _isStepHurried;

    public bool IsAutoMode => _isAutoMode;
    public bool IsSpeedUpMode => _isSpeedUpMode;
    public bool IsRapidSkipMode => _isRapidSkipMode;
    public bool IsStepHurried => _isStepHurried;

    public float TimeScale => _timeScale;
    public float AutoAdvanceDelay => PlaybackSettings != null
        ? PlaybackSettings.autoModeDelaySeconds
        : 1.5f;

    public void SetAutoModeEnabled(bool enabled)
    {
        _isAutoMode = enabled;
    }

    public void SetSpeedUpModeEnabled(bool enabled)
    {
        _isSpeedUpMode = enabled;
    }

    public void SetRapidSkipModeEnabled(bool enabled)
    {
        _isRapidSkipMode = enabled;
    }

    public void HurryStep() => _isStepHurried = true;

    public void EndStepHurry() => _isStepHurried = false;

    public void SetTimeScale(float timeScale)
    {
        _timeScale = Mathf.Max(0.0001f, timeScale);
    }

    public void ResetPlaybackModifiers()
    {
        _isAutoMode = false;
        _isSpeedUpMode = false;
        _isRapidSkipMode = false;
        _isStepHurried = false;
        _timeScale = 1f;
    }
}