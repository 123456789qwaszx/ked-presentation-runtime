using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Ked.Progression.Save;
using UnityEngine;

namespace Ked.Samples.UI
{
    // 05 저장 · 06 불러오기 — 기존 수동 저장 유즈케이스(IKedSaveSlotPort = ManualSaveFlow · SaveSlotService)만 쓴다.
    //
    // 슬롯 순서: 저장 서비스의 슬롯은 번호 칸이 아니라 목록이고, 덮어쓰면 그 항목이 목록 끝으로 간다.
    // 그래서 화면은 "최근 저장이 01"로 보인다(SavedAtUtc 내림차순). 저장·덮어쓰기 직후의 기록은 언제나 01이다.
    // 저장 모드에서는 목록 다음 칸 하나가 "빈 슬롯(새로 저장)"이고, 그 뒤 칸은 비어 있는 자리(누를 수 없음)다 —
    // 서비스에 번호 칸이 없으니 아무 빈 칸에나 저장할 수 있는 것처럼 보이게 하지 않는다.
    // 자동 저장 탭은 만들지 않는다(서비스에 없다). 이어하기 기록은 타이틀의 이어하기가 쓴다.
    public sealed partial class KedUIFlow
    {
        private enum SlotMode
        {
            Save,
            Load,
        }

        private readonly List<SaveSlotEntry> _slotEntries = new();
        private readonly Dictionary<string, bool> _loadable = new(StringComparer.Ordinal);

        private SlotMode _slotMode;
        private bool _slotsReadable;
        private int _slotPage;
        private int _slotSelected = -1;
        private bool _slotBusy;

        private void SubscribeSlots()
        {
            _slots.BackClicked += SlotsBack;
            _slots.CloseClicked += CloseAllPanels;
            _slots.PreviousClicked += () => TurnSlotPage(-1);
            _slots.NextClicked += () => TurnSlotPage(+1);
            _slots.CardClicked += SelectSlotCard;
            _slots.CardDoubleClicked += index =>
            {
                SelectSlotCard(index);
                ActOnSelectedSlot();
            };
            _slots.ActionClicked += ActOnSelectedSlot;
        }

        private void OpenSlots(SlotMode mode)
        {
            if (_slotPort == null)
                return;

            _slotMode = mode;
            _slotBusy = false;
            ReloadSlots();

            // 저장은 "새로 저장" 칸을 먼저 고른다 — 열자마자 실행 버튼을 눌러도 기록을 덮어쓰지 않게.
            _slotSelected = mode == SlotMode.Save && HasNewSlotCell
                ? _slotEntries.Count
                : FirstSelectableSlot();

            _slotPage = _slotSelected >= 0 ? _slotSelected / SaveLoadPanel.SlotsPerPage : 0;

            PushPanel<SaveLoadPanel>(_ => RedrawSlots(focus: true));
        }

        private void SlotsBack()
        {
            ClosePanel();
        }

        private void ReloadSlots()
        {
            _slotEntries.Clear();
            _loadable.Clear();

            _slotsReadable = _slotPort.TryReadSlots(out IReadOnlyList<SaveSlotEntry> slots);

            if (slots != null)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    if (slots[i] != null)
                        _slotEntries.Add(slots[i]);
                }
            }

