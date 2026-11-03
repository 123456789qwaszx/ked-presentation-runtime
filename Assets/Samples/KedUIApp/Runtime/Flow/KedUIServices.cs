using System;
using UnityEngine;

namespace Ked.Samples.UI
{
    // 화면 바인딩들이 같이 쓰는 재생·호스트와 UI 글자 표. 호스트는 KedUIFlow.Configure에서 이어진다 — 그 전엔 비어 있다.
    // 바인딩은 호스트를 직접 안다(회차 입구 ProgressionLauncher · 수동 저장 ManualSaveFlow · 무대 PresentationUIRoot).
    internal sealed class KedUIServices
    {
        public KedUIServices(Localizer texts)
        {
            Texts = texts;
        }

        public Localizer Texts { get; }

        public KedPlaybackBindings Playback { get; private set; }
        public ProgressionLauncher Launcher { get; private set; }
        public ManualSaveFlow Saves { get; private set; }
        public KedExampleCard Example { get; private set; }

        private PresentationUIRoot _stage;
        private Func<bool> _hasResumePoint;
        private Action _switchExample;

        public bool IsRunning => Launcher != null && Launcher.IsRunning;

        public bool CanSwitchExample => _switchExample != null;

        public void Configure(
            KedPlaybackBindings playback,
            ProgressionLauncher launcher,
            ManualSaveFlow saves,
            PresentationUIRoot stage,
            KedExampleCard example,
            Func<bool> hasResumePoint,
            Action switchExample)
        {
            Playback = playback;
            Launcher = launcher;
            Saves = saves;
            _stage = stage;
            Example = example;
            _hasResumePoint = hasResumePoint;
            _switchExample = switchExample;
        }

        // 이어할 기록이 있는가 — 가벼운 확인이다. 읽다 실패하면 이어하기를 막는다.
        public bool HasResumePoint()
        {
            try
            {
                return _hasResumePoint != null && _hasResumePoint();
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[Ked UI] 이어하기 기록을 확인하지 못했다 — 이어하기를 막는다. {error.Message}");
                return false;
            }
        }

        public void SwitchExample() => _switchExample?.Invoke();

        // 무대(PresentationUIRoot)는 씬의 무대 캔버스에 늘 놓여 있고 UI는 그 위 캔버스에 얹힌다 — 꺼져 있으면 켠다.
        public void ShowStage()
        {
            if (_stage != null && !_stage.gameObject.activeSelf)
                _stage.gameObject.SetActive(true);
        }

        // 빨리 넘기기는 버튼으로 켜면(Ctrl과 달리) 스스로 꺼지지 않는다. 회차를 떠나거나 바꿀 때 끈다 —
        // 그러지 않으면 다음 회차의 첫 줄부터 넘어간다. 자동 진행은 옛 UI와 같이 둔다.
        public void StopFastForward()
        {
            if (Playback?.PlaybackState != null && Playback.PlaybackState.IsRapidSkipMode)
                Playback.Features?.EndRapidSkip();
        }

        public string Text(string key) => KedUIUtil.Text(Texts, key);
        public string Format(string key, params object[] args) => KedUIUtil.Format(Texts, key, args);
    }
}
