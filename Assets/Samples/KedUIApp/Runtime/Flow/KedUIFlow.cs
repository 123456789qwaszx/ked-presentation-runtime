using System;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

namespace Ked.Samples.UI
{
    // 화면 이벤트를 재생 · 호스트에 잇는다. 화면 전환 · 스택 · 포커스 되돌리기 · Root 막기는 UIManager(UIPF)가 한다.
    public sealed partial class KedUIFlow : IPresentationView, IChapterEndView, IDisposable
    {
        private readonly UIManager _ui;
        private readonly Func<Type, UIPresentationSpec> _presentationOf;
        private readonly Localizer _texts;

        // 재생 쪽 — 조립 결과 · 재생 모드 · 씬 부품(타자기 · 오디오 · Yarn 러너 · 무대).
        private PresentationRuntime _runtime;
        private PlaybackRuntimeState _playbackState;
        private EllipsisBreathTypewriter _typewriter;
        private AudioSystem _audio;
        private DialogueRunner _dialogueRunner;
        private PresentationUIRoot _stage;

        private ProgressionLauncher _launcher;
        private ManualSaveFlow _saves;
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
            _playScreen = ui.GetUI<PlayScreen>();
        }

        public void Configure(
            PresentationRuntime runtime,
            PlaybackRuntimeState playbackState,
            AdvanceInputPoller inputPoller,
            EllipsisBreathTypewriter typewriter,
            AudioSystem audio,
            DialogueRunner dialogueRunner,
            PresentationUIRoot stage,
            ProgressionLauncher launcher,
            ManualSaveFlow saves,
            KedExampleCard example,
            Func<bool> hasResumePoint,
            Action switchExample)
        {
            if (_configured)
                throw new InvalidOperationException("[Ked UI] 흐름은 한 번만 잇는다.");

            _runtime = runtime;
            _playbackState = playbackState;
            _typewriter = typewriter;
            _audio = audio;
            _dialogueRunner = dialogueRunner;
            _stage = stage;
            _launcher = launcher;
            _saves = saves;
            _example = example;
            _hasResumePoint = hasResumePoint;
            _switchExample = switchExample;
            _configured = true;

            if (inputPoller != null)
                inputPoller.InputBlocked = IsPlaybackInputBlocked;

            ConfigureBacklog();
            ConfigureSettings();
        }

        // 이벤트가 없는 재생 상태(퀵메뉴 켜짐 · 다시 듣기 보이스)를 화면에 맞춘다.
        private void RefreshPlaybackState()
        {
            if (!_configured)
                return;

            RefreshQuickMenuModes();
            RefreshBacklogVoice();
        }

        #region 목적지

        // 타이틀로. 열린 Panel을 모두 닫고 Root를 타이틀로 바꾼다. 재생은 호스트가 이미 끝냈다(ExitAsync).
        private void HandleGoToTitle()
        {
            CloseAllPanels();

            // 의도를 비우고(Hold) 맞추면(Release) 흐르던 곡이 끊김.
            BgmPlayer bgm = _audio.Bgm;
            bgm?.Hold();
            bgm?.Release();

            OpenTitle();
        }

        // 나가는 동안(ExitAsync)은 Root를 막는다.
        // 되감기, 퀵메뉴가 정지와 겹치지 않게.
        // 다음 SwitchRoot에서 다시 품.
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
                HandleGoToTitle();
            }
        }

        #endregion

        #region 호스트

        private PlaybackFeatureController Features => _runtime?.Features;

        private bool IsRunning => _launcher.IsRunning;

        private bool IsPlayRoot => _ui.CurrentRoot == _playScreen;

        // 빨리 넘기기는 버튼으로 켜면(Ctrl과 달리) 스스로 꺼지지 않는다. 회차를 떠나거나 바꿀 때 끈다 —
        // 그러지 않으면 다음 회차의 첫 줄부터 넘어간다. 자동 진행은 옛 UI와 같이 둔다.
        private void StopFastForward()
        {
            if (_playbackState.IsRapidSkipMode)
                Features.EndRapidSkip();
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

        // Esc 한 번에 맨 위 한 층만 — 안내 → 확인, 끝 카드 → 닫지 않음(닫으면 끝난 재생 화면에 남는다),
        // 설정 → 저장하고 닫기, 그 밖(확인창의 취소 포함) → 한 층 닫기. Panel이 없고 UI를 숨겼으면 다시 보인다.
        private void HandleCancel()
        {
            switch (_ui.TopPanel)
            {
                case null:
                    if (IsPlayRoot && _playScreen.IsHudHidden)
                        RestoreHud();
                    break;
                case NoticePanel:
                    HandleNoticeResolved();
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

        #endregion

        #region UIContext
        
        private readonly Dictionary<UIBase, List<Action>> _cleanupByOwner = new();
        
        private void BindView<T>(T owner, Action<T> apply)
            where T : UIBase
        {
            Unbind(owner);
            apply(owner);
        }
    
        private void AddBinding<T>(T owner, Action<T> attach, Action<T> detach)
            where T : UIBase
        {
            attach(owner);
            AddCleanup(owner, () => detach(owner));
        }
    
        private void AddCleanup(UIBase owner, Action cleanup)
        {
            if (!_cleanupByOwner.TryGetValue(owner, out List<Action> cleanups))
            {
                cleanups = new List<Action>();
                _cleanupByOwner[owner] = cleanups;
            }
    
            cleanups.Add(cleanup);
        }
    
        private void Unbind(UIBase owner)
        {
            if (!_cleanupByOwner.TryGetValue(owner, out List<Action> cleanups))
                return;
    
            RunCleanups(cleanups);
            _cleanupByOwner.Remove(owner);
        }
    
        private void UnbindAll()
        {
            foreach (var kv in _cleanupByOwner)
                RunCleanups(kv.Value);
    
            _cleanupByOwner.Clear();
        }
    
        private static void RunCleanups(List<Action> cleanups)
        {
            for (int i = cleanups.Count - 1; i >= 0; i--)
                cleanups[i]?.Invoke();
        }
    
        public void Dispose()
        {
            UnbindAll();
        }
        
        #endregion
    }
}
