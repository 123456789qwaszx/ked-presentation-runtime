using System;
using UnityEngine;

namespace Ked.Samples.UI
{
    // 01 타이틀 — 회차 입구(ProgressionLauncher)와 예제 카드.
    public sealed partial class KedUIFlow
    {
        // Root를 타이틀로. 열린 Panel은 GoToTitle이 먼저 닫았다.
        private void OpenTitle()
        {
            _ui.SwitchRoot<TitleScreen>(
                Presentation<TitleScreen>(),
                afterPresented: root =>
                {
                    BindView(root, ApplyBindings);
                    RefreshTitle(root);
                },
                afterClosed: Unbind);
        }

        private void ApplyBindings(TitleScreen root)
        {
            AddBinding(root,
                r => r.StartClicked += HandleStartClicked,
                r => r.StartClicked -= HandleStartClicked);

            AddBinding(root,
                r => r.ContinueClicked += HandleContinueClicked,
                r => r.ContinueClicked -= HandleContinueClicked);

            AddBinding(root,
                r => r.LoadClicked += OpenLoad,
                r => r.LoadClicked -= OpenLoad);

            AddBinding(root,
                r => r.ExtraClicked += OpenExtra,
                r => r.ExtraClicked -= OpenExtra);

            AddBinding(root,
                r => r.SwitchExampleClicked += HandleSwitchExampleClicked,
                r => r.SwitchExampleClicked -= HandleSwitchExampleClicked);

            AddBinding(root,
                r => r.SettingsClicked += OpenSettings,
                r => r.SettingsClicked -= OpenSettings);

            AddBinding(root,
                r => r.QuitClicked += HandleQuitClicked,
                r => r.QuitClicked -= HandleQuitClicked);
        }

        #region Handlers

        private void HandleStartClicked()
        {
            _launcher?.StartNew();
        }

        private void HandleContinueClicked()
        {
            _launcher?.Resume();
        }

        private void HandleSwitchExampleClicked()
        {
            _switchExample?.Invoke();
        }

        private static void HandleQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Refresh

        private void RefreshTitle(TitleScreen root)
        {
            root.Present(new TitleScreen.Model
            {
                Example = _example,
                CanContinue = HasResumePoint(),
                ShowLoad = _saves != null,
                CanSwitchExample = _switchExample != null,
            });
        }

        // 이어할 기록이 있는가 — 가벼운 확인이다. 읽다 실패하면 이어하기를 막는다.
        private bool HasResumePoint()
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

        #endregion
    }
}
