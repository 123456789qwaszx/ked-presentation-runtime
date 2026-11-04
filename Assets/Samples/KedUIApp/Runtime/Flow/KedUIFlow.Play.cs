using System;
using UnityEngine;

namespace Ked.Samples.UI
{
    public sealed partial class KedUIFlow
    {
        private readonly PlayScreen _playScreen;

        // 퀵메뉴 판(자동 ~ 숨기기)을 접었는가. 저장하지 않는다.
        private bool _quickFolded;

        // 무대를 앞으로 가져오고 Root를 재생 화면으로 바꾼다.
        // 이미 재생 화면이면 Presentation만 다시 적용된다(되감기·장면 전환).
        private void OpenPlay()
        {
            ShowStage();

            bool entering = !IsPlayRoot;

            _ui.SwitchRoot<PlayScreen>(
                Presentation<PlayScreen>(),
                afterPresented: root =>
                {
                    BindView(root, ApplyBindings);

                    if (entering)
                        root.SetHudHidden(false);

                    root.SetQuickFolded(_quickFolded);
                    root.SetChapterLabel(ChapterLabel());
                },
                afterClosed: Unbind);
        }

        // 무대(PresentationUIRoot)는 씬의 무대 캔버스에 늘 놓여 있고 UI는 그 위 캔버스에 얹힌다 — 꺼져 있으면 켠다.
        private void ShowStage()
        {
            if (_stage != null && !_stage.gameObject.activeSelf)
                _stage.gameObject.SetActive(true);
        }

        private void ApplyBindings(PlayScreen root)
        {
            AddBinding(root,
                r => r.AdvanceClicked += HandleAdvance,
                r => r.AdvanceClicked -= HandleAdvance);

            AddBinding(root,
                r => r.AutoClicked += HandleAuto,
                r => r.AutoClicked -= HandleAuto);

            AddBinding(root,
                r => r.SpeedClicked += HandleSpeed,
                r => r.SpeedClicked -= HandleSpeed);

            AddBinding(root,
                r => r.RollbackClicked += HandleRollback,
                r => r.RollbackClicked -= HandleRollback);

            AddBinding(root,
                r => r.BacklogClicked += OpenBacklog,
                r => r.BacklogClicked -= OpenBacklog);

            AddBinding(root,
                r => r.HideClicked += HideHud,
                r => r.HideClicked -= HideHud);

            AddBinding(root,
                r => r.FoldClicked += HandleQuickFold,
                r => r.FoldClicked -= HandleQuickFold);

            AddBinding(root,
                r => r.MenuClicked += OpenMenu,
                r => r.MenuClicked -= OpenMenu);

            AddBinding(root,
                r => r.RestoreClicked += RestoreHud,
                r => r.RestoreClicked -= RestoreHud);

            // 줄 표시가 끝났다는 재생 쪽 이벤트 — 재생 화면이 떠 있는 동안만 받는다.
            LinePresentationFlow lineFlow = _runtime?.LineFlow;
            if (lineFlow != null)
            {
                lineFlow.LineDisplayed += HandleLineDisplayed;
                AddCleanup(root, () => lineFlow.LineDisplayed -= HandleLineDisplayed);
            }
        }

        #region Handlers

        private void HandleAdvance()
        {
            _runtime?.AdvanceDispatcher?.DispatchAdvance();
        }

        private void HandleAuto()
        {
            Features?.ToggleAuto();
        }

        // 배속 = 빠르게 재생(S 키와 같은 기능). 누를 때마다 끔 → 2 → 4 → 8배 → 끔.
        // 고른 배속은 설정의 빠르게 재생 배속과 같은 값이라 저장한다. 빨리 넘기기(Ctrl)는 키로만.
        private void HandleSpeed()
        {
            PlaybackFeatureController features = Features;
            PlaybackRuntimeState state = _playbackState;
            if (features == null || state == null)
                return;

            bool on = state.IsSpeedUpMode;

            if (!_settings.TryNextSpeed(on, out float next))
            {
                features.ToggleSpeedUpMode();
                return;
            }

            _settings.SetSpeedUp(next);
            SettingsValues.Store();
            features.SetSpeedUpMultiplier(next);

            if (!on)
                features.ToggleSpeedUpMode();
        }

        private void HandleQuickFold()
        {
            _quickFolded = !_quickFolded;
            _playScreen.SetQuickFolded(_quickFolded);
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

        private void HideHud()
        {
            _playScreen.SetHudHidden(true);
        }

        private void RestoreHud()
        {
            _playScreen.SetHudHidden(false);
        }

        private void HandleLineDisplayed()
        {
            _playScreen.SetNextMarkerVisible(true);
            _playScreen.SetChapterLabel(ChapterLabel());
        }

        #endregion

        #region Refresh

        // 퀵메뉴의 켜짐 표시(자동 진행 · 배속). 키(A · S)로도 바뀌는데 알리는 이벤트가 없어 매 프레임 옮긴다.
        private void RefreshQuickMenuModes()
        {
            if (!IsPlayRoot)
                return;

            PlaybackRuntimeState state = _playbackState;
            if (state != null)
                _playScreen.SetModes(state.IsAutoMode, state.IsSpeedUpMode, SettingsValues.SpeedUpMultiplier);
        }

        // 대사창 위 꼬리표: 예제 이름.
        private string ChapterLabel() => Text(_example?.NameKey);

        #endregion
    }
}
