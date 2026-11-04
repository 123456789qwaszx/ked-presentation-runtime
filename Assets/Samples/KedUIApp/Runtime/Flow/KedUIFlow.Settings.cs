using UnityEngine;

namespace Ked.Samples.UI
{
    // 07 · 11 · 12 · 13 설정 — 화면 연결, 값을 엔진 · 본체에 적용, 저장. 바꾸는 즉시 적용하고 저장한다.
    // 기본값 · 값 범위 · 배속 보기 · 탭 · 되돌리기는 SettingsState가, 글자는 각 Page가 정한다. 값은 상태의 Set*으로만 바꾼다.
    // 탭은 UIPF Page — SettingsPanel(Page Owner)의 PageRoot 아래 Page 넷을 SwitchPage로 바꿔 낀다. Page 이벤트도 그 Page를 열 때 묶는다.
    // Esc는 저장하고 닫는다(HandleCancel → CloseSettings).
    public sealed partial class KedUIFlow
    {
        // 본체 AudioVolumeSettings.DefaultVolume과 같은 값(그쪽은 상수를 내놓지 않는다).
        private const float DefaultChannelVolume = 0.8f;

        private readonly SettingsState _settings = new();

        private KedSettings SettingsValues => _settings.Values;

        // 기본값은 씬 · 본체에 적힌 값(출력 속도 · 자동 대기 · 배속)을 읽어 쓴다. 저장된 값이 있으면 그것을 적용한다.
        private void ConfigureSettings()
        {
            PlaybackSettings playback = _playback.PlaybackState?.PlaybackSettings;

            _settings.SetDefaults(
                typeSpeed: _playback.Typewriter != null ? _playback.Typewriter.unitsPerSecond : 30f,
                autoDelay: playback != null ? playback.autoModeDelaySeconds : 1.5f,
                speedUp: playback != null ? playback.speedupModeMultiplier : 12f,
                minMultiplier: playback != null ? playback.speedupModeMinMultiplier : 2f,
                maxMultiplier: playback != null ? playback.speedupModeMaxMultiplier : 32f);

            _settings.Adopt(KedSettings.Load(_settings.Defaults));
            ApplySettings();
        }

        #region 열고 닫기 · 탭

        private void OpenSettings()
        {
            _ui.PushPanel<SettingsPanel>(Presentation<SettingsPanel>(), panel =>
            {
                BindView(panel, ApplySettingsBindings);
                SelectTab(_settings.Tab);
            }, Unbind);
        }

