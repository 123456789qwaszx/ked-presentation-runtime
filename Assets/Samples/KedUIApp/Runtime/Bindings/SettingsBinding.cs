using System.Collections.Generic;
using UnityEngine;

namespace Ked.Samples.UI
{
    // 07 · 11 · 12 · 13 설정. 연결할 수 있는 항목만(KedSettings 표). 바꾸는 즉시 적용하고 저장한다.
    // 탭은 UIPF Page — SettingsPanel(Page Owner)의 PageRoot 아래 Page 넷을 SwitchPage로 바꿔 낀다. Page 이벤트도 그 Page를 열 때 묶는다.
    internal sealed class SettingsBinding : ScreenBinding
    {
        private const int AudioTab = 0;
        private const int TextTab = 1;
        private const int PlaybackTab = 2;
        private const int DisplayTab = 3;

        // 본체 AudioVolumeSettings.DefaultVolume과 같은 값(그쪽은 상수를 내놓지 않는다).
        private const float DefaultChannelVolume = 0.8f;

        private readonly UIManager _ui;
        private readonly SettingsPanel _panel;
        private readonly AudioSettingsPage _audioPage;
        private readonly TextSettingsPage _textPage;
        private readonly PlaybackSettingsPage _playbackPage;
        private readonly DisplaySettingsPage _displayPage;

        // 대사창 불투명도는 재생 화면에 그린다(그 화면의 바인딩은 PlayBinding).
        private readonly PlayScreen _play;

        private KedSettings _defaults;
        private KedSettings _values;
        private readonly List<float> _speedChoices = new();
        private int _tab;

        // ui는 글자 크기(UIContext)에만 쓴다 — 화면 전환은 흐름으로.
        public SettingsBinding(KedUIFlow flow, KedUIServices services, UIManager ui)
            : base(flow, services)
        {
            _ui = ui;
            _panel = Own(flow.Require<SettingsPanel>());
            _audioPage = Own(flow.Require<AudioSettingsPage>());
            _textPage = Own(flow.Require<TextSettingsPage>());
            _playbackPage = Own(flow.Require<PlaybackSettingsPage>());
            _displayPage = Own(flow.Require<DisplaySettingsPage>());
            _play = flow.Require<PlayScreen>();
        }

        private KedPlaybackBindings Playback => Services.Playback;

        // 기본값은 씬·본체에 적힌 값(출력 속도·자동 대기·배속)을 읽어 쓴다. 저장된 값이 있으면 그것을 적용한다.
        public override void OnConfigured()
        {
            PlaybackSettings playback = Playback.PlaybackState?.PlaybackSettings;

            _defaults = new KedSettings
            {
                MasterVolume = 1f,
                Muted = false,
                TextScale = 1f,
                TypeSpeed = Mathf.Clamp(
                    Playback.Typewriter != null ? Playback.Typewriter.unitsPerSecond : 30f,
                    KedSettings.MinTypeSpeed,
                    KedSettings.MaxTypeSpeed),
                BoxOpacity = 1f,
                AutoDelay = Mathf.Clamp(
                    playback != null ? playback.autoModeDelaySeconds : 1.5f,
                    KedSettings.MinAutoDelay,
                    KedSettings.MaxAutoDelay),
                SpeedUpMultiplier = playback != null ? playback.speedupModeMultiplier : 12f,
            };

            BuildSpeedChoices(playback);

            _values = KedSettings.Load(_defaults);

            if (!_speedChoices.Contains(_values.SpeedUpMultiplier))
                _values.SpeedUpMultiplier = _defaults.SpeedUpMultiplier;

            ApplyMasterVolume();
            ApplyTextScale();
            ApplyTypeSpeed();
            ApplyBoxOpacity();
            ApplyPlaybackSettings();
        }

        public override void Back(UIBase view) => Close();

        // 본체가 허용하는 배속(하한·상한) 안의 보기 셋. 마지막은 본체 기본값.
        private void BuildSpeedChoices(PlaybackSettings playback)
        {
            float min = playback != null ? playback.speedupModeMinMultiplier : 2f;
            float max = playback != null ? playback.speedupModeMaxMultiplier : 32f;

            _speedChoices.Clear();

            foreach (float candidate in new[] { 2f, 4f, _defaults.SpeedUpMultiplier })
            {
                float value = Mathf.Clamp(candidate, min, max);
                if (!_speedChoices.Contains(value))
                    _speedChoices.Add(value);
            }

            _speedChoices.Sort();
        }

        #region 열고 닫기 · 탭

        public void Open()
        {
            Flow.PushPanel<SettingsPanel>(panel =>
            {
                BindView(panel, ApplyBindings);
                SelectTab(_tab);
            });
        }

        private void Close()
        {
            _values?.Store();
            PlayerPrefs.Save();
            Flow.ClosePanel();
        }

