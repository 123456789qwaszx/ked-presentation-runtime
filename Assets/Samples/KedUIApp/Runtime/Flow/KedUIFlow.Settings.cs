using System.Collections.Generic;
using UnityEngine;

namespace Ked.Samples.UI
{
    // 07 · 11 · 12 · 13 설정. 연결할 수 있는 항목만(KedSettings 표). 바꾸는 즉시 적용하고 저장한다.
    // 탭은 UIPF Page — SettingsPanel(Page Owner)의 PageRoot 아래 Page 넷을 SwitchPage로 바꿔 낀다. Page 이벤트도 그 Page를 열 때 묶는다.
    // Esc는 저장하고 닫는다(HandleCancel → CloseSettings).
    public sealed partial class KedUIFlow
    {
        private const int AudioTab = 0;
        private const int TextTab = 1;
        private const int PlaybackTab = 2;
        private const int DisplayTab = 3;

        // 본체 AudioVolumeSettings.DefaultVolume과 같은 값(그쪽은 상수를 내놓지 않는다).
        private const float DefaultChannelVolume = 0.8f;

        private readonly SettingsPanel _settingsPanel;
        private readonly AudioSettingsPage _audioPage;
        private readonly TextSettingsPage _textPage;
        private readonly PlaybackSettingsPage _playbackPage;
        private readonly DisplaySettingsPage _displayPage;

        private KedSettings _settingsDefaults;
        private KedSettings _settingsValues;
        private readonly List<float> _speedChoices = new();
        private int _settingsTab;

        // 기본값은 씬·본체에 적힌 값(출력 속도·자동 대기·배속)을 읽어 쓴다. 저장된 값이 있으면 그것을 적용한다.
        private void ConfigureSettings()
        {
            PlaybackSettings playback = _playback.PlaybackState?.PlaybackSettings;

            _settingsDefaults = new KedSettings
            {
                MasterVolume = 1f,
                Muted = false,
                TextScale = 1f,
                TypeSpeed = Mathf.Clamp(
                    _playback.Typewriter != null ? _playback.Typewriter.unitsPerSecond : 30f,
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

            _settingsValues = KedSettings.Load(_settingsDefaults);

            if (!_speedChoices.Contains(_settingsValues.SpeedUpMultiplier))
                _settingsValues.SpeedUpMultiplier = _settingsDefaults.SpeedUpMultiplier;

            ApplyMasterVolume();
            ApplyTextScale();
            ApplyTypeSpeed();
            ApplyBoxOpacity();
            ApplyPlaybackSettings();
        }

        // 본체가 허용하는 배속(하한·상한) 안의 보기 셋. 마지막은 본체 기본값.
        private void BuildSpeedChoices(PlaybackSettings playback)
        {
            float min = playback != null ? playback.speedupModeMinMultiplier : 2f;
            float max = playback != null ? playback.speedupModeMaxMultiplier : 32f;

            _speedChoices.Clear();

            foreach (float candidate in new[] { 2f, 4f, _settingsDefaults.SpeedUpMultiplier })
            {
                float value = Mathf.Clamp(candidate, min, max);
                if (!_speedChoices.Contains(value))
                    _speedChoices.Add(value);
            }

            _speedChoices.Sort();
        }

        #region 열고 닫기 · 탭

        private void OpenSettings()
        {
            PushPanel<SettingsPanel>(panel =>
            {
                BindView(panel, ApplySettingsBindings);
                SelectTab(_settingsTab);
            });
        }

        private void CloseSettings()
        {
            _settingsValues?.Store();
            PlayerPrefs.Save();
            ClosePanel();
        }

        private void ApplySettingsBindings(SettingsPanel panel)
        {
            AddBinding(panel,
                p => p.CloseClicked += CloseSettings,
                p => p.CloseClicked -= CloseSettings);

            AddBinding(panel,
                p => p.TabClicked += SelectTab,
                p => p.TabClicked -= SelectTab);

            AddBinding(panel,
                p => p.ResetClicked += ResetTab,
                p => p.ResetClicked -= ResetTab);
        }

        private void SelectTab(int tab)
        {
            _settingsTab = Mathf.Clamp(tab, 0, SettingsPanel.TabCount - 1);
            _settingsPanel.SetSelectedTab(_settingsTab);
            _settingsPanel.SetResetAvailable(_settingsTab != DisplayTab);

            switch (_settingsTab)
            {
                case AudioTab:
                    SwitchPage<AudioSettingsPage>(_settingsPanel, page =>
                    {
                        BindView(page, ApplyAudioBindings);
                        PresentAudioPage(page);
                    });
                    break;
                case TextTab:
                    SwitchPage<TextSettingsPage>(_settingsPanel, page =>
                    {
                        BindView(page, ApplyTextBindings);
                        PresentTextPage(page);
                    });
                    break;
                case PlaybackTab:
                    SwitchPage<PlaybackSettingsPage>(_settingsPanel, page =>
                    {
                        BindView(page, ApplyPlaybackBindings);
                        PresentPlaybackPage(page);
                    });
                    break;
                default:
                    SwitchPage<DisplaySettingsPage>(_settingsPanel, page =>
                    {
                        BindView(page, ApplyDisplayBindings);
                        PresentDisplayPage(page, Screen.fullScreen);
                    });
                    break;
            }

            _settingsPanel.FocusTab(_settingsTab);
        }

        private void ResetTab()
        {
            switch (_settingsTab)
            {
                case AudioTab:
                    _settingsValues.MasterVolume = _settingsDefaults.MasterVolume;
                    _settingsValues.Muted = _settingsDefaults.Muted;
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
                    _settingsValues.TextScale = _settingsDefaults.TextScale;
                    _settingsValues.TypeSpeed = _settingsDefaults.TypeSpeed;
                    _settingsValues.BoxOpacity = _settingsDefaults.BoxOpacity;
                    ApplyTextScale();
                    ApplyTypeSpeed();
                    ApplyBoxOpacity();
                    PresentTextPage(_textPage);
                    break;

                case PlaybackTab:
                    _settingsValues.AutoDelay = _settingsDefaults.AutoDelay;
                    _settingsValues.SpeedUpMultiplier = _settingsDefaults.SpeedUpMultiplier;
                    ApplyPlaybackSettings();
                    PresentPlaybackPage(_playbackPage);
                    break;
            }

            _settingsValues.Store();
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
                Master = _settingsValues.MasterVolume,
                Bgm = volume != null ? volume.BgmVolume : DefaultChannelVolume,
                Sfx = volume != null ? volume.SfxVolume : DefaultChannelVolume,
                Voice = volume != null ? volume.VoiceVolume : DefaultChannelVolume,
                Muted = _settingsValues.Muted,
            });
        }

