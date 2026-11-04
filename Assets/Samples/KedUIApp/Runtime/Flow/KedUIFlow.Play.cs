using System;
using UnityEngine;

namespace Ked.Samples.UI
{
    // 02 대화 · 03 선택지 — 퀵메뉴와 진행 클릭.
    // 재생 쪽 대사창·선택지 상자 계약의 어댑터(DialogueBox · OptionsBox)도 이 화면 것이다 — 화면 묶음은 재생 런타임을 모른다.
    public sealed partial class KedUIFlow
    {
        private readonly PlayScreen _playScreen;
        private KedDialogueBoxView _dialogueBoxView;
        private KedOptionsBoxView _optionsBoxView;

        private void ConfigurePlay()
        {
            if (_runtime.LineFlow != null)
                _runtime.LineFlow.LineDisplayed += HandleLineDisplayed;
        }

        private void DisposePlay()
        {
            if (_runtime?.LineFlow != null)
                _runtime.LineFlow.LineDisplayed -= HandleLineDisplayed;
        }

        // 퀵메뉴의 켜짐 표시(자동 진행 · 빨리 넘기기).
        private void TickPlay()
        {
            if (!IsPlayRoot)
                return;

            PlaybackRuntimeState state = _playbackState;
            if (state != null)
                _playScreen.SetModes(state.IsAutoMode, state.IsRapidSkipMode);
        }

        // 무대를 앞으로 가져오고 Root를 재생 화면으로 바꾼다. 이미 재생 화면이면 Presentation만 다시 적용된다(되감기·장면 전환).
        private void OpenPlay()
        {
            ShowStage();

            bool entering = !IsPlayRoot;

            _ui.SwitchRoot<PlayScreen>(Presentation<PlayScreen>(), root =>
            {
                BindView(root, ApplyPlayBindings);

                if (entering)
                    root.SetHudHidden(false);

                root.SetChapterLabel(ChapterLabel());
            }, Unbind);
        }

        // 무대(PresentationUIRoot)는 씬의 무대 캔버스에 늘 놓여 있고 UI는 그 위 캔버스에 얹힌다 — 꺼져 있으면 켠다.
        private void ShowStage()
        {
            PresentationUIRoot stage = _scene?.Stage;
            if (stage != null && !stage.gameObject.activeSelf)
                stage.gameObject.SetActive(true);
        }

        private void RestoreHud() => _playScreen.SetHudHidden(false);

        private void HideHud() => _playScreen.SetHudHidden(true);

        private void ApplyPlayBindings(PlayScreen screen)
        {
            AddBinding(screen,
                s => s.AdvanceClicked += HandleAdvance,
                s => s.AdvanceClicked -= HandleAdvance);

            AddBinding(screen,
                s => s.AutoClicked += HandleAuto,
                s => s.AutoClicked -= HandleAuto);

            AddBinding(screen,
                s => s.FastForwardClicked += HandleFastForward,
                s => s.FastForwardClicked -= HandleFastForward);

            AddBinding(screen,
                s => s.RollbackClicked += HandleRollback,
                s => s.RollbackClicked -= HandleRollback);

            AddBinding(screen,
                s => s.BacklogClicked += OpenBacklog,
                s => s.BacklogClicked -= OpenBacklog);

            AddBinding(screen,
                s => s.HideClicked += HideHud,
                s => s.HideClicked -= HideHud);

            AddBinding(screen,
                s => s.MenuClicked += OpenMenu,
                s => s.MenuClicked -= OpenMenu);

            AddBinding(screen,
                s => s.RestoreClicked += RestoreHud,
                s => s.RestoreClicked -= RestoreHud);
        }

        private void HandleAdvance()
        {
            _runtime?.AdvanceDispatcher?.DispatchAdvance();
        }

        private void HandleAuto()
        {
            Features?.ToggleAuto();
        }

        // 빨리 넘기기 = Ctrl을 누르고 있는 것과 같은 기능(rapid skip). 버튼은 눌러서 켜고 다시 눌러 끈다.
        private void HandleFastForward()
        {
            PlaybackFeatureController features = Features;
            if (features == null)
                return;

            PlaybackRuntimeState state = _playbackState;
            if (state != null && state.IsRapidSkipMode)
                features.EndRapidSkip();
            else
                features.BeginRapidSkip();
        }

        // 한 줄 되감기 — R 키와 같은 길(표적 정하기 → 호스트가 같은 단위를 처음부터 다시).
        private async void HandleRollback()
        {
            try
            {
                if (!IsRunning)
                    return;

                if (!Features.RequestRollbackOneStep())
                    return;

                await _launcher.RequestReplayAsync();
            }
            catch (Exception error)
            {
                Debug.LogException(error);
            }
        }

        private void HandleLineDisplayed()
        {
            _playScreen.SetNextMarkerVisible(true);
            _playScreen.SetChapterLabel(ChapterLabel());
        }

        // 대사창 위 꼬리표: 예제 이름.
        private string ChapterLabel() => Text(_example?.NameKey);
    }
}
