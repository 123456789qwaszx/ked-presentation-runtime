using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Ked.Progression.Save;
using UnityEngine;

namespace Ked.Samples.UI
{
    // 05 저장 · 06 불러오기 — 수동 저장 유즈케이스(ManualSaveFlow)만 쓴다.
    //
    // 슬롯 순서: 저장 서비스의 슬롯은 번호 칸이 아니라 목록이고, 덮어쓰면 그 항목이 목록 끝으로 간다.
    // 그래서 화면은 "최근 저장이 01"로 보인다(SavedAtUtc 내림차순). 저장·덮어쓰기 직후의 기록은 언제나 01이다.
    // 저장 모드에서는 목록 다음 칸 하나가 "빈 슬롯(새로 저장)"이고, 그 뒤 칸은 비어 있는 자리(누를 수 없음)다 —
    // 서비스에 번호 칸이 없으니 아무 빈 칸에나 저장할 수 있는 것처럼 보이게 하지 않는다.
    // 자동 저장 탭은 만들지 않는다(서비스에 없다). 이어하기 기록은 타이틀의 이어하기가 쓴다.
    public sealed partial class KedUIFlow
    {
        private enum SaveLoadMode
        {
            Save,
            Load,
        }

        private readonly SaveLoadPanel _saveLoadPanel;
        private readonly List<SaveSlotEntry> _saveLoadEntries = new();
        private readonly Dictionary<string, bool> _saveLoadLoadable = new(StringComparer.Ordinal);

        private SaveLoadMode _saveLoadMode;
        private bool _saveLoadReadable;
        private int _saveLoadPage;
        private int _saveLoadSelected = -1;
        private bool _saveLoadBusy;

        private void OpenSave() => OpenSaveLoad(SaveLoadMode.Save);
        private void OpenLoad() => OpenSaveLoad(SaveLoadMode.Load);

        private void OpenSaveLoad(SaveLoadMode mode)
        {
            if (_saves == null)
                return;

            _saveLoadMode = mode;
            _saveLoadBusy = false;
            ReloadSlots();

            // 저장은 "새로 저장" 칸을 먼저 고른다 — 열자마자 실행 버튼을 눌러도 기록을 덮어쓰지 않게.
            _saveLoadSelected = mode == SaveLoadMode.Save && HasNewSlotCell
                ? _saveLoadEntries.Count
                : FirstSelectableSlot();

            _saveLoadPage = _saveLoadSelected >= 0 ? _saveLoadSelected / SaveLoadPanel.SlotsPerPage : 0;

            PushPanel<SaveLoadPanel>(panel =>
            {
                BindView(panel, ApplySaveLoadBindings);
                RedrawSlots(focus: true);
            });
        }

        private void ApplySaveLoadBindings(SaveLoadPanel panel)
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

        private void ReloadSlots()
        {
            _saveLoadEntries.Clear();
            _saveLoadLoadable.Clear();

            _saveLoadReadable = _saves.TryReadSlots(out IReadOnlyList<SaveSlotEntry> slots);

            if (slots != null)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    if (slots[i] != null)
                        _saveLoadEntries.Add(slots[i]);
                }
            }