        private void PresentTextPage(TextSettingsPage page)
        {
            page.Present(new TextSettingsPage.Model
            {
                TextScale = _settingsValues.TextScale,
                TypeSpeed = _settingsValues.TypeSpeed,
                BoxOpacity = _settingsValues.BoxOpacity,
            });
        }

        private void PresentPlaybackPage(PlaybackSettingsPage page)
        {
            page.Present(new PlaybackSettingsPage.Model
            {
                AutoDelay = _settingsValues.AutoDelay,
                SpeedChoices = _speedChoices,
                SpeedUpMultiplier = _settingsValues.SpeedUpMultiplier,
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
                    _settingsValues.MasterVolume = Mathf.Clamp01(value);
                    ApplyMasterVolume();
                    _settingsValues.Store();
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
            _settingsValues.Muted = muted;
            ApplyMasterVolume();
            _settingsValues.Store();
        }

        private void HandleTextScaleChanged(float scale)
        {
            _settingsValues.TextScale = scale;
            ApplyTextScale();
            _settingsValues.Store();
        }

        private void HandleTypeSpeedChanged(float speed)
        {
            _settingsValues.TypeSpeed = speed;
            ApplyTypeSpeed();
            _settingsValues.Store();
        }

        private void HandleBoxOpacityChanged(float opacity)
        {
            _settingsValues.BoxOpacity = opacity;
            ApplyBoxOpacity();
            _settingsValues.Store();
        }

        private void HandleAutoDelayChanged(float seconds)
        {
            _settingsValues.AutoDelay = seconds;
            ApplyPlaybackSettings();
            _settingsValues.Store();
        }

        private void HandleSpeedChoiceClicked(float multiplier)
        {
            _settingsValues.SpeedUpMultiplier = multiplier;
            ApplyPlaybackSettings();
            _settingsValues.Store();
            PresentPlaybackPage(_playbackPage);
        }

        private void HandleWindowModeClicked(bool fullscreen)
        {
            Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            PresentDisplayPage(_displayPage, fullscreen);
        }

        #endregion

        #region 적용

        private AudioVolumeSettings Volume => _playback?.Audio != null ? _playback.Audio.Volume : null;

        private void ApplyMasterVolume()
        {
            AudioListener.volume = _settingsValues.Muted ? 0f : Mathf.Clamp01(_settingsValues.MasterVolume);
        }

        // 글자 크기는 UIPF의 사용자 문맥(UIContext.TextScale)이다. 바꾸면 보이는 화면에 바로 다시 적용된다.
        private void ApplyTextScale()
        {
            UIContext context = _ui.Context;
            _ui.SetContext(context.With(context.ThemeId, context.LocaleId, _settingsValues.TextScale));
        }

        private void ApplyTypeSpeed()
        {
            if (_playback.Typewriter != null)
                _playback.Typewriter.unitsPerSecond = _settingsValues.TypeSpeed;
        }

        // 대사창 불투명도는 재생 화면에 그린다.
        private void ApplyBoxOpacity()
        {
            _playScreen.SetDialogueOpacity(_settingsValues.BoxOpacity);
        }

        // 배속은 다음에 빠르게 재생을 켤 때부터 적용된다(본체가 켤 때 배속을 읽는다).
        private void ApplyPlaybackSettings()
        {
            PlaybackSettings playback = _playback.PlaybackState?.PlaybackSettings;
            if (playback == null)
                return;

            playback.autoModeDelaySeconds = _settingsValues.AutoDelay;
            playback.speedupModeMultiplier = Mathf.Clamp(
                _settingsValues.SpeedUpMultiplier,
                playback.speedupModeMinMultiplier,
                playback.speedupModeMaxMultiplier);
        }

        #endregion
    }
}