            // 최근 저장이 앞(01). 시각을 읽지 못하는 항목은 뒤로.
            _slotEntries.Sort((a, b) => ParseUtc(b.SavedAtUtc).CompareTo(ParseUtc(a.SavedAtUtc)));
        }

        private int CellCount => _slotEntries.Count + (HasNewSlotCell ? 1 : 0);
        private bool HasNewSlotCell => _slotMode == SlotMode.Save && _slotsReadable;
        private int PageCount => Mathf.Max(1, Mathf.CeilToInt(CellCount / (float)SaveLoadPanel.SlotsPerPage));

        private bool IsFilledCell(int cell) => cell >= 0 && cell < _slotEntries.Count;
        private bool IsNewSlotCell(int cell) => HasNewSlotCell && cell == _slotEntries.Count;

        private bool IsSelectableCell(int cell)
        {
            if (_slotMode == SlotMode.Save)
                return IsFilledCell(cell) || IsNewSlotCell(cell);

            return IsFilledCell(cell) && CanLoad(_slotEntries[cell].Id);
        }

        private int FirstSelectableSlot()
        {
            for (int cell = 0; cell < CellCount; cell++)
            {
                if (IsSelectableCell(cell))
                    return cell;
            }

            return -1;
        }

        private bool CanLoad(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            if (!_loadable.TryGetValue(id, out bool loadable))
            {
                loadable = _slotPort.CanLoad(id);
                _loadable[id] = loadable;
            }

            return loadable;
        }

        private void TurnSlotPage(int delta)
        {
            int next = Mathf.Clamp(_slotPage + delta, 0, PageCount - 1);
            if (next == _slotPage)
                return;

            _slotPage = next;
            RedrawSlots(focus: false);
        }

        private void SelectSlotCard(int indexOnPage)
        {
            int cell = _slotPage * SaveLoadPanel.SlotsPerPage + indexOnPage;
            if (!IsSelectableCell(cell))
                return;

            _slotSelected = cell;
            RedrawSlots(focus: false);
        }

        private void RedrawSlots(bool focus)
        {
            bool save = _slotMode == SlotMode.Save;
            int first = _slotPage * SaveLoadPanel.SlotsPerPage;

            var cards = new SaveSlotCard.Model[SaveLoadPanel.SlotsPerPage];

            for (int i = 0; i < cards.Length; i++)
                cards[i] = CardFor(first + i);

            string hint;
            if (!_slotsReadable)
                hint = Text("slots.hint.unreadable");
            else if (!save && _slotEntries.Count == 0)
                hint = Text("slots.hint.none");
            else
                hint = Text(save ? "slots.hint.save" : "slots.hint.load");

            bool actionEnabled = !_slotBusy && IsSelectableCell(_slotSelected) &&
                                 (!save || _slotPort.CanSave);

            _slots.Present(new SaveLoadPanel.Model
            {
                Title = Text(save ? "slots.save.title" : "slots.load.title"),
                Subtitle = Text(save ? "slots.save.subtitle" : "slots.load.subtitle"),
                TabLabel = Text("slots.tab.manual"),
                PageInfo = Format("slots.pageInfo", KedUIUtil.TwoDigits(_slotPage + 1), KedUIUtil.TwoDigits(PageCount)),
                PageNumbers = PageNumbers(),
                Hint = hint,
                ActionLabel = Text(save ? "slots.save.action" : "slots.load.action"),
                ActionEnabled = actionEnabled,
                CanPrevious = _slotPage > 0,
                CanNext = _slotPage < PageCount - 1,
                Cards = cards,
                FocusCard = focus && _slotSelected >= first && _slotSelected < first + cards.Length
                    ? _slotSelected - first
                    : -1,
            });
        }

        private SaveSlotCard.Model CardFor(int cell)
        {
            bool selected = cell == _slotSelected;

            if (IsFilledCell(cell))
            {
                SaveSlotEntry entry = _slotEntries[cell];

                return new SaveSlotCard.Model
                {
                    Filled = true,
                    Number = KedUIUtil.TwoDigits(cell + 1),
                    Title = string.IsNullOrEmpty(entry.Label) ? entry.Preview : entry.Label,
                    Date = FormatSavedAt(entry.SavedAtUtc),
                    Chapter = entry.ChapterId,
                    PlayTime = Format("slots.playTime", FormatPlayTime(entry.PlaySeconds)),
                    Selectable = IsSelectableCell(cell),
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
        private string PageNumbers()
        {
            const int window = 5;
            int count = PageCount;
            int start = Mathf.Clamp(_slotPage - window / 2, 0, Mathf.Max(0, count - window));
            int end = Mathf.Min(count, start + window);

            var parts = new List<string>(window);
            for (int p = start; p < end; p++)
            {
                string number = (p + 1).ToString(CultureInfo.InvariantCulture);
                parts.Add(p == _slotPage ? $"<color=#13BDC5>{number}</color>" : number);
            }

            return string.Join("   ", parts);
        }

        private void ActOnSelectedSlot()
        {
            if (_slotBusy || !IsSelectableCell(_slotSelected))
                return;

            if (_slotMode == SlotMode.Save)
            {
                if (!_slotPort.CanSave)
                    return;

                if (IsNewSlotCell(_slotSelected))
                {
                    SaveInto(null);
                    return;
                }

                SaveSlotEntry entry = _slotEntries[_slotSelected];
                Confirm(new ConfirmPanel.Request
                {
                    Title = Text("confirm.overwrite.title"),
                    Body = Format("confirm.overwrite.body", KedUIUtil.TwoDigits(_slotSelected + 1)),
                    AcceptLabel = Text("confirm.overwrite.accept"),
                    CancelLabel = Text("common.cancel"),
                    OnAccept = () => SaveInto(entry.Id),
                });
                return;
            }

            SaveSlotEntry target = _slotEntries[_slotSelected];

            // 재생 중이면 지금 장면을 벗어난다는 것을 확인한다. 타이틀에서는 잃을 진행이 없다.
            if (IsRunning)
            {
                Confirm(new ConfirmPanel.Request
                {
                    Title = Text("confirm.load.title"),
                    Body = Format("confirm.load.body", KedUIUtil.TwoDigits(_slotSelected + 1)),
                    AcceptLabel = Text("confirm.load.accept"),
                    CancelLabel = Text("common.cancel"),
                    OnAccept = () => LoadFrom(target.Id),
                });
                return;
            }

            LoadFrom(target.Id);
        }

        // 저장 성공 알림은 실제로 끝난 저장에만. 실패하면 화면은 그대로 두고 이유를 알린다.
        private void SaveInto(string overwriteId)
        {
            bool saved;

            try
            {
                saved = overwriteId == null
                    ? _slotPort.TryCreate()
                    : _slotPort.TryOverwrite(overwriteId);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                saved = false;
            }

            ReloadSlots();

            if (saved)
            {
                _slotPage = 0;
                _slotSelected = 0;
                RedrawSlots(focus: true);

                Notice(
                    Text("notice.saved.title"),
                    Format("notice.saved.body", KedUIUtil.TwoDigits(1)),
                    NoticePanel.Kind.Success);
                return;
            }

            _slotSelected = Mathf.Min(_slotSelected, CellCount - 1);
            RedrawSlots(focus: true);

            Notice(
                Text("notice.saveFailed.title"),
                Text("notice.saveFailed.body"),
                NoticePanel.Kind.Failure);
        }

        private async void LoadFrom(string slotId)
        {
            if (_slotBusy)
                return;

            _slotBusy = true;
            RedrawSlots(focus: false);

            bool transitionStarted = false;

            try
            {
                Task load = _slotPort.LoadAsync(slotId, () =>
                {
                    transitionStarted = true;
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
                _slotBusy = false;
            }

            // 회차 전환까지 가지 못했다(본문을 읽지 못함 등) — 화면은 열린 채로 두고 알린다.
            if (!transitionStarted && _ui.TopPanel == _slots)
            {
                _loadable.Remove(slotId);
                RedrawSlots(focus: true);

                Notice(
                    Text("notice.loadFailed.title"),
                    Text("notice.loadFailed.body"),
                    NoticePanel.Kind.Failure);
            }
        }

        private static DateTime ParseUtc(string savedAtUtc)
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
            DateTime utc = ParseUtc(savedAtUtc);
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
