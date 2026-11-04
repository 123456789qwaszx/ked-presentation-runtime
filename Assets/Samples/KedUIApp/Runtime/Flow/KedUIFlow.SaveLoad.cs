using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ked.Progression.Save;
using UnityEngine;

namespace Ked.Samples.UI
{
    // 05 저장 · 06 불러오기 — 화면 연결과 동작(저장 · 덮어쓰기 · 불러오기)만.
    // 무엇을 보일지(쪽 · 선택 · 카드)는 SaveSlotsState가, 글자와 형식은 화면(SaveLoadPanel)이 정한다.
    // 자동 저장 탭은 만들지 않는다(서비스에 없다). 이어하기 기록은 타이틀의 이어하기가 쓴다.
    public sealed partial class KedUIFlow
    {
        private readonly SaveSlotsState _saveSlots = new();

        // 불러오기를 기다리는 중 — 실행 버튼을 막는다.
        private bool _saveLoadBusy;

        private void OpenSave() => OpenSaveLoad(save: true);
        private void OpenLoad() => OpenSaveLoad(save: false);

        private void OpenSaveLoad(bool save)
        {
            _saveLoadBusy = false;

            bool readable = _saves.TryReadSlots(out IReadOnlyList<SaveSlotEntry> slots);
            _saveSlots.Open(save, slots, readable, _saves.CanLoad);

            _ui.PushPanel<SaveLoadPanel>(Presentation<SaveLoadPanel>(), panel =>
            {
                BindView(panel, ApplyBindings);
                RefreshSlots(focus: true);
            });
        }

        private void ApplyBindings(SaveLoadPanel panel)
        {
            AddBinding(panel,
                p => p.BackClicked += ClosePanel,
                p => p.BackClicked -= ClosePanel);

            AddBinding(panel,
                p => p.CloseClicked += CloseAllPanels,
                p => p.CloseClicked -= CloseAllPanels);

            AddBinding(panel,
                p => p.PreviousClicked += PreviousSlotPage,
                p => p.PreviousClicked -= PreviousSlotPage);

            AddBinding(panel,
                p => p.NextClicked += NextSlotPage,
                p => p.NextClicked -= NextSlotPage);

            AddBinding(panel,
                p => p.CardClicked += SelectSlotCard,
                p => p.CardClicked -= SelectSlotCard);

            AddBinding(panel,
                p => p.CardDoubleClicked += ActOnSlotCard,
                p => p.CardDoubleClicked -= ActOnSlotCard);

            AddBinding(panel,
                p => p.ActionClicked += ActOnSelectedSlot,
                p => p.ActionClicked -= ActOnSelectedSlot);
        }

        #region Handlers

        private void PreviousSlotPage()
        {
            if (_saveSlots.TurnPage(-1))
                RefreshSlots(focus: false);
        }

        private void NextSlotPage()
        {
            if (_saveSlots.TurnPage(+1))
                RefreshSlots(focus: false);
        }

        private void SelectSlotCard(int indexOnPage)
        {
            if (_saveSlots.Select(indexOnPage))
                RefreshSlots(focus: false);
        }

        private void ActOnSlotCard(int indexOnPage)
        {
            SelectSlotCard(indexOnPage);
            ActOnSelectedSlot();
        }

        private void ActOnSelectedSlot()
        {
            if (_saveLoadBusy || !_saveSlots.HasTarget)
                return;

            if (_saveSlots.SaveMode)
            {
                if (!_saves.CanSave)
                    return;

                if (_saveSlots.SelectedIsNewSlot)
                {
                    SaveIntoSlot(null);
                    return;
                }

                string overwriteId = _saveSlots.SelectedEntry.Id;
                OpenConfirm(
                    ConfirmPanel.Question.Overwrite, 
                    () => SaveIntoSlot(overwriteId),
                    _saveSlots.SelectedNumber);
                return;
            }

            string loadId = _saveSlots.SelectedEntry.Id;

            // 재생 중이면 지금 장면을 벗어난다는 것을 확인한다. 타이틀에서는 잃을 진행이 없다.
            if (IsRunning)
            {
                OpenConfirm(
                    ConfirmPanel.Question.Load, 
                    () => LoadFromSlot(loadId), 
                    _saveSlots.SelectedNumber);
                return;
            }

            LoadFromSlot(loadId);
        }

        // 저장 성공 알림은 실제로 끝난 저장에만. 실패하면 화면은 그대로 두고 이유를 알린다.
        private void SaveIntoSlot(string overwriteId)
        {
            bool saved;

            try
            {
                saved = overwriteId == null
                    ? _saves.TryCreate()
                    : _saves.TryOverwrite(overwriteId);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                saved = false;
            }

            bool readable = _saves.TryReadSlots(out IReadOnlyList<SaveSlotEntry> slots);
            _saveSlots.Reload(slots, readable);

            if (saved)
            {
                _saveSlots.SelectLatest();
                RefreshSlots(focus: true);

                // 방금 저장한 기록은 언제나 01이다.
                OpenNotice(NoticePanel.Message.Saved, slotNumber: 1);
                return;
            }

            _saveSlots.ClampSelection();
            RefreshSlots(focus: true);

            OpenNotice(NoticePanel.Message.SaveFailed);
        }

        private async void LoadFromSlot(string slotId)
        {
            if (_saveLoadBusy)
                return;

            _saveLoadBusy = true;
            RefreshSlots(focus: false);

            bool transitionStarted = false;

            try
            {
                Task load = _saves.LoadAsync(slotId, () =>
                {
                    transitionStarted = true;
                    StopFastForward();
                    CloseAllPanels();
                });

                await load;
            }
            catch (Exception error)
            {
                Debug.LogException(error);
            }
            finally
            {
                _saveLoadBusy = false;
            }

            // 회차 전환까지 가지 못했다(본문을 읽지 못함 등) — 화면은 열린 채로 두고 알린다.
            if (!transitionStarted && _ui.TopPanel is SaveLoadPanel)
            {
                _saveSlots.ForgetLoadable(slotId);
                RefreshSlots(focus: true);

                OpenNotice(NoticePanel.Message.LoadFailed);
            }
        }

        #endregion

        #region Refresh

        private void RefreshSlots(bool focus) =>
            _ui.GetUI<SaveLoadPanel>().Present(_saveSlots.ToModel(_saveLoadBusy, _saves.CanSave, focus));

        #endregion
    }
}
