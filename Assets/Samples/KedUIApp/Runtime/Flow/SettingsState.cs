using System;
using System.Collections.Generic;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 설정 화면의 상태와 규칙 — 기본값 · 값 범위 · 배속 보기 · 지금 탭 · 탭 되돌리기. 각 탭의 화면 모델(값)을 낸다.
    /// 값은 여기 Set*으로만 바꾼다(범위는 여기서 맞춘다). 엔진 · 본체에 적용하고 저장하는 것은 흐름(KedUIFlow.Settings)이다. 글자는 각 Page가 정한다.
    /// </summary>
    internal sealed class SettingsState
    {
        private readonly List<float> _speedChoices = new();

        public KedSettings Defaults { get; private set; } = new();
        public KedSettings Values { get; private set; } = new();
        public IReadOnlyList<float> SpeedChoices => _speedChoices;
        public SettingsPanel.Tab Tab { get; set; }

        // 음량 탭의 캐릭터별 음성을 펼쳤는가. 저장하지 않는다.
        public bool CharacterVoicesExpanded { get; set; } = true;

        // 화면 탭은 되돌릴 값이 없다(창 모드는 엔진이 기억한다).
        public bool CanResetTab => Tab != SettingsPanel.Tab.Display;

        // 기본값은 씬 · 본체에 적힌 값(출력 속도 · 자동 대기 · 배속)이다 — 여기서 새 기본을 만들지 않는다.
        // 배속 보기는 2 · 4 · 8배(재생 화면의 배속 버튼과 같은 셋, 2026-11-04 소유자), 본체가 허용하는 범위(하한 · 상한) 안으로.
        // 기본 배속은 본체 기본값에 가장 가까운 보기.
        public void SetDefaults(float typeSpeed, float autoDelay, float speedUp, float minMultiplier, float maxMultiplier)
        {
            _speedChoices.Clear();

            foreach (float candidate in new[] { 2f, 4f, 8f })
            {
                float value = Math.Clamp(candidate, minMultiplier, maxMultiplier);
                if (!_speedChoices.Contains(value))
                    _speedChoices.Add(value);
            }

            _speedChoices.Sort();
            speedUp = Nearest(_speedChoices, Math.Clamp(speedUp, minMultiplier, maxMultiplier));

            Defaults = new KedSettings
            {
                MasterVolume = 1f,
                Muted = false,
                TextScale = 1f,
                TypeSpeed = Math.Clamp(typeSpeed, KedSettingRanges.MinTypeSpeed, KedSettingRanges.MaxTypeSpeed),
                BoxOpacity = 1f,
                AutoDelay = Math.Clamp(autoDelay, KedSettingRanges.MinAutoDelay, KedSettingRanges.MaxAutoDelay),
                SpeedUpMultiplier = speedUp,
            };

            Values = Defaults.Clone();
        }

        // 저장된 값을 받는다. 범위 밖 값은 범위 안으로, 배속이 보기에 없으면(본체 범위가 바뀌었다) 기본 배속 그대로.
        public void Adopt(KedSettings saved)
        {
            Values = Defaults.Clone();

            if (saved == null)
                return;

            SetMasterVolume(saved.MasterVolume);
            SetMuted(saved.Muted);
            SetTextScale(saved.TextScale);
            SetTypeSpeed(saved.TypeSpeed);
            SetBoxOpacity(saved.BoxOpacity);
            SetAutoDelay(saved.AutoDelay);
            SetSpeedUp(saved.SpeedUpMultiplier);

            foreach (KeyValuePair<string, KedSettings.CharacterVoice> pair in saved.CharacterVoices)
            {
                SetCharacterVolume(pair.Key, pair.Value.Volume);
                SetCharacterVoiceOn(pair.Key, pair.Value.On);
            }
        }

        #region 값 바꾸기

        public void SetMasterVolume(float volume) => Values.MasterVolume = Math.Clamp(volume, 0f, 1f);

        public void SetMuted(bool muted) => Values.Muted = muted;

        public void SetTextScale(float scale) =>
            Values.TextScale = Math.Clamp(scale, KedSettingRanges.MinTextScale, KedSettingRanges.MaxTextScale);

        public void SetTypeSpeed(float speed) =>
            Values.TypeSpeed = Math.Clamp(speed, KedSettingRanges.MinTypeSpeed, KedSettingRanges.MaxTypeSpeed);

        public void SetBoxOpacity(float opacity) =>
            Values.BoxOpacity = Math.Clamp(opacity, KedSettingRanges.MinBoxOpacity, 1f);

        public void SetAutoDelay(float seconds) =>
            Values.AutoDelay = Math.Clamp(seconds, KedSettingRanges.MinAutoDelay, KedSettingRanges.MaxAutoDelay);

        // 배속은 보기 중 하나만 — 그래서 늘 본체 범위 안이다. 보기에 없으면 바꾸지 않는다.
        public bool SetSpeedUp(float multiplier)
        {
            if (!_speedChoices.Contains(multiplier))
                return false;

            Values.SpeedUpMultiplier = multiplier;
            return true;
        }

        // 배속 버튼의 다음 배속 — 끔 → 2 → 4 → 8 → 끔. 마지막 배속에서 누르면 false(끈다).
        public bool TryNextSpeed(bool on, out float next)
        {
            int index = on ? _speedChoices.IndexOf(Values.SpeedUpMultiplier) + 1 : 0;

            if (index >= _speedChoices.Count)
            {
                next = 0f;
                return false;
            }

            next = _speedChoices[index];
            return true;
        }

        private static float Nearest(List<float> choices, float value)
        {
            float best = choices[0];

            foreach (float choice in choices)
            {
                if (Math.Abs(choice - value) < Math.Abs(best - value))
                    best = choice;
            }

            return best;
        }

        public void SetCharacterVolume(string id, float volume) => VoiceFor(id).Volume = Math.Clamp(volume, 0f, 1f);

        public void SetCharacterVoiceOn(string id, bool on) => VoiceFor(id).On = on;

        private KedSettings.CharacterVoice VoiceFor(string id)
        {
            if (!Values.CharacterVoices.TryGetValue(id, out KedSettings.CharacterVoice voice))
                Values.CharacterVoices[id] = voice = new KedSettings.CharacterVoice();

            return voice;
        }

        // 읽기만 — 없는 인물은 기본값.
        private KedSettings.CharacterVoice VoiceOf(string id) =>
            Values.CharacterVoices.TryGetValue(id, out KedSettings.CharacterVoice voice) ? voice : new KedSettings.CharacterVoice();

        #endregion

        // 지금 탭의 값을 기본값으로. 음량 탭의 채널 음량(배경음 · 효과음 · 음성)은 본체가 저장하므로 흐름이 되돌린다.
        public void ResetTab()
        {
            switch (Tab)
            {
                case SettingsPanel.Tab.Audio:
                    Values.MasterVolume = Defaults.MasterVolume;
                    Values.Muted = Defaults.Muted;
                    Values.CharacterVoices.Clear();
                    break;

                case SettingsPanel.Tab.Text:
                    Values.TextScale = Defaults.TextScale;
                    Values.TypeSpeed = Defaults.TypeSpeed;
                    Values.BoxOpacity = Defaults.BoxOpacity;
                    break;

                case SettingsPanel.Tab.Playback:
                    Values.AutoDelay = Defaults.AutoDelay;
                    Values.SpeedUpMultiplier = Defaults.SpeedUpMultiplier;
                    break;
            }
        }

        // 채널 음량은 본체가 들고 있어 흐름이 넘긴다. 인물은 Page가 보이는 순서(characterIds)대로.
        public AudioSettingsPage.Model AudioModel(float bgm, float sfx, float voice, IReadOnlyList<string> characterIds)
        {
            var characters = new AudioSettingsPage.CharacterVoice[characterIds.Count];

            for (int i = 0; i < characters.Length; i++)
            {
                KedSettings.CharacterVoice v = VoiceOf(characterIds[i]);
                characters[i] = new AudioSettingsPage.CharacterVoice { Volume = v.Volume, On = v.On };
            }

            return new AudioSettingsPage.Model
            {
                Master = Values.MasterVolume,
                Bgm = bgm,
                Sfx = sfx,
                Voice = voice,
                Muted = Values.Muted,
                CharactersExpanded = CharacterVoicesExpanded,
                Characters = characters,
            };
        }

        // 기록 › 캐릭터의 켜기/끄기. 같은 값을 쓴다.
        public CharactersPage.Model CharactersModel(IReadOnlyList<string> characterIds)
        {
            var on = new bool[characterIds.Count];

            for (int i = 0; i < on.Length; i++)
                on[i] = VoiceOf(characterIds[i]).On;

            return new CharactersPage.Model { VoiceOn = on };
        }

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