        private void ApplyBindings(SettingsPanel panel)
        {
            AddBinding(panel,
                p => p.CloseClicked += Close,
                p => p.CloseClicked -= Close);

            AddBinding(panel,
                p => p.TabClicked += SelectTab,
                p => p.TabClicked -= SelectTab);

            AddBinding(panel,
                p => p.ResetClicked += ResetTab,
                p => p.ResetClicked -= ResetTab);
        }

        private void SelectTab(int tab)
        {
            _tab = Mathf.Clamp(tab, 0, SettingsPanel.TabCount - 1);
            _panel.SetSelectedTab(_tab);
            _panel.SetResetAvailable(_tab != DisplayTab);

            switch (_tab)
            {
                case AudioTab:
                    Flow.SwitchPage<AudioSettingsPage>(_panel, page =>
                    {
                        BindView(page, ApplyAudioBindings);
                        PresentAudioPage(page);
                    });
                    break;
                case TextTab:
                    Flow.SwitchPage<TextSettingsPage>(_panel, page =>
                    {
                        BindView(page, ApplyTextBindings);
                        PresentTextPage(page);
                    });
                    break;
                case PlaybackTab:
                    Flow.SwitchPage<PlaybackSettingsPage>(_panel, page =>
                    {
                        BindView(page, ApplyPlaybackBindings);
                        PresentPlaybackPage(page);
                    });
                    break;
                default:
                    Flow.SwitchPage<DisplaySettingsPage>(_panel, page =>
                    {
                        BindView(page, ApplyDisplayBindings);
                        PresentDisplayPage(page, Screen.fullScreen);
                    });
                    break;
            }

            _panel.FocusTab(_tab);
        }

        private void ResetTab()
        {
            switch (_tab)
            {
                case AudioTab:
                    _values.MasterVolume = _defaults.MasterVolume;
                    _values.Muted = _defaults.Muted;
                    ApplyMasterVolume();

                    AudioVolumeSettings volume = Volume;
                    if (volume != null)
                    {
                        volume.SetBgmVolume(DefaultChannelVolume);
                        volume.SetSfxVolume(DefaultChannelVolume);
                        volume.SetVoiceVolume(DefaultChannelVolume);
                    }

                    PresentAudioPage(_audioPage);
                    break;

                case TextTab:
                    _values.TextScale = _defaults.TextScale;
                    _values.TypeSpeed = _defaults.TypeSpeed;
                    _values.BoxOpacity = _defaults.BoxOpacity;
                    ApplyTextScale();
                    ApplyTypeSpeed();
                    ApplyBoxOpacity();
                    PresentTextPage(_textPage);
                    break;

                case PlaybackTab:
                    _values.AutoDelay = _defaults.AutoDelay;
                    _values.SpeedUpMultiplier = _defaults.SpeedUpMultiplier;
                    ApplyPlaybackSettings();
                    PresentPlaybackPage(_playbackPage);
                    break;
            }

            _values.Store();
        }

        #endregion

        #region 탭 묶기

        private void ApplyAudioBindings(AudioSettingsPage page)
        {
            AddBinding(page,
                p => p.VolumeChanged += HandleVolumeChanged,
                p => p.VolumeChanged -= HandleVolumeChanged);

            AddBinding(page,
                p => p.MuteChanged += HandleMuteChanged,
                p => p.MuteChanged -= HandleMuteChanged);
        }

        private void ApplyTextBindings(TextSettingsPage page)
        {
            AddBinding(page,
                p => p.TextScaleChanged += HandleTextScaleChanged,
                p => p.TextScaleChanged -= HandleTextScaleChanged);

            AddBinding(page,
                p => p.TypeSpeedChanged += HandleTypeSpeedChanged,
                p => p.TypeSpeedChanged -= HandleTypeSpeedChanged);

            AddBinding(page,
                p => p.BoxOpacityChanged += HandleBoxOpacityChanged,
                p => p.BoxOpacityChanged -= HandleBoxOpacityChanged);
        }

        private void ApplyPlaybackBindings(PlaybackSettingsPage page)
        {
            AddBinding(page,
                p => p.AutoDelayChanged += HandleAutoDelayChanged,
                p => p.AutoDelayChanged -= HandleAutoDelayChanged);

            AddBinding(page,
                p => p.SpeedChoiceClicked += HandleSpeedChoiceClicked,
                p => p.SpeedChoiceClicked -= HandleSpeedChoiceClicked);
        }

        private void ApplyDisplayBindings(DisplaySettingsPage page)
        {
            AddBinding(page,
                p => p.WindowModeClicked += HandleWindowModeClicked,
                p => p.WindowModeClicked -= HandleWindowModeClicked);
        }

        #endregion

        #region 탭 그리기

