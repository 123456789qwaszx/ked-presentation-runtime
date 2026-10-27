using System.Collections.Generic;
using UnityEngine;

namespace Ked.Samples.UI
{
    // 07 · 11 · 12 · 13 설정. 연결할 수 있는 항목만(KedSettings 표). 바꾸는 즉시 적용하고 저장한다.
    // 탭은 UIPF Page — SettingsPanel(Page Owner)의 PageRoot 아래 Page 넷을 SwitchPage로 바꿔 낀다.
    public sealed partial class KedUIFlow
    {
        private const int AudioTab = 0;
        private const int TextTab = 1;
        private const int PlaybackTab = 2;
        private const int DisplayTab = 3;

        // 본체 AudioVolumeSettings.DefaultVolume과 같은 값(그쪽은 상수를 내놓지 않는다).
        private const float DefaultChannelVolume = 0.8f;

        private KedSettings _settingsDefaults;
        private KedSettings _values;
        private readonly List<float> _speedChoices = new();
        private int _settingsTab;

        private void SubscribeSettings()
        {
            _settings.CloseClicked += CloseSettings;
            _settings.TabClicked += SelectSettingsTab;
            _settings.ResetClicked += ResetSettingsTab;

            _audioPage.VolumeChanged += HandleVolumeChanged;
            _audioPage.MuteChanged += muted =>
            {
                _values.Muted = muted;
                ApplyMasterVolume();
                _values.Store();
            };

            _textPage.TextScaleChanged += scale =>
            {
                _values.TextScale = scale;
                ApplyTextScale();
                _values.Store();
            };

            _textPage.TypeSpeedChanged += speed =>
            {
                _values.TypeSpeed = speed;
                ApplyTypeSpeed();
                _values.Store();
            };

            _textPage.BoxOpacityChanged += opacity =>
            {
                _values.BoxOpacity = opacity;
                ApplyBoxOpacity();
                _values.Store();
            };

            _playbackPage.AutoDelayChanged += seconds =>
            {
                _values.AutoDelay = seconds;
                ApplyPlaybackSettings();
                _values.Store();
            };

            _playbackPage.SpeedChoiceClicked += multiplier =>
            {
                _values.SpeedUpMultiplier = multiplier;
                ApplyPlaybackSettings();
                _values.Store();
                PresentPlaybackPage(_playbackPage);
            };

            _displayPage.WindowModeClicked += fullscreen =>
            {
                Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                PresentDisplayPage(_displayPage, fullscreen);
            };
        }

        // 기본값은 씬·본체에 적힌 값(출력 속도·자동 대기·배속)을 읽어 쓴다. 저장된 값이 있으면 그것을 적용한다.
        private void InitializeSettings()
        {
            VNPlaybackSettings playback = _playback.PlaybackState?.PlaybackSettings;

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

            _values = KedSettings.Load(_settingsDefaults);

            if (!_speedChoices.Contains(_values.SpeedUpMultiplier))
                _values.SpeedUpMultiplier = _settingsDefaults.SpeedUpMultiplier;

            ApplyMasterVolume();
            ApplyTextScale();
            ApplyTypeSpeed();
            ApplyBoxOpacity();
            ApplyPlaybackSettings();
        }

        // 본체가 허용하는 배속(하한·상한) 안의 보기 셋. 마지막은 본체 기본값.
        private void BuildSpeedChoices(VNPlaybackSettings playback)
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
            PushPanel<SettingsPanel>(_ => SelectSettingsTab(_settingsTab));
        }

        private void CloseSettings()
        {
            _values?.Store();
            PlayerPrefs.Save();
            ClosePanel();
        }

        private void SelectSettingsTab(int tab)
        {
            _settingsTab = Mathf.Clamp(tab, 0, SettingsPanel.TabCount - 1);
            _settings.SetSelectedTab(_settingsTab);
            _settings.SetResetAvailable(_settingsTab != DisplayTab);

            switch (_settingsTab)
            {
                case AudioTab:
                    _ui.SwitchPage<AudioSettingsPage>(_settings, Presentation<AudioSettingsPage>(), PresentAudioPage);
                    break;
                case TextTab:
                    _ui.SwitchPage<TextSettingsPage>(_settings, Presentation<TextSettingsPage>(), PresentTextPage);
                    break;
                case PlaybackTab:
                    _ui.SwitchPage<PlaybackSettingsPage>(_settings, Presentation<PlaybackSettingsPage>(), PresentPlaybackPage);
                    break;
                default:
                    _ui.SwitchPage<DisplaySettingsPage>(
                        _settings,
                        Presentation<DisplaySettingsPage>(),
                        page => PresentDisplayPage(page, Screen.fullScreen));
                    break;
            }

            _settings.FocusTab(_settingsTab);
        }

        private void ResetSettingsTab()
        {
            switch (_settingsTab)
            {
                case AudioTab:
                    _values.MasterVolume = _settingsDefaults.MasterVolume;
                    _values.Muted = _settingsDefaults.Muted;
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
                    _values.TextScale = _settingsDefaults.TextScale;
                    _values.TypeSpeed = _settingsDefaults.TypeSpeed;
                    _values.BoxOpacity = _settingsDefaults.BoxOpacity;
                    ApplyTextScale();
                    ApplyTypeSpeed();
                    ApplyBoxOpacity();
                    PresentTextPage(_textPage);
                    break;

                case PlaybackTab:
                    _values.AutoDelay = _settingsDefaults.AutoDelay;
                    _values.SpeedUpMultiplier = _settingsDefaults.SpeedUpMultiplier;
                    ApplyPlaybackSettings();
                    PresentPlaybackPage(_playbackPage);
                    break;
            }

            _values.Store();
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

        #region 적용

        private AudioVolumeSettings Volume => _playback?.Audio != null ? _playback.Audio.Volume : null;

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
            if (_playback.Typewriter != null)
                _playback.Typewriter.unitsPerSecond = _values.TypeSpeed;
        }

        private void ApplyBoxOpacity()
        {
            _play.SetDialogueOpacity(_values.BoxOpacity);
        }

        // 배속은 다음에 빠르게 재생을 켤 때부터 적용된다(본체가 켤 때 배속을 읽는다).
        private void ApplyPlaybackSettings()
        {
            VNPlaybackSettings playback = _playback.PlaybackState?.PlaybackSettings;
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
