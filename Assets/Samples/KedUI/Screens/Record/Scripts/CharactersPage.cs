using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 기록 · 캐릭터 탭. 인물 카드 셋(그림 · 이름 · 소개)과 인물마다 음성 켜기/끄기.
    /// 인물 내용과 id는 빌드 명세의 고정 내용이라 프리팹에 들어 있다. 켜기/끄기는 설정 › 음량의 캐릭터별 음성과 같은 값이다.
    /// </summary>
    [UIViewKey("Ked.CharactersPage")]
    public sealed class CharactersPage : UIPage<CharactersPage.Refs>
    {
        public const int CardCount = 3;

        public enum Refs
        {
            CharacterCard_0,
            CharacterVoiceLabel_0,
            CharacterVoice_0,
            CharacterCard_1,
            CharacterVoiceLabel_1,
            CharacterVoice_1,
            CharacterCard_2,
            CharacterVoiceLabel_2,
            CharacterVoice_2,
            CharactersHint,
        }

        // CharacterIds 순서.
        public sealed class Model
        {
            public bool[] VoiceOn;
        }

        public event Action<string, bool> VoiceToggled;

        // 카드 순서의 인물 id. 명세가 넣는다.
        [SerializeField] private string[] characterIds = Array.Empty<string>();

        private static readonly Refs[] VoiceRefs = { Refs.CharacterVoice_0, Refs.CharacterVoice_1, Refs.CharacterVoice_2 };

        private readonly KedToggleSwitch[] _switches = new KedToggleSwitch[CardCount];

        public IReadOnlyList<string> CharacterIds => characterIds;

        protected override void OnInitialize()
        {
            for (int i = 0; i < CardCount && i < characterIds.Length; i++)
            {
                string id = characterIds[i];

                _switches[i] = View.Component<KedToggleSwitch>(VoiceRefs[i]);
                Toggle toggle = View.Component<Toggle>(VoiceRefs[i]);
                if (toggle != null)
                    toggle.onValueChanged.AddListener(on => VoiceToggled?.Invoke(id, on));
            }
        }

        public void Present(Model model)
        {
            for (int i = 0; i < _switches.Length; i++)
            {
                if (model.VoiceOn != null && i < model.VoiceOn.Length)
                    _switches[i]?.SetWithoutNotify(model.VoiceOn[i]);
            }
        }

        public Selectable FirstSelectable => View.Component<Toggle>(Refs.CharacterVoice_0);
    }
}
