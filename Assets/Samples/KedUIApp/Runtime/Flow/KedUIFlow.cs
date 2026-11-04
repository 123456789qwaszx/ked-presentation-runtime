using System;
using System.Collections.Generic;
using Ked.Progression;
using UnityEngine;

namespace Ked.Samples.UI
{
    // 화면 이벤트를 재생 · 호스트에 잇는다. 화면 전환 · 스택 · 포커스 되돌리기 · Root 막기는 UIManager(UIPF)가 한다.
    public sealed partial class KedUIFlow : IPresentationView, IDisposable
    {
        private readonly UIManager _ui;
        private readonly Func<Type, UIPresentationSpec> _presentationOf;
        private readonly Localizer _texts;

        private readonly Dictionary<UIBase, List<Action>> _cleanupByView = new();

        private KedPlaybackBindings _playback;
        private ProgressionLauncher _launcher;
        private ManualSaveFlow _saves;
        private PresentationUIRoot _stage;
        private KedExampleCard _example;
        private Func<bool> _hasResumePoint;
        private Action _switchExample;

        private bool _configured;

        // 타이틀로 나가는 중(ExitAsync를 기다리는 동안). 정지와 되감기·메뉴가 겹치지 않게.
        private bool _exiting;

        // UI가 재생 입력을 마지막으로 가진 프레임(IsPlaybackInputBlocked).
        private int _uiInputFrame = -1;

        public KedUIFlow(
            UIManager ui,
            Localizer texts,
            Func<Type, UIPresentationSpec> presentationOf)
        {
            _ui = ui;
            _texts = texts;
            _presentationOf = presentationOf;

            // 대사창 · 선택지 상자는 조립이 화면 전환보다 먼저 꺼낸다.
            _playScreen = ui.GetUI<PlayScreen>();
            if (_playScreen == null)
                throw new InvalidOperationException("[Ked UI] 재생 화면(PlayScreen)이 등록되지 않았다. 빌더(Tools/Ked UI/Build Sample UI)를 다시 실행한다.");
        }

        /// <summary>재생 본체 쪽 대사창 계약의 구현. 조립(PresentationAssembler)에 넘긴다.</summary>
        public IPresentationDialogueBoxView DialogueBox =>
            _dialogueBoxView ??= new KedDialogueBoxView(_playScreen.Dialogue);

        /// <summary>재생 본체 쪽 선택지 상자 계약의 구현. 조립과 진행 예제의 선택지 화면에 넘긴다.</summary>
        public IPresentationOptionsBoxView OptionsBox =>
            _optionsBoxView ??= new KedOptionsBoxView(_playScreen.Options);

        public void Configure(
            KedPlaybackBindings playback,
            ProgressionLauncher launcher,
            ManualSaveFlow saves,
            PresentationUIRoot stage,
            KedExampleCard example,
            Func<bool> hasResumePoint,
            Action switchExample)
        {
            if (_configured)
                throw new InvalidOperationException("[Ked UI] 흐름은 한 번만 잇는다.");

            _playback = playback;
            _launcher = launcher;
            _saves = saves;
            _stage = stage;
            _example = example;
            _hasResumePoint = hasResumePoint;
            _switchExample = switchExample;
            _configured = true;

            if (playback.InputPoller != null)
                playback.InputPoller.InputBlocked = IsPlaybackInputBlocked;

            ConfigurePlay();
            ConfigureBacklog();
            ConfigureSettings();
        }

        public void Dispose()
        {
            if (_playback?.InputPoller != null)
                _playback.InputPoller.InputBlocked = null;

            DisposePlay();

            foreach (List<Action> cleanups in _cleanupByView.Values)
                RunCleanups(cleanups);

            _cleanupByView.Clear();
        }

        // 매 프레임 — KedUIHost가 부른다. Esc와 HUD 되살리기, 이벤트가 없는 재생 상태를 화면에 옮기기.
        public void Tick()
        {
            if (!_configured)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                HandleCancel();
            else if (IsPlayRoot && _playScreen.IsHudHidden && !_ui.HasPanel && AnyRestoreKey())
                RestoreHud();

            TickPlay();
            TickBacklog();
        }

        #region 목적지 — 호스트가 부르는 것

        /// <summary>타이틀로. 열린 Panel을 모두 닫고 Root를 타이틀로 바꾼다. 재생은 호스트가 이미 끝냈다(ExitAsync).</summary>
        public void GoToTitle()
        {
            CloseAllPanels();
            OpenTitle();
        }

        /// <summary>IPresentationView — 재생이 시작될 때 본체가 부른다. Root를 재생 화면으로 바꾼다.</summary>
        public void GoToPresentationView() => OpenPlay();

        public void ShowChapterEnd(ChapterDefinition chapter, EpisodeNode lastEpisode, ChapterState state)
        {
            if (!_configured || chapter == null)
                return;

            // 끝은 재생 화면에서만 온다. 타이틀로 나가는 중이거나 이미 타이틀이면 띄울 자리가 없다.
            if (_exiting || !IsPlayRoot)
            {
                Debug.Log("[Ked UI] 재생 화면이 아니라 끝 카드를 띄우지 않는다.");
                return;
            }

            OpenEnd(chapter, lastEpisode, state);
        }

