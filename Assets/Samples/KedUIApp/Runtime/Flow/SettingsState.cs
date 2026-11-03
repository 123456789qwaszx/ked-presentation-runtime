using System;
using System.Collections.Generic;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 설정 화면의 상태와 규칙 — 기본값 · 배속 보기 · 지금 탭 · 탭 되돌리기. 각 탭의 화면 모델(값)을 낸다.
    /// 값을 엔진 · 본체에 적용하고 저장하는 것은 흐름(KedUIFlow.Settings)이다. 글자는 각 Page가 정한다.
    /// </summary>
    internal sealed class SettingsState
    {
        public const int AudioTab = 0;
        public const int TextTab = 1;
        public const int PlaybackTab = 2;
        public const int DisplayTab = 3;

        private readonly List<float> _speedChoices = new();

        public KedSettings Defaults { get; private set; } = new();
        public KedSettings Values { get; private set; } = new();
        public IReadOnlyList<float> SpeedChoices => _speedChoices;
        public int Tab { get; private set; }

        // 화면 탭은 되돌릴 값이 없다(창 모드는 엔진이 기억한다).
        public bool CanResetTab => Tab != DisplayTab;

        // 기본값은 씬 · 본체에 적힌 값(출력 속도 · 자동 대기 · 배속)이다 — 여기서 새 기본을 만들지 않는다.
        // 배속 보기는 본체가 허용하는 범위(하한 · 상한) 안의 셋. 마지막은 본체 기본값.
        public void SetDefaults(float typeSpeed, float autoDelay, float speedUp, float minMultiplier, float maxMultiplier)
        {
            Defaults = new KedSettings
            {
                MasterVolume = 1f,
                Muted = false,
                TextScale = 1f,
                TypeSpeed = Math.Clamp(typeSpeed, KedSettings.MinTypeSpeed, KedSettings.MaxTypeSpeed),
                BoxOpacity = 1f,
                AutoDelay = Math.Clamp(autoDelay, KedSettings.MinAutoDelay, KedSettings.MaxAutoDelay),
                SpeedUpMultiplier = speedUp,
            };

            _speedChoices.Clear();

            foreach (float candidate in new[] { 2f, 4f, speedUp })
            {
                float value = Math.Clamp(candidate, minMultiplier, maxMultiplier);
                if (!_speedChoices.Contains(value))
                    _speedChoices.Add(value);
            }

            _speedChoices.Sort();
            Values = Defaults.Clone();
        }

        // 저장된 값을 받는다. 배속이 보기에 없으면(본체 범위가 바뀌었다) 기본 배속으로.
        public void Adopt(KedSettings saved)
        {
            Values = saved ?? Defaults.Clone();

            if (!_speedChoices.Contains(Values.SpeedUpMultiplier))
                Values.SpeedUpMultiplier = Defaults.SpeedUpMultiplier;
        }

        public int SelectTab(int tab)
        {
            Tab = Math.Clamp(tab, 0, SettingsPanel.TabCount - 1);
            return Tab;
        }

        // 지금 탭의 값을 기본값으로. 음량 탭의 채널 음량(배경음 · 효과음 · 음성)은 본체가 저장하므로 흐름이 되돌린다.
        public void ResetTab()
        {
            switch (Tab)
            {
                case AudioTab:
                    Values.MasterVolume = Defaults.MasterVolume;
                    Values.Muted = Defaults.Muted;
                    break;

                case TextTab:
                    Values.TextScale = Defaults.TextScale;
                    Values.TypeSpeed = Defaults.TypeSpeed;
                    Values.BoxOpacity = Defaults.BoxOpacity;
                    break;

                case PlaybackTab:
                    Values.AutoDelay = Defaults.AutoDelay;
                    Values.SpeedUpMultiplier = Defaults.SpeedUpMultiplier;
                    break;
            }
        }

        // 채널 음량은 본체가 들고 있어 흐름이 넘긴다.
        public AudioSettingsPage.Model AudioModel(float bgm, float sfx, float voice) => new()
        {
            Master = Values.MasterVolume,
            Bgm = bgm,
            Sfx = sfx,
            Voice = voice,
            Muted = Values.Muted,
        };

        public TextSettingsPage.Model TextModel() => new()
        {
            TextScale = Values.TextScale,
            TypeSpeed = Values.TypeSpeed,
            BoxOpacity = Values.BoxOpacity,
        };

        public PlaybackSettingsPage.Model PlaybackModel() => new()
        {
            AutoDelay = Values.AutoDelay,
            SpeedChoices = _speedChoices,
            SpeedUpMultiplier = Values.SpeedUpMultiplier,
        };
    }
}
