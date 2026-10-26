namespace Ked.Samples.UI
{
    // 01 타이틀 · Root 전환.
    public sealed partial class KedUIFlow
    {
        private void SubscribeTitle()
        {
            _title.StartClicked += () => _titlePort?.StartNew();
            _title.ContinueClicked += () => _titlePort?.Resume();
            _title.LoadClicked += () => OpenSlots(SlotMode.Load);
            _title.SwitchExampleClicked += () => _titlePort?.SwitchExample();
            _title.SettingsClicked += OpenSettings;
            _title.QuitClicked += () => _titlePort?.Quit();
        }

        /// <summary>타이틀로. 열린 Panel을 모두 닫고 Root를 타이틀로 바꾼다. 재생은 호스트가 이미 끝냈다(ExitAsync).</summary>
        public void GoToTitle()
        {
            CloseAllPanels();

            _ui.SwitchRoot<TitleScreen>(
                Presentation<TitleScreen>(),
                root => root.Present(new TitleScreen.Model
                {
                    Example = _titlePort?.Example,
                    CanContinue = _titlePort != null && _titlePort.HasResumePoint,
                    ShowLoad = _slotPort != null,
                    CanSwitchExample = _titlePort != null && _titlePort.CanSwitchExample,
                }));

            SyncInput();
        }

        /// <summary>
        /// IPresentationView — 재생이 시작될 때 본체가 부른다. 무대를 앞으로 가져오고 Root를 재생 화면으로 바꾼다.
        /// 이미 재생 화면이면 Presentation만 다시 적용된다(되감기·장면 전환).
        /// </summary>
        public void GoToPresentationView()
        {
            _stage?.ShowStage();

            bool entering = !IsPlayRoot;

            _ui.SwitchRoot<PlayScreen>(
                Presentation<PlayScreen>(),
                root =>
                {
                    if (entering)
                        root.SetHudHidden(false);

                    root.SetChapterLabel(ChapterLabel());
                });

            SyncInput();
        }
    }
}
