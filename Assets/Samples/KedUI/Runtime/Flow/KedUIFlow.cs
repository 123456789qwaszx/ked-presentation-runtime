using System;
using UnityEngine;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 새 예제 UI의 화면 흐름 — 옛 VNScreenBindings의 몫. 어떤 화면을 언제 열고 닫는지, 화면의 사건을 재생·저장 쪽의
    /// 어느 기능에 잇는지를 여기서 정한다. 화면(View)은 그리기와 사건 전달만, 기능은 재생(본체)과 호스트 계약이 갖는다.
    ///
    /// UIPF 매핑(시안 IMPLEMENTATION §6의 역할 → 실제 API):
    ///   Root    TitleScreen · PlayScreen            UIManager.SwitchRoot
    ///   Panel   메뉴 · 저장/불러오기 · 백로그 · 설정  UIManager.PushPanel / PopPanel / PopAllPanels
    ///   Page    설정의 탭 넷                          UIManager.SwitchPage(SettingsPanel, …)
    ///   모달    확인 · 안내                           Panel(맨 위만 입력을 받는 Panel 스택 규칙). Overlay는 아래를 막지 않아 쓰지 않는다.
    ///
    /// 화면 사건은 만들 때 한 번만 구독한다(화면과 흐름의 수명이 같다). 열 때마다 묶지 않으니 다시 열어도 리스너가 늘지 않는다.
    /// </summary>
    public sealed partial class KedUIFlow : IPresentationView, IDisposable
    {
        private readonly UIManager _ui;
        private readonly Localizer _texts;
        private readonly Func<Type, UIPresentationSpec> _presentationOf;

        private readonly TitleScreen _title;
        private readonly PlayScreen _play;
        private readonly SystemMenuPanel _menu;
        private readonly SaveLoadPanel _slots;
        private readonly BacklogPanel _backlog;
        private readonly SettingsPanel _settings;
        private readonly AudioSettingsPage _audioPage;
        private readonly TextSettingsPage _textPage;
        private readonly PlaybackSettingsPage _playbackPage;
        private readonly DisplaySettingsPage _displayPage;
        private readonly ConfirmPanel _confirm;
        private readonly NoticePanel _notice;

        private KedPlaybackBindings _playback;
        private IKedTitlePort _titlePort;
        private IKedSaveSlotPort _slotPort;
        private IKedStagePort _stage;
        private KedInputOwnership _input;
        private YarnSpeakerNames _speakers;
        private bool _configured;

        public KedUIFlow(UIManager ui, Localizer texts, Func<Type, UIPresentationSpec> presentationOf)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _texts = texts;
            _presentationOf = presentationOf ?? throw new ArgumentNullException(nameof(presentationOf));

            _title = Require<TitleScreen>();
            _play = Require<PlayScreen>();
            _menu = Require<SystemMenuPanel>();
            _slots = Require<SaveLoadPanel>();
            _backlog = Require<BacklogPanel>();
            _settings = Require<SettingsPanel>();
            _audioPage = Require<AudioSettingsPage>();
            _textPage = Require<TextSettingsPage>();
            _playbackPage = Require<PlaybackSettingsPage>();
            _displayPage = Require<DisplaySettingsPage>();
            _confirm = Require<ConfirmPanel>();
            _notice = Require<NoticePanel>();

            SubscribeTitle();
            SubscribePlay();
            SubscribeMenu();
            SubscribeSlots();
            SubscribeBacklog();
            SubscribeSettings();
            SubscribeModals();
        }

        /// <summary>재생 본체 쪽 대사창 계약의 구현. 조립(PresentationAssembler)에 넘긴다.</summary>
        public IPresentationDialogueBoxView DialogueBox => _play.DialogueBox;

        /// <summary>재생 본체 쪽 선택지 상자 계약의 구현. 조립과 진행 예제의 선택지 화면에 넘긴다.</summary>
        public IPresentationOptionsBoxView OptionsBox => _play.OptionsBox;

        /// <summary>
        /// 재생과 호스트를 잇는다. 조립이 끝난 뒤(재생 호스트가 정해진 뒤) 한 번.
        /// slots는 저장 슬롯이 없는 예제(연출 단독)에서 null.
        /// </summary>
        public void Configure(
            KedPlaybackBindings playback,
            IKedTitlePort title,
            IKedSaveSlotPort slots,
            IKedStagePort stage)
        {
            if (_configured)
                throw new InvalidOperationException("[Ked UI] 흐름은 한 번만 잇는다.");

            _playback = playback ?? throw new ArgumentNullException(nameof(playback));
            _titlePort = title ?? throw new ArgumentNullException(nameof(title));
            _slotPort = slots;
            _stage = stage;
            _configured = true;

            _input = new KedInputOwnership(playback.InputPoller);
            _speakers = new YarnSpeakerNames(playback.DialogueRunner);

            if (playback.LineFlow != null)
                playback.LineFlow.LineDisplayed += HandleLineDisplayed;

            InitializeSettings();
        }

        public void Dispose()
        {
            if (_playback?.LineFlow != null)
                _playback.LineFlow.LineDisplayed -= HandleLineDisplayed;
        }

        #region 매 프레임 — KedUIHost가 부른다

        public void Tick()
        {
            if (!_configured)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                HandleCancel();
            else if (_play.IsHudHidden && IsPlayRoot && !_ui.HasPanel && AnyRestoreKey())
                RestoreHud();

            if (IsPlayRoot)
            {
                VNPlaybackRuntimeState state = _playback.PlaybackState;
                if (state != null)
                    _play.SetModes(state.IsAutoMode, state.IsRapidSkipMode);
            }

            SyncInput();
        }

        public void LateTick()
        {
            _input?.LateApply();
        }

        #endregion

        #region Esc — 맨 위 한 층만

        // Esc 한 번에 한 층만 닫는다: 확인 → 취소, 안내 → 확인, 그 밖의 Panel → 뒤로. Panel이 없고 UI를 숨겼으면 다시 보인다.
        private void HandleCancel()
        {
            UIBase top = _ui.TopPanel;

            if (top == null)
            {
                if (_play.IsHudHidden && IsPlayRoot)
                    RestoreHud();

                return;
            }

            if (top == _confirm)
                _confirm.Cancel();
            else if (top == _notice)
                _notice.Accept();
            else if (top == _settings)
                CloseSettings();
            else if (top == _slots)
                SlotsBack();
            else
                ClosePanel();
        }

        private static bool AnyRestoreKey() =>
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter);

        #endregion

        #region 공통

        private bool IsPlayRoot => _ui.CurrentRoot == _play;
        private bool IsRunning => _playback?.Host != null && _playback.Host.IsRunning;

        private T Require<T>() where T : UIBase
        {
            T view = _ui.GetUI<T>();
            if (view == null)
                throw new InvalidOperationException($"[Ked UI] 화면 '{typeof(T).Name}'이 등록되지 않았다. 빌더(Tools/Ked UI/Build Sample UI)를 다시 실행한다.");

            return view;
        }

        private UIPresentationSpec Presentation<T>() => _presentationOf(typeof(T));

        private T PushPanel<T>(Action<T> afterPresented) where T : UIBase, IUIPanel
        {
            T panel = _ui.PushPanel(Presentation<T>(), afterPresented);
            SyncInput();
            return panel;
        }

        private void ClosePanel()
        {
            _ui.PopPanel();
            SyncInput();
        }

        private void CloseAllPanels()
        {
            _ui.PopAllPanels();
            SyncInput();
        }

        // 입력 소유권: 재생 화면이 아니거나(타이틀), Panel이 하나라도 있거나, 재생 화면의 UI를 숨겼으면 UI가 갖는다.
        // 재생 입력은 재생 화면에서 Panel 없이 대사를 보고 있을 때만 돈다. 타이틀에서 Space로 버튼을 눌러 시작해도
        // 그 Space가 첫 대사를 넘기지 않는다(돌려주는 것은 프레임 끝).
        private void SyncInput()
        {
            if (_input == null)
                return;

            bool uiOwns = !IsPlayRoot || _ui.HasPanel || _play.IsHudHidden;
            _input.Set(uiOwns);
        }

        private string Text(string key) => KedUIUtil.Text(_texts, key);
        private string Format(string key, params object[] args) => KedUIUtil.Format(_texts, key, args);

        #endregion
    }
}
