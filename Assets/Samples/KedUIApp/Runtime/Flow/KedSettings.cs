using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 새 예제 UI의 설정 값. 연결할 수 있는 것만 둔다(PLAN U4 "실제로 동작하는 것만") — 캐릭터별 음성만 예외(아래 표).
    ///
    /// | 탭 | 항목 | 실제로 바뀌는 곳 |
    /// |---|---|---|
    /// | 음량 | 전체 음량 · 음소거 | AudioListener.volume (엔진 전체) — 이 값만 여기 저장 |
    /// | 음량 | 배경음 · 효과음 · 음성 | 본체 AudioVolumeSettings — 저장도 그쪽(PlayerPrefs Volume_*). 여기 두지 않는다 |
    /// | 음량 | 캐릭터별 음성(음량 · 켜기) | 아직 없다 — 화면만 만들고 값은 저장한다(2026-11-04 소유자 결정). 기록 › 캐릭터의 켜기/끄기도 같은 값 |
    /// | 대사 | 글자 크기 | UIPF UIContext.TextScale → 대사·선택지·백로그·팝업 본문 |
    /// | 대사 | 글자 출력 속도 | EllipsisBreathTypewriter.unitsPerSecond |
    /// | 대사 | 대사창 불투명도 | 대사창·이름표 그림의 투명도(0–100%, 0이면 그림 없이 글자만). 글자는 그대로 |
    /// | 재생 | 자동 진행 대기 | PlaybackSettings.autoModeDelaySeconds |
    /// | 재생 | 빠르게 재생 배속 | PlaybackSettings.speedupModeMultiplier (본체 하한 2배) |
    /// | 화면 | 창 모드 · 전체 화면 | Screen.fullScreenMode — 엔진이 스스로 기억한다. 에디터에서는 바뀌지 않는다 |
    ///
    /// 기본값 중 둘(출력 속도·자동 대기·배속)은 씬·본체에 적힌 값을 시작할 때 읽어 쓴다 — 여기서 새 기본을 만들지 않는다.
    /// 값 범위는 설정 상태(SettingsState)가 맞춘다. 여기는 값을 담고 읽고 쓰기만 한다.
    /// </summary>
    public sealed class KedSettings
    {
        private const string Prefix = "KedUI.";

        public float MasterVolume = 1f;
        public bool Muted;

        public float TextScale = 1f;
        public float TypeSpeed = 30f;
        public float BoxOpacity = 1f;

        public float AutoDelay = 1.5f;
        public float SpeedUpMultiplier = 12f;

        // 인물 id → 음성. 없는 인물은 기본(음량 1 · 켜짐).
        public Dictionary<string, CharacterVoice> CharacterVoices = new(StringComparer.Ordinal);

        public sealed class CharacterVoice
        {
            public float Volume = 1f;
            public bool On = true;
        }

        public KedSettings Clone()
        {
            var copy = (KedSettings)MemberwiseClone();
            copy.CharacterVoices = new Dictionary<string, CharacterVoice>(StringComparer.Ordinal);

            foreach (KeyValuePair<string, CharacterVoice> pair in CharacterVoices)
                copy.CharacterVoices[pair.Key] = new CharacterVoice { Volume = pair.Value.Volume, On = pair.Value.On };

            return copy;
        }

        /// <summary>저장된 값을 읽는다. 없는 값은 defaults의 값. 범위는 맞추지 않는다(SettingsState.Adopt가 맞춘다).</summary>
        public static KedSettings Load(KedSettings defaults)
        {
            KedSettings s = defaults.Clone();

            s.MasterVolume = PlayerPrefs.GetFloat(Prefix + "MasterVolume", defaults.MasterVolume);
            s.Muted = PlayerPrefs.GetInt(Prefix + "Muted", defaults.Muted ? 1 : 0) != 0;

            s.TextScale = PlayerPrefs.GetFloat(Prefix + "TextScale", defaults.TextScale);
            s.TypeSpeed = PlayerPrefs.GetFloat(Prefix + "TypeSpeed", defaults.TypeSpeed);
            s.BoxOpacity = PlayerPrefs.GetFloat(Prefix + "BoxOpacity", defaults.BoxOpacity);

            s.AutoDelay = PlayerPrefs.GetFloat(Prefix + "AutoDelay", defaults.AutoDelay);
            s.SpeedUpMultiplier = PlayerPrefs.GetFloat(Prefix + "SpeedUpMultiplier", defaults.SpeedUpMultiplier);

            if (PlayerPrefs.HasKey(Prefix + "CharacterVoices"))
                s.CharacterVoices = ReadCharacterVoices(PlayerPrefs.GetString(Prefix + "CharacterVoices"));

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

            PlayerPrefs.SetString(Prefix + "CharacterVoices", WriteCharacterVoices(CharacterVoices));
        }

        // "id:음량:켜기;…" — 음량은 0–1, 켜기는 1/0. 읽지 못한 줄은 건너뛴다.
        private static Dictionary<string, CharacterVoice> ReadCharacterVoices(string text)
        {
            var voices = new Dictionary<string, CharacterVoice>(StringComparer.Ordinal);

            foreach (string entry in (text ?? string.Empty).Split(';'))
            {
                string[] parts = entry.Split(':');
                if (parts.Length != 3 || parts[0].Length == 0)
                    continue;

                if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float volume))
                    continue;

                voices[parts[0]] = new CharacterVoice { Volume = volume, On = parts[2] != "0" };
            }

            return voices;
        }

        private static string WriteCharacterVoices(Dictionary<string, CharacterVoice> voices)
        {
            var sb = new StringBuilder();

            foreach (KeyValuePair<string, CharacterVoice> pair in voices)
            {
                if (sb.Length > 0)
                    sb.Append(';');

                sb.Append(pair.Key).Append(':')
                    .Append(pair.Value.Volume.ToString("0.###", CultureInfo.InvariantCulture)).Append(':')
                    .Append(pair.Value.On ? '1' : '0');
            }

            return sb.ToString();
        }
    }
}