        private void CloseSettings()
        {
            SettingsValues.Store();
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

        private void SelectTab(SettingsPanel.Tab tab)
        {
            SettingsPanel panel = _ui.GetUI<SettingsPanel>();

            _settings.Tab = tab;
            panel.SetSelectedTab(tab);
            panel.SetResetAvailable(_settings.CanResetTab);
            ShowTabPage();
            panel.FocusTab(tab);
        }

        // 값은 상태가 기본값으로 되돌린다. 채널 음량만 본체가 들고 있어 여기서 되돌린다.
        // 바뀐 값은 모두 다시 적용하고(같은 값은 그대로다) 지금 Page를 다시 그린다.
        private void ResetTab()
        {
            _settings.ResetTab();

            if (_settings.Tab == SettingsPanel.Tab.Audio)
            {
                AudioVolumeSettings volume = Volume;
                if (volume != null)
                {
                    volume.SetBgmVolume(DefaultChannelVolume);
                    volume.SetSfxVolume(DefaultChannelVolume);
                    volume.SetVoiceVolume(DefaultChannelVolume);
                }
            }

            ApplySettings();
            SettingsValues.Store();
            ShowTabPage();
        }

        // 지금 탭의 Page를 열고 그린다. 이미 열린 Page면 다시 그리기만 한다(UIPF는 같은 Page를 닫지 않고, 묶기는 한 벌만 남는다).
        // 탭마다 나뉘는 곳은 여기 하나다(되돌릴 값의 탭 나눔은 SettingsState.ResetTab).
        private void ShowTabPage()
        {
            SettingsPanel owner = _ui.GetUI<SettingsPanel>();

            switch (_settings.Tab)
            {
                case SettingsPanel.Tab.Audio:
                    _ui.SwitchPage<AudioSettingsPage>(owner, Presentation<AudioSettingsPage>(), page =>
                    {
                        BindView(page, ApplyAudioBindings);
                        PresentAudioPage(page);
                    }, Unbind);
                    break;
                case SettingsPanel.Tab.Text:
                    _ui.SwitchPage<TextSettingsPage>(owner, Presentation<TextSettingsPage>(), page =>
                    {
                        BindView(page, ApplyTextBindings);
                        page.Present(_settings.TextModel());
                    }, Unbind);
                    break;
                case SettingsPanel.Tab.Playback:
                    _ui.SwitchPage<PlaybackSettingsPage>(owner, Presentation<PlaybackSettingsPage>(), page =>
                    {
                        BindView(page, ApplyPlaybackBindings);
                        page.Present(_settings.PlaybackModel());
                    }, Unbind);
                    break;
                case SettingsPanel.Tab.Display:
                    _ui.SwitchPage<DisplaySettingsPage>(owner, Presentation<DisplaySettingsPage>(), page =>
                    {
                        BindView(page, ApplyDisplayBindings);
                        page.Present(new DisplaySettingsPage.Model { Fullscreen = Screen.fullScreen });
                    }, Unbind);
                    break;
            }
        }

        private void PresentAudioPage(AudioSettingsPage page)
        {
            AudioVolumeSettings volume = Volume;

            page.Present(_settings.AudioModel(
                volume != null ? volume.BgmVolume : DefaultChannelVolume,
                volume != null ? volume.SfxVolume : DefaultChannelVolume,
                volume != null ? volume.VoiceVolume : DefaultChannelVolume));
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

        #region 바뀜 → 적용 · 저장

        private void HandleVolumeChanged(AudioSettingsPage.Channel channel, float value)
        {
            AudioVolumeSettings volume = Volume;

            switch (channel)
            {
                case AudioSettingsPage.Channel.Master:
                    _settings.SetMasterVolume(value);
                    ApplyMasterVolume();
                    SettingsValues.Store();
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
            _settings.SetMuted(muted);
            ApplyMasterVolume();
            SettingsValues.Store();
        }

        private void HandleTextScaleChanged(float scale)
        {
            _settings.SetTextScale(scale);
            ApplyTextScale();
            SettingsValues.Store();
        }

        private void HandleTypeSpeedChanged(float speed)
        {
            _settings.SetTypeSpeed(speed);
            ApplyTypeSpeed();
            SettingsValues.Store();
        }

        private void HandleBoxOpacityChanged(float opacity)
        {
            _settings.SetBoxOpacity(opacity);
            ApplyBoxOpacity();
            SettingsValues.Store();
        }

        private void HandleAutoDelayChanged(float seconds)
        {
            _settings.SetAutoDelay(seconds);
            ApplyPlaybackSettings();
            SettingsValues.Store();
        }

        private void HandleSpeedChoiceClicked(float multiplier)
        {
            if (!_settings.SetSpeedUp(multiplier))
                return;

            ApplyPlaybackSettings();
            SettingsValues.Store();
            _ui.GetUI<PlaybackSettingsPage>().Present(_settings.PlaybackModel());
        }

        private void HandleWindowModeClicked(bool fullscreen)
        {
            Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            _ui.GetUI<DisplaySettingsPage>().Present(new DisplaySettingsPage.Model { Fullscreen = fullscreen });
        }

        #endregion

        #region 적용 — 엔진 · 본체

        private AudioVolumeSettings Volume => _playback?.Audio != null ? _playback.Audio.Volume : null;

        // 지금 값을 엔진 · 본체에 모두 적용한다. 같은 값을 다시 적용해도 그대로다.
        private void ApplySettings()
        {
            ApplyMasterVolume();
            ApplyTextScale();
            ApplyTypeSpeed();
            ApplyBoxOpacity();
            ApplyPlaybackSettings();
        }

        private void ApplyMasterVolume()
        {
            AudioListener.volume = SettingsValues.Muted ? 0f : SettingsValues.MasterVolume;
        }

        // 글자 크기는 UIPF의 사용자 문맥(UIContext.TextScale)이다. 바꾸면 보이는 화면에 바로 다시 적용된다.
        private void ApplyTextScale()
        {
            UIContext context = _ui.Context;
            _ui.SetContext(context.With(context.ThemeId, context.LocaleId, SettingsValues.TextScale));
        }

        private void ApplyTypeSpeed()
        {
            if (_playback.Typewriter != null)
                _playback.Typewriter.unitsPerSecond = SettingsValues.TypeSpeed;
        }

        // 대사창 불투명도는 재생 화면에 그린다.
        private void ApplyBoxOpacity()
        {
            _playScreen.SetDialogueOpacity(SettingsValues.BoxOpacity);
        }

        // 배속은 다음에 빠르게 재생을 켤 때부터 적용된다(본체가 켤 때 배속을 읽는다).
        // 배속은 상태가 본체 범위 안의 보기로만 둔다.
        private void ApplyPlaybackSettings()
        {
            PlaybackSettings playback = _playback.PlaybackState?.PlaybackSettings;
            if (playback == null)
                return;

            playback.autoModeDelaySeconds = SettingsValues.AutoDelay;
            playback.speedupModeMultiplier = SettingsValues.SpeedUpMultiplier;
        }

        #endregion
    }
}
