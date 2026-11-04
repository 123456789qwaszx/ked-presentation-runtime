using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 07 설정 · 음량 탭. UIPF Page(SettingsPanel의 PageRoot 아래). 값은 0–100으로 보이고 0–1로 넘긴다.
    /// 음성 줄 아래에 캐릭터별 음성(음량 · 켜기/끄기)을 펼친다. 인물과 id는 빌드 명세의 고정 내용이라 프리팹에 들어 있다.
    /// </summary>
    [UIViewKey("Ked.AudioSettingsPage")]
    public sealed class AudioSettingsPage : UIPage<AudioSettingsPage.Refs>
    {
        public const int CharacterCount = 3;

        public enum Channel
        {
            Master = 0,
            Bgm = 1,
            Sfx = 2,
            Voice = 3,
        }

        public enum Refs
        {
            AudioHeading,
            AudioDescription,
            AudioLabel_0,
            AudioSlider_0,
            AudioValue_0,
            AudioLabel_1,
            AudioSlider_1,
            AudioValue_1,
            AudioLabel_2,
            AudioSlider_2,
            AudioValue_2,
            AudioLabel_3,
            AudioSlider_3,
            AudioValue_3,

            CharactersToggle,
            CharactersToggleLabel,
            CharactersToggleArrow,
            CharacterVoices,
            CharacterRowBody_0,
            CharacterName_0,
            CharacterSlider_0,
            CharacterValue_0,
            CharacterVoice_0,
            CharacterRowBody_1,
            CharacterName_1,
            CharacterSlider_1,
            CharacterValue_1,
            CharacterVoice_1,
            CharacterRowBody_2,
            CharacterName_2,
            CharacterSlider_2,
            CharacterValue_2,
            CharacterVoice_2,

            MuteLabel,
            MuteToggle,
        }

        public sealed class CharacterVoice
        {
            public float Volume;
            public bool On;
        }

        public sealed class Model
        {
            public float Master;
            public float Bgm;
            public float Sfx;
            public float Voice;
            public bool Muted;
            public bool CharactersExpanded;
            public CharacterVoice[] Characters;     // CharacterIds 순서
        }

        public event Action<Channel, float> VolumeChanged;
        public event Action<bool> MuteChanged;
        public event Action CharactersToggleClicked;
        public event Action<string, float> CharacterVolumeChanged;
        public event Action<string, bool> CharacterVoiceToggled;

        // 줄 순서의 인물 id. 명세가 넣는다.
        [SerializeField] private string[] characterIds = Array.Empty<string>();

        // 접으면 음소거 줄이 올라가는 거리(시안 px). 명세가 넣는다.
        [SerializeField] private float collapsedShift;

        private static readonly Refs[] BodyRefs = { Refs.CharacterRowBody_0, Refs.CharacterRowBody_1, Refs.CharacterRowBody_2 };
        private static readonly Refs[] SliderRefs = { Refs.CharacterSlider_0, Refs.CharacterSlider_1, Refs.CharacterSlider_2 };
        private static readonly Refs[] ValueRefs = { Refs.CharacterValue_0, Refs.CharacterValue_1, Refs.CharacterValue_2 };
        private static readonly Refs[] VoiceRefs = { Refs.CharacterVoice_0, Refs.CharacterVoice_1, Refs.CharacterVoice_2 };

        private readonly KedSliderField[] _fields = new KedSliderField[4];
        private readonly KedSliderField[] _characterFields = new KedSliderField[CharacterCount];
        private readonly KedToggleSwitch[] _characterSwitches = new KedToggleSwitch[CharacterCount];
        private readonly CanvasGroup[] _characterBodies = new CanvasGroup[CharacterCount];
        private KedToggleSwitch _mute;

        private RectTransform _muteLabel;
        private RectTransform _muteToggle;
        private Vector2 _muteLabelAt;
        private Vector2 _muteToggleAt;

        public IReadOnlyList<string> CharacterIds => characterIds;

        protected override void OnInitialize()
        {
            Refs[] sliders = { Refs.AudioSlider_0, Refs.AudioSlider_1, Refs.AudioSlider_2, Refs.AudioSlider_3 };
            Refs[] values = { Refs.AudioValue_0, Refs.AudioValue_1, Refs.AudioValue_2, Refs.AudioValue_3 };

            for (int i = 0; i < sliders.Length; i++)
            {
                Channel channel = (Channel)i;

                _fields[i] = new KedSliderField(
                    View.Component<Slider>(sliders[i]),
                    View.Text(values[i]),
                    0f, 100f, wholeNumbers: true,
                    format: v => v.ToString("0"),
                    changed: v => VolumeChanged?.Invoke(channel, v / 100f));
            }

            KedUIUtil.OnClick(View.Button(Refs.CharactersToggle), () => CharactersToggleClicked?.Invoke());

            for (int i = 0; i < CharacterCount && i < characterIds.Length; i++)
            {
                string id = characterIds[i];

                _characterFields[i] = new KedSliderField(
                    View.Component<Slider>(SliderRefs[i]),
                    View.Text(ValueRefs[i]),
                    0f, 100f, wholeNumbers: true,
                    format: v => v.ToString("0"),
                    changed: v => CharacterVolumeChanged?.Invoke(id, v / 100f));

                _characterSwitches[i] = View.Component<KedToggleSwitch>(VoiceRefs[i]);
                Toggle voice = View.Component<Toggle>(VoiceRefs[i]);
                if (voice != null)
                    voice.onValueChanged.AddListener(on => CharacterVoiceToggled?.Invoke(id, on));

                _characterBodies[i] = View.Component<CanvasGroup>(BodyRefs[i]);
            }

            _mute = View.Component<KedToggleSwitch>(Refs.MuteToggle);
            Toggle toggle = View.Component<Toggle>(Refs.MuteToggle);
            if (toggle != null)
                toggle.onValueChanged.AddListener(on => MuteChanged?.Invoke(on));

            _muteLabel = View.Rect(Refs.MuteLabel);
            _muteToggle = View.Rect(Refs.MuteToggle);
            _muteLabelAt = _muteLabel != null ? _muteLabel.anchoredPosition : Vector2.zero;
            _muteToggleAt = _muteToggle != null ? _muteToggle.anchoredPosition : Vector2.zero;
        }

        public void Present(Model model)
        {
            _fields[(int)Channel.Master]?.Set(model.Master * 100f);
            _fields[(int)Channel.Bgm]?.Set(model.Bgm * 100f);
            _fields[(int)Channel.Sfx]?.Set(model.Sfx * 100f);
            _fields[(int)Channel.Voice]?.Set(model.Voice * 100f);
            _mute?.SetWithoutNotify(model.Muted);

            for (int i = 0; i < CharacterCount; i++)
            {
                if (model.Characters == null || i >= model.Characters.Length)
                    continue;

                CharacterVoice voice = model.Characters[i];
                _characterFields[i]?.Set(voice.Volume * 100f);
                _characterSwitches[i]?.SetWithoutNotify(voice.On);

                // 꺼진 인물은 음량 줄을 흐리게 하고 막는다. 켜기/끄기는 늘 만질 수 있다.
                CanvasGroup body = _characterBodies[i];
                if (body != null)
                {
                    body.alpha = voice.On ? 1f : KedColors.DisabledAlpha;
                    body.interactable = voice.On;
                }
            }

            ShowCharacters(model.CharactersExpanded);
        }

        // 접으면 목록을 숨기고 음소거 줄을 그 자리로 올린다.
        private void ShowCharacters(bool expanded)
        {
            KedUIUtil.SetActive(View.Rect(Refs.CharacterVoices), expanded);
            KedUIUtil.SetText(View.Text(Refs.CharactersToggleArrow), expanded ? "▲" : "▼");

            Vector2 shift = expanded ? Vector2.zero : new Vector2(0f, collapsedShift);

            if (_muteLabel != null)
                _muteLabel.anchoredPosition = _muteLabelAt + shift;

            if (_muteToggle != null)
                _muteToggle.anchoredPosition = _muteToggleAt + shift;
        }

        public Selectable FirstSelectable => _fields[0]?.Slider;
    }
}
