using System;
using UnityEngine;

namespace Ked.Samples.UI
{
    // 02 대화 · 03 선택지 — 퀵메뉴와 진행 클릭.
    // 재생 쪽 대사창·선택지 상자 계약의 어댑터도 여기서 만든다 — 화면 묶음은 재생 런타임을 모른다.
    internal sealed class PlayBinding : ScreenBinding
    {
        private KedDialogueBoxView _dialogueBoxView;
        private KedOptionsBoxView _optionsBoxView;

        public PlayBinding(KedUIFlow flow, KedUIServices services)
            : base(flow, services)
        {
            View = Own(flow.Require<PlayScreen>());
        }

        public PlayScreen View { get; }

        public IPresentationDialogueBoxView DialogueBox => _dialogueBoxView ??= new KedDialogueBoxView(View.Dialogue);
        public IPresentationOptionsBoxView OptionsBox => _optionsBoxView ??= new KedOptionsBoxView(View.Options);

        public override void OnConfigured()
        {
            if (Services.Playback.LineFlow != null)
                Services.Playback.LineFlow.LineDisplayed += HandleLineDisplayed;
        }

        public override void Dispose()
        {
            if (Services.Playback?.LineFlow != null)
                Services.Playback.LineFlow.LineDisplayed -= HandleLineDisplayed;

            base.Dispose();
        }

        // 퀵메뉴의 켜짐 표시(자동 진행 · 빨리 넘기기).
        public override void Tick()
        {
            if (!Flow.IsPlayRoot)
                return;

            PlaybackRuntimeState state = Services.Playback.PlaybackState;
            if (state != null)
                View.SetModes(state.IsAutoMode, state.IsRapidSkipMode);
        }

        // 무대를 앞으로 가져오고 Root를 재생 화면으로 바꾼다. 이미 재생 화면이면 Presentation만 다시 적용된다(되감기·장면 전환).
        public void Open()
        {
            Services.ShowStage();

            bool entering = !Flow.IsPlayRoot;

            Flow.SwitchRoot<PlayScreen>(root =>
            {
                BindView(root, ApplyBindings);

                if (entering)
                    root.SetHudHidden(false);

                root.SetChapterLabel(ChapterLabel());
            });
        }

        public void RestoreHud()
        {
            View.SetHudHidden(false);
            Flow.SyncInput();
        }

        private void HideHud()
        {
            View.SetHudHidden(true);
            Flow.SyncInput();
        }

        private void ApplyBindings(PlayScreen screen)
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
                s => s.BacklogClicked += Flow.OpenBacklog,
                s => s.BacklogClicked -= Flow.OpenBacklog);

            AddBinding(screen,
                s => s.HideClicked += HideHud,
                s => s.HideClicked -= HideHud);

            AddBinding(screen,
                s => s.MenuClicked += Flow.OpenMenu,
                s => s.MenuClicked -= Flow.OpenMenu);

            AddBinding(screen,
                s => s.RestoreClicked += RestoreHud,
                s => s.RestoreClicked -= RestoreHud);
        }

        private void HandleAdvance()
        {
            Services.Playback?.AdvanceDispatcher?.DispatchAdvance();
        }

        private void HandleAuto()
        {
            Services.Playback?.Features?.ToggleAuto();
        }

        // 빨리 넘기기 = Ctrl을 누르고 있는 것과 같은 기능(rapid skip). 버튼은 눌러서 켜고 다시 눌러 끈다.
        private void HandleFastForward()
        {
            PlaybackFeatureController features = Services.Playback?.Features;
            if (features == null)
                return;

            PlaybackRuntimeState state = Services.Playback.PlaybackState;
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
                if (!Services.IsRunning)
                    return;

                if (!Services.Playback.Features.RequestRollbackOneStep())
                    return;

                await Services.Launcher.RequestReplayAsync();
            }
            catch (Exception error)
            {
                Debug.LogException(error);
            }
        }

        private void HandleLineDisplayed()
        {
            View.SetNextMarkerVisible(true);
            View.SetChapterLabel(ChapterLabel());
        }

        // 대사창 위 꼬리표: 예제 이름.
        private string ChapterLabel() => Text(Services.Example?.NameKey);
    }
}
