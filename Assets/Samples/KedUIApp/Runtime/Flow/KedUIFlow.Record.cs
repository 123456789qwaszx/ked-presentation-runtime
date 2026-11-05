using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ked.Progression.Save;
using UnityEngine;

namespace Ked.Samples.UI
{
    // 기록 — 저장 · 불러오기 · 팁 · 캐릭터. 화면 연결과 동작(저장 · 덮어쓰기 · 불러오기 · 음성 켜기/끄기)만.
    // 여는 길은 OpenRecord(탭) 하나 — 메뉴의 저장 · 불러오기, 타이틀의 불러오기, 차후 알림(팁 · 새 인물)이 탭을 골라 연다.
    // 탭은 UIPF Page — RecordPanel(Page Owner)의 PageRoot 아래 Page 넷을 SwitchPage로 바꿔 낀다.
    // 슬롯의 쪽 · 선택 · 카드는 SaveSlotsState가, 글자와 형식은 각 Page가 정한다.
    // 자동 저장 탭은 만들지 않는다(서비스에 없다). 이어하기 기록은 타이틀의 이어하기가 쓴다.
    public sealed partial class KedUIFlow
    {
        private readonly SaveSlotsState _saveSlots = new();

        private RecordPanel.Tab _recordTab;
        private int _recordTip;

        // 불러오기를 기다리는 중 — 카드 누름을 막는다.
        private bool _saveLoadBusy;

        private void OpenSave() => OpenRecord(RecordPanel.Tab.Save);
        private void OpenLoad() => OpenRecord(RecordPanel.Tab.Load);

        private void OpenRecord(RecordPanel.Tab tab)
        {
            _ui.PushPanel<RecordPanel>(Presentation<RecordPanel>(), panel =>
            {
                BindView(panel, ApplyBindings);

                panel.SetTabAvailable(RecordPanel.Tab.Save, CanUseRecordTab(RecordPanel.Tab.Save));
                panel.SetTabAvailable(RecordPanel.Tab.Load, CanUseRecordTab(RecordPanel.Tab.Load));
                SelectRecordTab(CanUseRecordTab(tab) ? tab : RecordPanel.Tab.Tips);
            });
        }

        // 지금 탭의 Page를 열고 그린다. 이미 열린 Page면 다시 그리기만 한다.
        private void ShowRecordPage()
        {
            RecordPanel owner = _ui.GetUI<RecordPanel>();

            switch (_recordTab)
            {
                case RecordPanel.Tab.Save:
                    OpenSlots(save: true);
                    _ui.SwitchPage<SavePage>(
                        owner,
                        Presentation<SavePage>(),
                        afterPresented: page =>
                        {
                            BindView(page, ApplyBindings);
                            RefreshSlots(focus: true);
                        },
                        afterClosed: Unbind);
                    break;
                case RecordPanel.Tab.Load:
                    OpenSlots(save: false);
                    _ui.SwitchPage<LoadPage>(
                        owner,
                        Presentation<LoadPage>(),
                        afterPresented: page =>
                        {
                            BindView(page, ApplyBindings);
                            RefreshSlots(focus: true);
                        },
                        afterClosed: Unbind);
                    break;
                case RecordPanel.Tab.Tips:
                    _ui.SwitchPage<TipsPage>(
                        owner,
                        Presentation<TipsPage>(),
                        afterPresented: page =>
                        {
                            BindView(page, ApplyBindings);
                            RefreshTips(page);
                        },
                        afterClosed: Unbind);
                    break;
                case RecordPanel.Tab.Characters:
                    _ui.SwitchPage<CharactersPage>(
                        owner,
                        Presentation<CharactersPage>(),
                        afterPresented: page =>
                        {
                            BindView(page, ApplyBindings);
                            RefreshCharacters(page);
                        },
                        afterClosed: Unbind);
                    break;
            }
        }

        private void ApplyBindings(RecordPanel panel)
        {
            AddBinding(panel,
                p => p.BackClicked += ClosePanel,
                p => p.BackClicked -= ClosePanel);

            AddBinding(panel,
                p => p.TabClicked += SelectRecordTab,
                p => p.TabClicked -= SelectRecordTab);
        }

        // 저장 · 불러오기 탭이 같이 쓴다.
        private void ApplyBindings(SaveSlotsPage page)
        {
            AddBinding(page,
                p => p.PreviousClicked += PreviousSlotPage,
                p => p.PreviousClicked -= PreviousSlotPage);

            AddBinding(page,
                p => p.NextClicked += NextSlotPage,
                p => p.NextClicked -= NextSlotPage);

            AddBinding(page,
                p => p.CardClicked += HandleSlotCardClicked,
                p => p.CardClicked -= HandleSlotCardClicked);
        }

