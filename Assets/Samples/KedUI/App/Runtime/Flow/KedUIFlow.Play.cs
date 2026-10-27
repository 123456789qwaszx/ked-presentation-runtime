using System;
using UnityEngine;

namespace Ked.Samples.UI
{
    // 02 대화 · 03 선택지 — 퀵메뉴와 진행 클릭.
    public sealed partial class KedUIFlow
    {
        private void SubscribePlay()
        {
            _play.AdvanceClicked += HandleAdvance;
            _play.AutoClicked += HandleAuto;
            _play.FastForwardClicked += HandleFastForward;
            _play.RollbackClicked += HandleRollback;
            _play.BacklogClicked += OpenBacklog;
            _play.HideClicked += HideHud;
            _play.MenuClicked += OpenMenu;
            _play.RestoreClicked += RestoreHud;
        }

        private void HandleAdvance()
        {
            _playback?.AdvanceDispatcher?.DispatchAdvance();
        }

        private void HandleAuto()
        {
            _playback?.Features?.ToggleAuto();
        }

        // 빨리 넘기기 = Ctrl을 누르고 있는 것과 같은 기능(rapid skip). 버튼은 눌러서 켜고 다시 눌러 끈다.
        private void HandleFastForward()
        {
            VNFeatureController features = _playback?.Features;
            if (features == null)
                return;

            if (_playback.PlaybackState != null && _playback.PlaybackState.IsRapidSkipMode)
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

                if (!_playback.Features.RequestRollbackOneStep())
                    return;

                await _playback.Host.RequestReplayAsync();
            }
            catch (Exception error)
            {
                Debug.LogException(error);
            }
        }

        private void HideHud()
        {
            _play.SetHudHidden(true);
            SyncInput();
        }

        private void RestoreHud()
        {
            _play.SetHudHidden(false);
            SyncInput();
        }

        private void HandleLineDisplayed()
        {
            _play.SetNextMarkerVisible(true);
            _play.SetChapterLabel(ChapterLabel());
        }

        // 대사창 위 꼬리표: "예제 · 지금 장면". 장면 이름은 재생 쪽이 아는 노드 이름이다.
        private string ChapterLabel()
        {
            string example = Text(_titlePort?.Example?.NameKey);
            string unit = _playback?.CurrentUnitLabel?.Invoke();

            if (string.IsNullOrEmpty(unit))
                return example;

            return Format("play.chapterTag", example, unit);
        }
    }
}