            // 최근 저장이 앞(01). 시각을 읽지 못하는 항목은 뒤로.
            _saveLoadEntries.Sort((a, b) => ParseSavedAtUtc(b.SavedAtUtc).CompareTo(ParseSavedAtUtc(a.SavedAtUtc)));
        }

        private int SlotCellCount => _saveLoadEntries.Count + (HasNewSlotCell ? 1 : 0);
        private bool HasNewSlotCell => _saveLoadMode == SaveLoadMode.Save && _saveLoadReadable;
        private int SlotPageCount => Mathf.Max(1, Mathf.CeilToInt(SlotCellCount / (float)SaveLoadPanel.SlotsPerPage));

        private bool IsFilledSlotCell(int cell) => cell >= 0 && cell < _saveLoadEntries.Count;
        private bool IsNewSlotCell(int cell) => HasNewSlotCell && cell == _saveLoadEntries.Count;

        private bool IsSelectableSlotCell(int cell)
        {
            if (_saveLoadMode == SaveLoadMode.Save)
                return IsFilledSlotCell(cell) || IsNewSlotCell(cell);

            return IsFilledSlotCell(cell) && CanLoadSlot(_saveLoadEntries[cell].Id);
        }

        private int FirstSelectableSlot()
        {
            for (int cell = 0; cell < SlotCellCount; cell++)
            {
                if (IsSelectableSlotCell(cell))
                    return cell;
            }

            return -1;
        }

        private bool CanLoadSlot(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            if (!_saveLoadLoadable.TryGetValue(id, out bool loadable))
            {
                loadable = _saves.CanLoad(id);
                _saveLoadLoadable[id] = loadable;
            }

            return loadable;
        }

        private void PreviousSlotPage() => TurnSlotPage(-1);
        private void NextSlotPage() => TurnSlotPage(+1);

        // 쪽을 넘기면 선택도 그 쪽의 첫 칸으로 옮긴다 — 보이지 않는 칸에 실행이 걸리지 않게.
        private void TurnSlotPage(int delta)
        {
            int next = Mathf.Clamp(_saveLoadPage + delta, 0, SlotPageCount - 1);
            if (next == _saveLoadPage)
                return;

            _saveLoadPage = next;
            _saveLoadSelected = FirstSelectableSlotOnPage(_saveLoadPage);
            RedrawSlots(focus: false);
        }

        private int FirstSelectableSlotOnPage(int page)
        {
            int first = page * SaveLoadPanel.SlotsPerPage;
            int end = Mathf.Min(SlotCellCount, first + SaveLoadPanel.SlotsPerPage);

            for (int cell = first; cell < end; cell++)
            {
                if (IsSelectableSlotCell(cell))
                    return cell;
            }

            return -1;
        }

        private bool IsOnCurrentSlotPage(int cell) =>
            cell >= 0 && cell / SaveLoadPanel.SlotsPerPage == _saveLoadPage;

        private void SelectSlotCard(int indexOnPage)
        {
            int cell = _saveLoadPage * SaveLoadPanel.SlotsPerPage + indexOnPage;
            if (!IsSelectableSlotCell(cell))
                return;

            _saveLoadSelected = cell;
            RedrawSlots(focus: false);
        }

        private void ActOnSlotCard(int indexOnPage)
        {
            SelectSlotCard(indexOnPage);
            ActOnSelectedSlot();
        }

        private void RedrawSlots(bool focus)
        {
            bool save = _saveLoadMode == SaveLoadMode.Save;
            int first = _saveLoadPage * SaveLoadPanel.SlotsPerPage;

            var cards = new SaveSlotCard.Model[SaveLoadPanel.SlotsPerPage];

            for (int i = 0; i < cards.Length; i++)
                cards[i] = SlotCardFor(first + i);

            string hint;
            if (!_saveLoadReadable)
                hint = Text("slots.hint.unreadable");
            else if (!save && _saveLoadEntries.Count == 0)
                hint = Text("slots.hint.none");
            else
                hint = Text(save ? "slots.hint.save" : "slots.hint.load");

            bool actionEnabled = !_saveLoadBusy && IsOnCurrentSlotPage(_saveLoadSelected) &&
                                 IsSelectableSlotCell(_saveLoadSelected) && (!save || _saves.CanSave);

            _saveLoadPanel.Present(new SaveLoadPanel.Model
            {
                Title = Text(save ? "slots.save.title" : "slots.load.title"),
                Subtitle = Text(save ? "slots.save.subtitle" : "slots.load.subtitle"),
                TabLabel = Text("slots.tab.manual"),
                PageInfo = Format("slots.pageInfo", KedUIUtil.TwoDigits(_saveLoadPage + 1), KedUIUtil.TwoDigits(SlotPageCount)),
                PageNumbers = SlotPageNumbers(),
                Hint = hint,
                ActionLabel = Text(save ? "slots.save.action" : "slots.load.action"),
                ActionEnabled = actionEnabled,
                CanPrevious = _saveLoadPage > 0,
                CanNext = _saveLoadPage < SlotPageCount - 1,
                Cards = cards,
                FocusCard = focus && _saveLoadSelected >= first && _saveLoadSelected < first + cards.Length
                    ? _saveLoadSelected - first
                    : -1,
            });
        }

        private SaveSlotCard.Model SlotCardFor(int cell)
        {
            bool selected = cell == _saveLoadSelected;

            if (IsFilledSlotCell(cell))
            {
                SaveSlotEntry entry = _saveLoadEntries[cell];

                return new SaveSlotCard.Model
                {
                    Filled = true,
                    Number = KedUIUtil.TwoDigits(cell + 1),
                    Title = string.IsNullOrEmpty(entry.Label) ? entry.Preview : entry.Label,
                    Date = FormatSavedAt(entry.SavedAtUtc),
                    Chapter = entry.ChapterId,
                    PlayTime = Format("slots.playTime", FormatPlayTime(entry.PlaySeconds)),
                    Selectable = IsSelectableSlotCell(cell),
                    Selected = selected,
                };
            }

            bool isNew = IsNewSlotCell(cell);

            return new SaveSlotCard.Model
            {
                Filled = false,
                EmptyTitle = Text("slots.empty"),
                EmptyHint = isNew ? Text("slots.empty.hint") : string.Empty,
                Selectable = isNew,
                Selected = selected && isNew,
            };
        }

        // "1 2 3 4 5" — 지금 쪽만 청록. 쪽이 많으면 지금 쪽 둘레만.
        private string SlotPageNumbers()
        {
            const int window = 5;
            int count = SlotPageCount;
            int start = Mathf.Clamp(_saveLoadPage - window / 2, 0, Mathf.Max(0, count - window));
            int end = Mathf.Min(count, start + window);

            var parts = new List<string>(window);
            for (int p = start; p < end; p++)
            {
                string number = (p + 1).ToString(CultureInfo.InvariantCulture);
                parts.Add(p == _saveLoadPage ? $"<color=#13BDC5>{number}</color>" : number);
            }

            return string.Join("   ", parts);
        }

        private void ActOnSelectedSlot()
        {
            if (_saveLoadBusy || !IsOnCurrentSlotPage(_saveLoadSelected) || !IsSelectableSlotCell(_saveLoadSelected))
                return;

            if (_saveLoadMode == SaveLoadMode.Save)
            {
                if (!_saves.CanSave)
                    return;

                if (IsNewSlotCell(_saveLoadSelected))
                {
                    SaveIntoSlot(null);
                    return;
                }

                SaveSlotEntry entry = _saveLoadEntries[_saveLoadSelected];
                Confirm(new ConfirmPanel.Request
                {
                    Title = Text("confirm.overwrite.title"),
                    Body = Format("confirm.overwrite.body", KedUIUtil.TwoDigits(_saveLoadSelected + 1)),
                    AcceptLabel = Text("confirm.overwrite.accept"),
                    CancelLabel = Text("common.cancel"),
                    OnAccept = () => SaveIntoSlot(entry.Id),
                });
                return;
            }

            SaveSlotEntry target = _saveLoadEntries[_saveLoadSelected];

            // 재생 중이면 지금 장면을 벗어난다는 것을 확인한다. 타이틀에서는 잃을 진행이 없다.
            if (IsRunning)
            {
                Confirm(new ConfirmPanel.Request
                {
                    Title = Text("confirm.load.title"),
                    Body = Format("confirm.load.body", KedUIUtil.TwoDigits(_saveLoadSelected + 1)),
                    AcceptLabel = Text("confirm.load.accept"),
                    CancelLabel = Text("common.cancel"),
                    OnAccept = () => LoadFromSlot(target.Id),
                });
                return;
            }

            LoadFromSlot(target.Id);
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

            ReloadSlots();

            if (saved)
            {
                _saveLoadPage = 0;
                _saveLoadSelected = 0;
                RedrawSlots(focus: true);

                Notice(
                    Text("notice.saved.title"),
                    Format("notice.saved.body", KedUIUtil.TwoDigits(1)),
                    NoticePanel.Kind.Success);
                return;
            }

            _saveLoadSelected = Mathf.Min(_saveLoadSelected, SlotCellCount - 1);
            RedrawSlots(focus: true);

            Notice(
                Text("notice.saveFailed.title"),
                Text("notice.saveFailed.body"),
                NoticePanel.Kind.Failure);
        }

        private async void LoadFromSlot(string slotId)
        {
            if (_saveLoadBusy)
                return;

            _saveLoadBusy = true;
            RedrawSlots(focus: false);

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
            if (!transitionStarted && IsTop(_saveLoadPanel))
            {
                _saveLoadLoadable.Remove(slotId);
                RedrawSlots(focus: true);

                Notice(
                    Text("notice.loadFailed.title"),
                    Text("notice.loadFailed.body"),
                    NoticePanel.Kind.Failure);
            }
        }

        private static DateTime ParseSavedAtUtc(string savedAtUtc)
        {
            return DateTime.TryParse(
                savedAtUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTime value)
                ? value.ToUniversalTime()
                : DateTime.MinValue;
        }

        private static string FormatSavedAt(string savedAtUtc)
        {
            DateTime utc = ParseSavedAtUtc(savedAtUtc);
            return utc == DateTime.MinValue
                ? string.Empty
                : utc.ToLocalTime().ToString("yyyy.MM.dd  HH:mm", CultureInfo.InvariantCulture);
        }

        private static string FormatPlayTime(int seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(Mathf.Max(0, seconds));
            return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
        }
    }
}