        private void ApplyBindings(TipsPage page)
        {
            AddBinding(page,
                p => p.TipClicked += SelectTip,
                p => p.TipClicked -= SelectTip);
        }

        private void ApplyBindings(CharactersPage page)
        {
            AddBinding(page,
                p => p.VoiceToggled += HandleRecordVoiceToggled,
                p => p.VoiceToggled -= HandleRecordVoiceToggled);
        }

        #region Handlers

        private void SelectRecordTab(RecordPanel.Tab tab)
        {
            if (!CanUseRecordTab(tab))
                return;

            RecordPanel panel = _ui.GetUI<RecordPanel>();

            _recordTab = tab;
            panel.SetSelectedTab(tab);
            ShowRecordPage();

            // 슬롯 탭은 카드에 포커스를 둔다(RefreshSlots).
            if (tab == RecordPanel.Tab.Tips || tab == RecordPanel.Tab.Characters)
                panel.FocusTab(tab);
        }

        private void SelectTip(int index)
        {
            _recordTip = index;
            RefreshTips(_ui.GetUI<TipsPage>());
        }

        // 설정 › 음량의 캐릭터별 음성과 같은 값. 재생(VoicePlayer)에는 아직 반영하지 않는다.
        private void HandleRecordVoiceToggled(string characterId, bool on)
        {
            _settings.SetCharacterVoiceOn(characterId, on);
            SettingsValues.Store();
        }

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

        // 카드를 누르면 고르고 바로 묻는다 — 확인하면 그대로 저장 · 불러오기(실행 버튼은 없다).
        private void HandleSlotCardClicked(int indexOnPage)
        {
            if (_saveLoadBusy || !_saveSlots.Select(indexOnPage))
                return;

            RefreshSlots(focus: false);

            if (_saveSlots.SaveMode)
            {
                // 저장할 수 없는 순간(대사가 아직 표시되지 않음 등) — 묻지 않고 이유를 알린다.
                if (!_saves.CanSave)
                {
                    OpenNotice(NoticePanel.Message.SaveFailed);
                    return;
                }

                string overwriteId = _saveSlots.SelectedIsNewSlot ? null : _saveSlots.SelectedEntry.Id;
                OpenConfirm(
                    overwriteId == null ? ConfirmPanel.Question.Save : ConfirmPanel.Question.Overwrite,
                    () => SaveIntoSlot(overwriteId),
                    _saveSlots.SelectedNumber);
                return;
            }

            string loadId = _saveSlots.SelectedEntry.Id;
            OpenConfirm(
                ConfirmPanel.Question.Load,
                () => LoadFromSlot(loadId),
                _saveSlots.SelectedNumber);
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
            if (!transitionStarted && _ui.TopPanel is RecordPanel)
            {
                _saveSlots.ForgetLoadable(slotId);
                RefreshSlots(focus: true);

                OpenNotice(NoticePanel.Message.LoadFailed);
            }
        }

        #endregion

        #region Refresh

        // 저장은 재생 중에만, 저장 · 불러오기는 저장 슬롯이 있는 예제에서만. 팁 · 캐릭터는 늘.
        private bool CanUseRecordTab(RecordPanel.Tab tab) => tab switch
        {
            RecordPanel.Tab.Save => _saves != null && IsRunning,
            RecordPanel.Tab.Load => _saves != null,
            _ => true,
        };

        // 슬롯 탭을 열 때마다 목록을 다시 읽는다 — 다른 탭에서 저장했을 수 있다.
        private void OpenSlots(bool save)
        {
            _saveLoadBusy = false;

            bool readable = _saves.TryReadSlots(out IReadOnlyList<SaveSlotEntry> slots);
            _saveSlots.Open(save, slots, readable, _saves.CanLoad);
        }

        private void RefreshSlots(bool focus)
        {
            SaveSlotsPage page = _saveSlots.SaveMode ? _ui.GetUI<SavePage>() : _ui.GetUI<LoadPage>();
            page.Present(_saveSlots.ToModel(focus));
        }

        private void RefreshTips(TipsPage page)
        {
            page.Present(new TipsPage.Model { Selected = _recordTip });
        }

        private void RefreshCharacters(CharactersPage page)
        {
            page.Present(_settings.CharactersModel(page.CharacterIds));
        }

        #endregion
    }
}