        // 나가는 동안(ExitAsync)은 Root를 막는다 — 되감기·퀵메뉴가 정지와 겹치지 않게. 다음 SwitchRoot에서 UIPF가 다시 푼다.
        private async void ReturnToTitle()
        {
            _exiting = true;

            try
            {
                StopFastForward();
                CloseAllPanels();

                if (_ui.CurrentRoot != null && _ui.CurrentRoot.TryGetComponent(out CanvasGroup root))
                    root.interactable = false;

                if (_launcher != null)
                    await _launcher.ExitAsync();
            }
            catch (Exception error)
            {
                Debug.LogException(error);
            }
            finally
            {
                _exiting = false;
                GoToTitle();
            }
        }

        #endregion

        #region 호스트

        private PlaybackFeatureController Features => _playback?.Features;

        private bool IsRunning => _launcher != null && _launcher.IsRunning;

        private bool IsPlayRoot => _ui.CurrentRoot == _playScreen;

        // 빨리 넘기기는 버튼으로 켜면(Ctrl과 달리) 스스로 꺼지지 않는다. 회차를 떠나거나 바꿀 때 끈다 —
        // 그러지 않으면 다음 회차의 첫 줄부터 넘어간다. 자동 진행은 옛 UI와 같이 둔다.
        private void StopFastForward()
        {
            if (_playback?.PlaybackState != null && _playback.PlaybackState.IsRapidSkipMode)
                Features?.EndRapidSkip();
        }

        private string Text(string key) => KedUIUtil.Text(_texts, key);

        #endregion

        #region 입력 — 재생 폴러가 묻는다

        // UI가 재생 입력을 가질 때: 재생 화면이 아니거나(타이틀), Panel이 있거나, UI를 숨겼거나, 나가는 중.
        private bool UiOwnsInput => !IsPlayRoot || _ui.HasPanel || _playScreen.IsHudHidden || _exiting;

        // 지금 갖거나 바로 앞 프레임까지 가졌으면 막는다 — UI에서 재생으로 넘긴 그 프레임의 키(타이틀에서 시작을 누른
        // Space, 메뉴를 닫은 Enter)를 재생이 다시 읽지 않게. 폴러가 매 프레임 물으니 따로 맞출 곳이 없다.
        private bool IsPlaybackInputBlocked()
        {
            if (UiOwnsInput)
                _uiInputFrame = Time.frameCount;

            return _uiInputFrame >= Time.frameCount - 1;
        }

        #endregion

        #region 화면 — UIManager를 부른다

        private UIPresentationSpec Presentation<T>() => _presentationOf(typeof(T));

        private void ClosePanel() => _ui.PopPanel(Unbind);

        private void CloseAllPanels() => _ui.PopAllPanels(Unbind);

        // Esc 한 번에 맨 위 한 층만 — 확인 → 취소, 안내 → 확인, 끝 카드 → 닫지 않음(닫으면 끝난 재생 화면에 남는다),
        // 설정 → 저장하고 닫기, 그 밖 → 한 층 닫기. Panel이 없고 UI를 숨겼으면 다시 보인다.
        private void HandleCancel()
        {
            switch (_ui.TopPanel)
            {
                case null:
                    if (IsPlayRoot && _playScreen.IsHudHidden)
                        RestoreHud();
                    break;
                case ConfirmPanel confirm:
                    confirm.Cancel();
                    break;
                case NoticePanel notice:
                    notice.Accept();
                    break;
                case EpisodeEndPanel:
                    break;
                case SettingsPanel:
                    CloseSettings();
                    break;
                default:
                    ClosePanel();
                    break;
            }
        }

        private static bool AnyRestoreKey() =>
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter);

        #endregion

        #region View 이벤트 묶기 · 풀기

        // 다시 열려도(같은 Root로 전환 · 쌓인 Panel로 돌아가기) 한 벌만 묶이게 먼저 푼다.
        private void BindView<T>(T view, Action<T> apply) where T : UIBase
        {
            Unbind(view);
            apply(view);
        }

        private void AddBinding<T>(T view, Action<T> attach, Action<T> detach) where T : UIBase
        {
            attach(view);

            if (!_cleanupByView.TryGetValue(view, out List<Action> cleanups))
            {
                cleanups = new List<Action>();
                _cleanupByView[view] = cleanups;
            }

            cleanups.Add(() => detach(view));
        }

        // UIPF 닫힘 콜백 — 닫힌 화면의 View 이벤트를 푼다.
        private void Unbind(UIBase view)
        {
            if (view == null || !_cleanupByView.TryGetValue(view, out List<Action> cleanups))
                return;

            _cleanupByView.Remove(view);
            RunCleanups(cleanups);
        }

        private static void RunCleanups(List<Action> cleanups)
        {
            for (int i = cleanups.Count - 1; i >= 0; i--)
                cleanups[i]?.Invoke();
        }

        #endregion
    }
}
