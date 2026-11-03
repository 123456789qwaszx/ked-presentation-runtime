using System;
using System.Collections.Generic;
using Ked.Progression;
using UnityEngine;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 새 예제 UI의 화면 흐름 — 옛 VNScreenBindings처럼 클래스 하나이고, 화면마다 부분 파일(KedUIFlow.Title.cs …)로 나눈다.
    /// 호스트(재생 · 회차 입구 · 수동 저장 · 무대)는 필드로 직접 들고, 화면끼리는 메서드를 바로 부른다.
    /// 이 파일은 화면들이 같이 쓰는 것 — 호스트 잇기, 스택 전환, Esc 한 층, 포커스 보정, 입력 소유권, View 이벤트 묶기/풀기.
    /// 화면별 필드·메서드는 화면 이름을 붙인다(_saveLoadPage · OpenSaveLoad …) — 한 클래스라 이름이 겹치지 않게.
    ///
    /// UIPF 매핑(시안 IMPLEMENTATION §6의 역할 → 실제 API):
    ///   Root    TitleScreen · PlayScreen            UIManager.SwitchRoot
    ///   Panel   메뉴 · 저장/불러오기 · 백로그 · 설정  UIManager.PushPanel / PopPanel / PopAllPanels
    ///   Page    설정의 탭 넷                          UIManager.SwitchPage(SettingsPanel, …)
    ///   모달    확인 · 안내                           Panel(맨 위만 입력을 받는 Panel 스택 규칙). Overlay는 아래를 막지 않아 쓰지 않는다.
    ///   끝 카드 회차 끝                               Panel. 챕터가 끝날 때만 연다(ShowChapterEnd — SceneRunner가 KedUIHost를 거쳐 부른다).
    ///
    /// View 이벤트는 화면을 열 때 묶고(BindView · AddBinding), 닫힐 때 UIPF 닫힘 콜백(Unbind)으로 푼다.
    /// </summary>
    public sealed partial class KedUIFlow : IPresentationView, IDisposable
    {
        private readonly UIManager _ui;
        private readonly Func<Type, UIPresentationSpec> _presentationOf;
        private readonly Localizer _texts;

        private readonly Dictionary<UIBase, List<Action>> _cleanupByView = new();

        // 호스트 — Configure에서 이어진다. 그 전엔 비어 있다.
        private KedPlaybackBindings _playback;
        private ProgressionLauncher _launcher;
        private ManualSaveFlow _saves;
        private PresentationUIRoot _stage;
        private KedExampleCard _example;
        private Func<bool> _hasResumePoint;
        private Action _switchExample;

        private KedInputOwnership _input;
        private bool _configured;

        // 타이틀로 나가는 중(ExitAsync를 기다리는 동안). 입력을 UI가 쥐고 Root를 막는다 — 정지와 되감기·메뉴가 겹치지 않게.
        private bool _exiting;

        public KedUIFlow(UIManager ui, Localizer texts, Func<Type, UIPresentationSpec> presentationOf)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _presentationOf = presentationOf ?? throw new ArgumentNullException(nameof(presentationOf));
            _texts = texts;

            // 화면이 다 등록됐는지 처음에 확인한다(빌더를 다시 돌려야 하면 여기서 알린다).
            // 필드로 드는 화면은 그리기·Esc·매 프레임에 다시 쓴다. 나머지는 열 때 UIPF가 내준다.
            Require<TitleScreen>();
            Require<SystemMenuPanel>();
            _playScreen = Require<PlayScreen>();
            _backlogPanel = Require<BacklogPanel>();
            _saveLoadPanel = Require<SaveLoadPanel>();
            _settingsPanel = Require<SettingsPanel>();
            _audioPage = Require<AudioSettingsPage>();
            _textPage = Require<TextSettingsPage>();
            _playbackPage = Require<PlaybackSettingsPage>();
            _displayPage = Require<DisplaySettingsPage>();
            _confirmPanel = Require<ConfirmPanel>();
            _noticePanel = Require<NoticePanel>();
            _endPanel = Require<EpisodeEndPanel>();
        }

        /// <summary>재생 본체 쪽 대사창 계약의 구현. 조립(PresentationAssembler)에 넘긴다.</summary>
        public IPresentationDialogueBoxView DialogueBox => _dialogueBoxView ??= new KedDialogueBoxView(_playScreen.Dialogue);

        /// <summary>재생 본체 쪽 선택지 상자 계약의 구현. 조립과 진행 예제의 선택지 화면에 넘긴다.</summary>
        public IPresentationOptionsBoxView OptionsBox => _optionsBoxView ??= new KedOptionsBoxView(_playScreen.Options);

        /// <summary>재생과 호스트를 잇는다. 조립이 끝난 뒤(재생 호스트가 정해진 뒤) 한 번.</summary>
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

            _playback = playback ?? throw new ArgumentNullException(nameof(playback));
            _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
            _saves = saves ?? throw new ArgumentNullException(nameof(saves));
            _stage = stage;
            _example = example;
            _hasResumePoint = hasResumePoint;
            _switchExample = switchExample;
            _configured = true;

            _input = new KedInputOwnership(playback.InputPoller);

            ConfigurePlay();
            ConfigureBacklog();
            ConfigureSettings();
        }

        public void Dispose()
        {
            DisposePlay();

            foreach (List<Action> cleanups in _cleanupByView.Values)
                RunCleanups(cleanups);

            _cleanupByView.Clear();
        }

        #region 매 프레임 — KedUIHost가 부른다

        public void Tick()
        {
            if (!_configured)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                HandleCancel();
            else if (IsHudHidden && IsPlayRoot && !_ui.HasPanel && AnyRestoreKey())
                RestoreHud();

            TickPlay();
            TickBacklog();

            SyncInput();
        }

        public void LateTick()
        {
            _input?.LateApply();
        }

        #endregion

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

        // 나가는 동안(ExitAsync)은 입력을 UI가 쥐고 Root를 막는다 — 정지와 되감기·메뉴가 겹치지 않게.
        private async void ReturnToTitle()
        {
            _exiting = true;

            try
            {
                StopFastForward();
                CloseAllPanels();

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

        // 빨리 넘기기는 버튼으로 켜면(Ctrl과 달리) 스스로 꺼지지 않는다. 회차를 떠나거나 바꿀 때 끈다 —
        // 그러지 않으면 다음 회차의 첫 줄부터 넘어간다. 자동 진행은 옛 UI와 같이 둔다.
        private void StopFastForward()
        {
            if (_playback?.PlaybackState != null && _playback.PlaybackState.IsRapidSkipMode)
                Features?.EndRapidSkip();
        }

        private string Text(string key) => KedUIUtil.Text(_texts, key);
        private string Format(string key, params object[] args) => KedUIUtil.Format(_texts, key, args);

        #endregion

        #region 스택 전환

        private bool IsPlayRoot => _ui.CurrentRoot == _playScreen;
        private bool IsHudHidden => _playScreen.IsHudHidden;
        private bool IsTop(UIBase view) => _ui.TopPanel == view;

        private T Require<T>() where T : UIBase
        {
            T view = _ui.GetUI<T>();
            if (view == null)
                throw new InvalidOperationException($"[Ked UI] 화면 '{typeof(T).Name}'이 등록되지 않았다. 빌더(Tools/Ked UI/Build Sample UI)를 다시 실행한다.");

            return view;
        }

        private T SwitchRoot<T>(Action<T> afterPresented) where T : UIBase, IUIRoot
        {
            T root = _ui.SwitchRoot<T>(Presentation<T>(), afterPresented, Unbind);
            SyncInput();
            return root;
        }

        private T PushPanel<T>(Action<T> afterPresented) where T : UIBase, IUIPanel
        {
            T panel = _ui.PushPanel<T>(Presentation<T>(), afterPresented, Unbind);
            SyncInput();
            return panel;
        }

        private T SwitchPage<T>(UIBase owner, Action<T> afterPresented) where T : UIBase, IUIPage
        {
            return _ui.SwitchPage<T>(owner, Presentation<T>(), afterPresented, Unbind);
        }

        // 포커스는 UIPF가 되돌린다 — 닫으면 그 Panel을 연 순간의 포커스로, 전부 닫으면 맨 처음 Panel 전의 포커스로.
        private void ClosePanel()
        {
            _ui.PopPanel(Unbind);
            SyncInput();
            EnsureFocus();
        }

        private void CloseAllPanels()
        {
            _ui.PopAllPanels(Unbind);
            SyncInput();
            EnsureFocus();
        }

        private UIPresentationSpec Presentation<T>() => _presentationOf(typeof(T));

        // 입력 소유권: 재생 화면이 아니거나(타이틀), Panel이 하나라도 있거나, 재생 화면의 UI를 숨겼거나,
        // 재생을 끝내는 중이면 UI가 갖는다. 재생 입력은 재생 화면에서 Panel 없이 대사를 보고 있을 때만 돈다.
        // 타이틀에서 Space로 버튼을 눌러 시작해도 그 Space가 첫 대사를 넘기지 않는다(돌려주는 것은 프레임 끝).
        //
        // Root 막기: Panel이 열린 동안은 UIPF가 막는다(BlockRootUnderPanels). 여기서는 재생을 끝내는 중(_exiting)에도
        // 막는다 — Panel이 없어도 되감기·퀵메뉴가 정지와 겹치지 않게. UIPF가 쌓기·꺼내기 때마다 다시 맞추므로 그 뒤에 부른다.
        private void SyncInput()
        {
            UIBase root = _ui.CurrentRoot;
            if (root != null && root.TryGetComponent(out CanvasGroup rootGroup))
                rootGroup.interactable = !_ui.HasPanel && !_exiting;

            if (_input == null)
                return;

            bool uiOwns = !IsPlayRoot || _ui.HasPanel || IsHudHidden || _exiting;
            _input.Set(uiOwns);
        }

        // UIPF가 되돌리지 못했을 때(연 버튼이 사라졌거나 막혔거나, 포인터로 열어 포커스가 없었을 때)만 대신 고른다.
        // 포커스가 빈 채로 남으면 마우스를 쓰기 전까지 Enter·방향키가 먹지 않는다.
        private void EnsureFocus()
        {
            if (KedUIUtil.TryFocus(KedUIUtil.CurrentFocus()))
                return;

            UIBase top = _ui.TopPanel;

            if (top != null)
                KedUIUtil.FocusFirstIn(top);
            else if (IsPlayRoot)
                _playScreen.RestoreFocus();
            else
                KedUIUtil.ClearFocus();
        }

        #endregion

        #region Esc — 맨 위 한 층만

        // Esc 한 번에 한 층만 닫는다 — 확인 → 취소, 안내 → 확인, 끝 카드 → 닫지 않음(닫으면 끝난 재생 화면에 남는다),
        // 설정 → 저장하고 닫기, 그 밖 → 한 층 닫기. Panel이 없고 UI를 숨겼으면 다시 보인다.
        private void HandleCancel()
        {
            UIBase top = _ui.TopPanel;

            if (top == null)
            {
                if (IsHudHidden && IsPlayRoot)
                    RestoreHud();
            }
            else if (top == _confirmPanel)
                _confirmPanel.Cancel();
            else if (top == _noticePanel)
                _noticePanel.Accept();
            else if (top == _endPanel)
                return;
            else if (top == _settingsPanel)
                CloseSettings();
            else
                ClosePanel();
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