        private void PresentAudioPage(AudioSettingsPage page)
        {
            AudioVolumeSettings volume = Volume;

            page.Present(new AudioSettingsPage.Model
            {
                Master = _values.MasterVolume,
                Bgm = volume != null ? volume.BgmVolume : DefaultChannelVolume,
                Sfx = volume != null ? volume.SfxVolume : DefaultChannelVolume,
                Voice = volume != null ? volume.VoiceVolume : DefaultChannelVolume,
                Muted = _values.Muted,
            });
        }

        private void PresentTextPage(TextSettingsPage page)
        {
            page.Present(new TextSettingsPage.Model
            {
                TextScale = _values.TextScale,
                TypeSpeed = _values.TypeSpeed,
                BoxOpacity = _values.BoxOpacity,
            });
        }

        private void PresentPlaybackPage(PlaybackSettingsPage page)
        {
            page.Present(new PlaybackSettingsPage.Model
            {
                AutoDelay = _values.AutoDelay,
                SpeedChoices = _speedChoices,
                SpeedUpMultiplier = _values.SpeedUpMultiplier,
                FormatSeconds = seconds => Format("settings.playback.seconds", seconds.ToString("0.0")),
            });
        }

        private void PresentDisplayPage(DisplaySettingsPage page, bool fullscreen)
        {
            page.Present(new DisplaySettingsPage.Model
            {
                Fullscreen = fullscreen,
                Note = Application.isEditor ? Text("settings.display.editorNote") : string.Empty,
            });
        }

        #endregion

        #region 바뀜 → 적용 · 저장

        private void HandleVolumeChanged(AudioSettingsPage.Channel channel, float value)
        {
            AudioVolumeSettings volume = Volume;

            switch (channel)
            {
                case AudioSettingsPage.Channel.Master:
                    _values.MasterVolume = Mathf.Clamp01(value);
                    ApplyMasterVolume();
                    _values.Store();
                    break;
                case AudioSettingsPage.Channel.Bgm:
                    volume?.SetBgmVolume(value);
                    break;
                case AudioSettingsPage.Channel.Sfx:
                    volume?.SetSfxVolume(value);
                    break;
                case AudioSettingsPage.Channel.Voice:
                    volume?.SetVoiceVolume(value);
                    break;
            }
        }

        private void HandleMuteChanged(bool muted)
        {
            _values.Muted = muted;
            ApplyMasterVolume();
            _values.Store();
        }

        private void HandleTextScaleChanged(float scale)
        {
            _values.TextScale = scale;
            ApplyTextScale();
            _values.Store();
        }

        private void HandleTypeSpeedChanged(float speed)
        {
            _values.TypeSpeed = speed;
            ApplyTypeSpeed();
            _values.Store();
        }

        private void HandleBoxOpacityChanged(float opacity)
        {
            _values.BoxOpacity = opacity;
            ApplyBoxOpacity();
            _values.Store();
        }

        private void HandleAutoDelayChanged(float seconds)
        {
            _values.AutoDelay = seconds;
            ApplyPlaybackSettings();
            _values.Store();
        }

        private void HandleSpeedChoiceClicked(float multiplier)
        {
            _values.SpeedUpMultiplier = multiplier;
            ApplyPlaybackSettings();
            _values.Store();
            PresentPlaybackPage(_playbackPage);
        }

        private void HandleWindowModeClicked(bool fullscreen)
        {
            Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            PresentDisplayPage(_displayPage, fullscreen);
        }

        #endregion

        #region 적용

        private AudioVolumeSettings Volume => Playback?.Audio != null ? Playback.Audio.Volume : null;

        private void ApplyMasterVolume()
        {
            AudioListener.volume = _values.Muted ? 0f : Mathf.Clamp01(_values.MasterVolume);
        }

        // 글자 크기는 UIPF의 사용자 문맥(UIContext.TextScale)이다. 바꾸면 보이는 화면에 바로 다시 적용된다.
        private void ApplyTextScale()
        {
            UIContext context = _ui.Context;
            _ui.SetContext(context.With(context.ThemeId, context.LocaleId, _values.TextScale));
        }

        private void ApplyTypeSpeed()
        {
            if (Playback.Typewriter != null)
                Playback.Typewriter.unitsPerSecond = _values.TypeSpeed;
        }

        private void ApplyBoxOpacity()
        {
            _play.SetDialogueOpacity(_values.BoxOpacity);
        }

        // 배속은 다음에 빠르게 재생을 켤 때부터 적용된다(본체가 켤 때 배속을 읽는다).
        private void ApplyPlaybackSettings()
        {
            PlaybackSettings playback = Playback.PlaybackState?.PlaybackSettings;
            if (playback == null)
                return;

            playback.autoModeDelaySeconds = _values.AutoDelay;
            playback.speedupModeMultiplier = Mathf.Clamp(
                _values.SpeedUpMultiplier,
                playback.speedupModeMinMultiplier,
                playback.speedupModeMaxMultiplier);
        }

        #endregion
    }
}
