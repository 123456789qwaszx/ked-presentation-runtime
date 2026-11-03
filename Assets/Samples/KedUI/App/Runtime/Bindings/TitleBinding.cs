namespace Ked.Samples.UI
{
    // 01 타이틀 — 회차 입구와 예제 카드(IKedTitlePort). 불러오기는 저장 슬롯이 있는 예제에만.
    internal sealed class TitleBinding : ScreenBinding
    {
        public TitleBinding(KedUIFlow flow, KedUIServices services)
            : base(flow, services)
        {
            Own(flow.Require<TitleScreen>());
        }

        // Root를 타이틀로. 열린 Panel은 흐름이 먼저 닫았다.
        public void Open()
        {
            IKedTitlePort title = Services.Title;

            Flow.SwitchRoot<TitleScreen>(root =>
            {
                BindView(root, ApplyBindings);
                root.Present(new TitleScreen.Model
                {
                    Example = title?.Example,
                    CanContinue = title != null && title.HasResumePoint,
                    ShowLoad = Services.Slots != null,
                    CanSwitchExample = title != null && title.CanSwitchExample,
                });
            });
        }

        private void ApplyBindings(TitleScreen screen)
        {
            AddBinding(screen,
                s => s.StartClicked += StartNew,
                s => s.StartClicked -= StartNew);

            AddBinding(screen,
                s => s.ContinueClicked += Resume,
                s => s.ContinueClicked -= Resume);

            AddBinding(screen,
                s => s.LoadClicked += Flow.OpenLoad,
                s => s.LoadClicked -= Flow.OpenLoad);

            AddBinding(screen,
                s => s.SwitchExampleClicked += SwitchExample,
                s => s.SwitchExampleClicked -= SwitchExample);

            AddBinding(screen,
                s => s.SettingsClicked += Flow.OpenSettings,
                s => s.SettingsClicked -= Flow.OpenSettings);

            AddBinding(screen,
                s => s.QuitClicked += Quit,
                s => s.QuitClicked -= Quit);
        }

        private void StartNew() => Services.Title?.StartNew();
        private void Resume() => Services.Title?.Resume();
        private void SwitchExample() => Services.Title?.SwitchExample();
        private void Quit() => Services.Title?.Quit();
    }
}
