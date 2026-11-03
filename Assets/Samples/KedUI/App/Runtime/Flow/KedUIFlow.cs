using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 새 예제 UI의 화면 흐름 — 전환과 스택만. 어떤 화면을 언제 열고 닫는지, Esc 한 층, 포커스 보정, 입력 소유권을 여기서 정한다.
    /// 화면마다의 일(상태를 읽어 View 모델로, View 이벤트를 동작으로)은 화면 바인딩(Bindings/)이, 그리기는 화면(View, Screens/)이 한다.
    ///
    /// UIPF 매핑(시안 IMPLEMENTATION §6의 역할 → 실제 API):
    ///   Root    TitleScreen · PlayScreen            UIManager.SwitchRoot
    ///   Panel   메뉴 · 저장/불러오기 · 백로그 · 설정  UIManager.PushPanel / PopPanel / PopAllPanels
    ///   Page    설정의 탭 넷                          UIManager.SwitchPage(SettingsPanel, …)
    ///   모달    확인 · 안내                           Panel(맨 위만 입력을 받는 Panel 스택 규칙). Overlay는 아래를 막지 않아 쓰지 않는다.
    ///   끝 카드 회차 끝                               Panel. 호스트가 끝을 알릴 때만 연다(ShowPlaythroughEnd).
    ///
    /// 바인딩은 화면을 열 때 View 이벤트를 묶는다. 닫힘(Pop · Root 교체 · Page 교체)은 여기서 UIPF 닫힘 콜백으로 받아 그 화면의 바인딩에 알린다.
    /// </summary>
    public sealed class KedUIFlow : IPresentationView, IDisposable
    {
        private readonly UIManager _ui;
        private readonly Func<Type, UIPresentationSpec> _presentationOf;
        private readonly KedUIServices _services;

        private readonly TitleBinding _title;
        private readonly PlayBinding _play;
        private readonly MenuBinding _menu;
        private readonly SaveLoadBinding _slots;
        private readonly BacklogBinding _backlog;
        private readonly SettingsBinding _settings;
        private readonly ModalBinding _modal;
        private readonly EpisodeEndBinding _end;

        private readonly List<ScreenBinding> _bindings = new();
        private readonly Dictionary<UIBase, ScreenBinding> _bindingByView = new();

        private KedInputOwnership _input;
        private bool _configured;

        // 타이틀로 나가는 중(ExitAsync를 기다리는 동안). 입력을 UI가 쥐고 Root를 막는다 — 정지와 되감기·메뉴가 겹치지 않게.
        private bool _exiting;

        public KedUIFlow(UIManager ui, Localizer texts, Func<Type, UIPresentationSpec> presentationOf)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _presentationOf = presentationOf ?? throw new ArgumentNullException(nameof(presentationOf));
            _services = new KedUIServices(texts);

            _title = Add(new TitleBinding(this, _services));
            _play = Add(new PlayBinding(this, _services));
            _menu = Add(new MenuBinding(this, _services));
            _slots = Add(new SaveLoadBinding(this, _services));
            _backlog = Add(new BacklogBinding(this, _services));
            _settings = Add(new SettingsBinding(this, _services, ui));
            _modal = Add(new ModalBinding(this, _services));
            _end = Add(new EpisodeEndBinding(this, _services));
        }

        /// <summary>재생 본체 쪽 대사창 계약의 구현. 조립(PresentationAssembler)에 넘긴다.</summary>
        public IPresentationDialogueBoxView DialogueBox => _play.DialogueBox;

        /// <summary>재생 본체 쪽 선택지 상자 계약의 구현. 조립과 진행 예제의 선택지 화면에 넘긴다.</summary>
        public IPresentationOptionsBoxView OptionsBox => _play.OptionsBox;

        /// <summary>
        /// 재생과 호스트를 잇는다. 조립이 끝난 뒤(재생 호스트가 정해진 뒤) 한 번.
        /// slots는 저장 슬롯이 없는 호스트에서 null — 저장·불러오기·갈라지기가 빠진다(예제는 늘 넘긴다).
        /// </summary>
        public void Configure(
            KedPlaybackBindings playback,
            IKedTitlePort title,
            IKedSaveSlotPort slots,
            IKedStagePort stage)
        {
            if (_configured)
                throw new InvalidOperationException("[Ked UI] 흐름은 한 번만 잇는다.");

            if (playback == null)
                throw new ArgumentNullException(nameof(playback));

            if (title == null)
                throw new ArgumentNullException(nameof(title));

            _services.Configure(playback, title, slots, stage);
            _configured = true;

            _input = new KedInputOwnership(playback.InputPoller);

            foreach (ScreenBinding binding in _bindings)
                binding.OnConfigured();
        }

        public void Dispose()
        {
            foreach (ScreenBinding binding in _bindings)
                binding.Dispose();
        }

        #region 매 프레임 — KedUIHost가 부른다

        public void Tick()
        {
            if (!_configured)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                HandleCancel();
            else if (IsHudHidden && IsPlayRoot && !_ui.HasPanel && AnyRestoreKey())
                _play.RestoreHud();

            foreach (ScreenBinding binding in _bindings)
                binding.Tick();

            SyncInput();
        }

        public void LateTick()
        {
            _input?.LateApply();
        }

        #endregion

        #region 목적지 — 화면을 이름으로 연다

        /// <summary>타이틀로. 열린 Panel을 모두 닫고 Root를 타이틀로 바꾼다. 재생은 호스트가 이미 끝냈다(ExitAsync).</summary>
        public void GoToTitle()
        {
            CloseAllPanels();
            _title.Open();
        }

        /// <summary>IPresentationView — 재생이 시작될 때 본체가 부른다. Root를 재생 화면으로 바꾼다.</summary>
        public void GoToPresentationView() => _play.Open();

        public void ShowPlaythroughEnd(KedPlaythroughEnd end)
        {
            if (!_configured || end == null)
                return;

            // 끝은 재생 화면에서만 온다. 타이틀로 나가는 중이거나 이미 타이틀이면 띄울 자리가 없다.
            if (_exiting || !IsPlayRoot)
            {
                Debug.Log("[Ked UI] 재생 화면이 아니라 끝 카드를 띄우지 않는다.");
                return;
            }

            _end.Open(end);
        }

        internal void OpenMenu() => _menu.Open();
        internal void OpenBacklog() => _backlog.Open();
        internal void OpenSave() => _slots.Open(SaveLoadBinding.Mode.Save);
        internal void OpenLoad() => _slots.Open(SaveLoadBinding.Mode.Load);
        internal void OpenSettings() => _settings.Open();
        internal void RestoreHud() => _play.RestoreHud();

        internal void Confirm(ConfirmPanel.Request request) => _modal.Confirm(request);

        internal void Notice(string title, string body, NoticePanel.Kind kind, Action onClosed = null) =>
            _modal.Notice(title, body, kind, onClosed);

        // 나가는 동안(ExitAsync)은 입력을 UI가 쥐고 Root를 막는다 — 정지와 되감기·메뉴가 겹치지 않게.
        internal async void ReturnToTitle()
        {
            _exiting = true;

            try
            {
                _services.StopFastForward();
                CloseAllPanels();

                if (_services.Playback?.Host != null)
                    await _services.Playback.Host.ExitAsync();
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

        #region 전환 · 스택 — 바인딩이 화면을 열고 닫는 길

        internal bool IsPlayRoot => _ui.CurrentRoot == _play.View;
        internal bool IsHudHidden => _play.View.IsHudHidden;
        internal bool HasPanel => _ui.HasPanel;
        internal bool IsTop(UIBase view) => _ui.TopPanel == view;

        internal T Require<T>() where T : UIBase
        {
            T view = _ui.GetUI<T>();
            if (view == null)
                throw new InvalidOperationException($"[Ked UI] 화면 '{typeof(T).Name}'이 등록되지 않았다. 빌더(Tools/Ked UI/Build Sample UI)를 다시 실행한다.");

            return view;
        }

        internal T SwitchRoot<T>(Action<T> afterPresented) where T : UIBase, IUIRoot
        {
            T root = _ui.SwitchRoot<T>(Presentation<T>(), afterPresented, Unbind);
            SyncInput();
            return root;
        }

        internal T PushPanel<T>(Action<T> afterPresented) where T : UIBase, IUIPanel
        {
            T panel = _ui.PushPanel<T>(Presentation<T>(), afterPresented, Unbind);
            SyncInput();
            return panel;
        }

        internal T SwitchPage<T>(UIBase owner, Action<T> afterPresented) where T : UIBase, IUIPage
        {
            return _ui.SwitchPage<T>(owner, Presentation<T>(), afterPresented, Unbind);
        }

        // 포커스는 UIPF가 되돌린다 — 닫으면 그 Panel을 연 순간의 포커스로, 전부 닫으면 맨 처음 Panel 전의 포커스로.
        internal void ClosePanel()
        {
            _ui.PopPanel(Unbind);
            SyncInput();
            EnsureFocus();
        }

        internal void CloseAllPanels()
        {
            _ui.PopAllPanels(Unbind);
            SyncInput();
            EnsureFocus();
        }

        // 입력 소유권: 재생 화면이 아니거나(타이틀), Panel이 하나라도 있거나, 재생 화면의 UI를 숨겼거나,
        // 재생을 끝내는 중이면 UI가 갖는다. 재생 입력은 재생 화면에서 Panel 없이 대사를 보고 있을 때만 돈다.
        // 타이틀에서 Space로 버튼을 눌러 시작해도 그 Space가 첫 대사를 넘기지 않는다(돌려주는 것은 프레임 끝).
        //
        // Root 막기: Panel이 열린 동안은 UIPF가 막는다(BlockRootUnderPanels). 여기서는 재생을 끝내는 중(_exiting)에도
        // 막는다 — Panel이 없어도 되감기·퀵메뉴가 정지와 겹치지 않게. UIPF가 쌓기·꺼내기 때마다 다시 맞추므로 그 뒤에 부른다.
        internal void SyncInput()
        {
            UIBase root = _ui.CurrentRoot;
            if (root != null && root.TryGetComponent(out CanvasGroup rootGroup))
                rootGroup.interactable = !_ui.HasPanel && !_exiting;

            if (_input == null)
                return;

            bool uiOwns = !IsPlayRoot || _ui.HasPanel || IsHudHidden || _exiting;
            _input.Set(uiOwns);
        }

        #endregion

        #region Esc — 맨 위 한 층만

        // Esc 한 번에 한 층만 닫는다 — 맨 위 Panel의 바인딩이 정한다(확인 → 취소, 안내 → 확인, 끝 카드 → 닫지 않음, 그 밖 → 뒤로).
        // Panel이 없고 UI를 숨겼으면 다시 보인다.
        private void HandleCancel()
        {
            UIBase top = _ui.TopPanel;

            if (top == null)
            {
                if (IsHudHidden && IsPlayRoot)
                    _play.RestoreHud();

                return;
            }

            if (_bindingByView.TryGetValue(top, out ScreenBinding binding))
                binding.Back(top);
            else
                ClosePanel();
        }

        private static bool AnyRestoreKey() =>
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter);

        #endregion

        #region 공통

        private T Add<T>(T binding) where T : ScreenBinding
        {
            _bindings.Add(binding);

            foreach (UIBase view in binding.Views)
                _bindingByView.Add(view, binding);

            return binding;
        }

        // UIPF 닫힘 콜백 — 닫힌 화면의 바인딩이 View 이벤트를 푼다.
        private void Unbind(UIBase view)
        {
            if (view != null && _bindingByView.TryGetValue(view, out ScreenBinding binding))
                binding.Unbind(view);
        }

        private UIPresentationSpec Presentation<T>() => _presentationOf(typeof(T));

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
                _play.View.RestoreFocus();
            else
                KedUIUtil.ClearFocus();
        }

        #endregion
    }
}
