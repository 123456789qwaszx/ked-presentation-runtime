using UnityEngine;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 새 예제 UI의 설정 값. 연결할 수 있는 것만 둔다(PLAN U4 "실제로 동작하는 것만").
    ///
    /// | 탭 | 항목 | 실제로 바뀌는 곳 |
    /// |---|---|---|
    /// | 음량 | 전체 음량 · 음소거 | AudioListener.volume (엔진 전체) — 이 값만 여기 저장 |
    /// | 음량 | 배경음 · 효과음 · 음성 | 본체 AudioVolumeSettings — 저장도 그쪽(PlayerPrefs Volume_*). 여기 두지 않는다 |
    /// | 대사 | 글자 크기 | UIPF UIContext.TextScale → 대사·선택지·백로그·팝업 본문 |
    /// | 대사 | 글자 출력 속도 | EllipsisBreathTypewriter.unitsPerSecond |
    /// | 대사 | 대사창 불투명도 | 대사창·이름표 그림의 투명도 |
    /// | 재생 | 자동 진행 대기 | VNPlaybackSettings.autoModeDelaySeconds |
    /// | 재생 | 빠르게 재생 배속 | VNPlaybackSettings.speedupModeMultiplier (본체 하한 2배) |
    /// | 화면 | 창 모드 · 전체 화면 | Screen.fullScreenMode — 엔진이 스스로 기억한다. 에디터에서는 바뀌지 않는다 |
    ///
    /// 기본값 중 둘(출력 속도·자동 대기·배속)은 씬·본체에 적힌 값을 시작할 때 읽어 쓴다 — 여기서 새 기본을 만들지 않는다.
    /// </summary>
    public sealed class KedSettings
    {
        private const string Prefix = "KedUI.";

        public const float MinTextScale = 0.8f;
        public const float MaxTextScale = 1.3f;
        public const float MinTypeSpeed = 10f;
        public const float MaxTypeSpeed = 90f;
        public const float MinBoxOpacity = 0.3f;
        public const float MinAutoDelay = 0.5f;
        public const float MaxAutoDelay = 5f;

        public float MasterVolume = 1f;
        public bool Muted;

        public float TextScale = 1f;
        public float TypeSpeed = 30f;
        public float BoxOpacity = 1f;

        public float AutoDelay = 1.5f;
        public float SpeedUpMultiplier = 12f;

        public KedSettings Clone() => (KedSettings)MemberwiseClone();

        /// <summary>저장된 값을 읽는다. 없는 값은 defaults의 값.</summary>
        public static KedSettings Load(KedSettings defaults)
        {
            KedSettings s = defaults.Clone();

            s.MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "MasterVolume", defaults.MasterVolume));
            s.Muted = PlayerPrefs.GetInt(Prefix + "Muted", defaults.Muted ? 1 : 0) != 0;

            s.TextScale = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "TextScale", defaults.TextScale), MinTextScale, MaxTextScale);
            s.TypeSpeed = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "TypeSpeed", defaults.TypeSpeed), MinTypeSpeed, MaxTypeSpeed);
            s.BoxOpacity = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "BoxOpacity", defaults.BoxOpacity), MinBoxOpacity, 1f);

            s.AutoDelay = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "AutoDelay", defaults.AutoDelay), MinAutoDelay, MaxAutoDelay);
            s.SpeedUpMultiplier = PlayerPrefs.GetFloat(Prefix + "SpeedUpMultiplier", defaults.SpeedUpMultiplier);

            return s;
        }

        public void Store()
        {
            PlayerPrefs.SetFloat(Prefix + "MasterVolume", MasterVolume);
            PlayerPrefs.SetInt(Prefix + "Muted", Muted ? 1 : 0);

            PlayerPrefs.SetFloat(Prefix + "TextScale", TextScale);
            PlayerPrefs.SetFloat(Prefix + "TypeSpeed", TypeSpeed);
            PlayerPrefs.SetFloat(Prefix + "BoxOpacity", BoxOpacity);

            PlayerPrefs.SetFloat(Prefix + "AutoDelay", AutoDelay);
            PlayerPrefs.SetFloat(Prefix + "SpeedUpMultiplier", SpeedUpMultiplier);
        }
    }
}
