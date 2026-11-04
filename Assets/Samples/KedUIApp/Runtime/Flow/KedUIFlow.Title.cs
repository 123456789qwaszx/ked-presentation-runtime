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
            _ui.SwitchRoot<TitleScreen>(Presentation<TitleScreen>(), root =>
            {
                BindView(root, ApplyTitleBindings);
                root.Present(new TitleScreen.Model
                {
                    Example = _example,
                    CanContinue = HasResumePoint(),
                    ShowLoad = _saves != null,
                    CanSwitchExample = _switchExample != null,
                });
            }, Unbind);
        }

        private void ApplyTitleBindings(TitleScreen screen)
        {
            AddBinding(screen,
                s => s.StartClicked += StartNew,
                s => s.StartClicked -= StartNew);

            AddBinding(screen,
                s => s.ContinueClicked += Resume,
                s => s.ContinueClicked -= Resume);

            AddBinding(screen,
                s => s.LoadClicked += OpenLoad,
                s => s.LoadClicked -= OpenLoad);

            AddBinding(screen,
                s => s.SwitchExampleClicked += SwitchExample,
                s => s.SwitchExampleClicked -= SwitchExample);

            AddBinding(screen,
                s => s.SettingsClicked += OpenSettings,
                s => s.SettingsClicked -= OpenSettings);

            AddBinding(screen,
                s => s.QuitClicked += Quit,
                s => s.QuitClicked -= Quit);
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

        private void StartNew() => _launcher?.StartNew();
        private void Resume() => _launcher?.Resume();
        private void SwitchExample() => _switchExample?.Invoke();

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
