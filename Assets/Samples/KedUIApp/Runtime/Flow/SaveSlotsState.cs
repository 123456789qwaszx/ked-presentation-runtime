using System;
using System.Collections.Generic;
using System.Globalization;
using Ked.Progression.Save;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 저장 · 불러오기 화면의 상태와 규칙 — 어느 기록이 몇 쪽 몇째 칸인지, 무엇을 고를 수 있는지, 무엇을 보일지.
    /// 슬롯 목록과 불러오기 가능 판정을 받아 화면 모델(뜻)을 낸다. 호스트 · UIPF · 화면 동작을 모르고,
    /// 글자와 형식은 화면(SavePage · LoadPage · SaveSlotCard)이 정한다. 저장 · 불러오기를 부르는 것은 흐름(KedUIFlow.Record)이다.
    ///
    /// 칸: 저장 서비스의 슬롯은 번호 칸이 아니라 목록이고, 덮어쓰면 그 항목이 목록 끝으로 간다.
    /// 그래서 최근 저장이 01이다(SavedAtUtc 내림차순). 저장 모드에서는 목록 다음 칸 하나가 "새로 저장" 칸이고,
    /// 그 뒤 칸은 비어 있는 자리(고를 수 없음)다 — 서비스에 번호 칸이 없으니 아무 빈 칸에나 저장할 수 있는 것처럼 보이지 않게.
    /// </summary>
    internal sealed class SaveSlotsState
    {
        private const int PerPage = SaveSlotsPage.SlotsPerPage;

        private readonly List<SaveSlotEntry> _entries = new();
        private readonly Dictionary<string, bool> _loadable = new(StringComparer.Ordinal);
        private Func<string, bool> _canLoad;

        public bool SaveMode { get; private set; }
        public bool Readable { get; private set; }
        public int Page { get; private set; }
        public int Selected { get; private set; } = -1;

        /// <summary>고른 칸이 지금 쪽에 있고 고를 수 있는 칸이다 — 확인 창이 물을 곳.</summary>
        public bool HasTarget => IsOnPage(Selected) && IsSelectable(Selected);

        public bool SelectedIsNewSlot => IsNewSlotCell(Selected);
        public SaveSlotEntry SelectedEntry => IsFilledCell(Selected) ? _entries[Selected] : null;

        /// <summary>화면에 보이는 번호(01부터).</summary>
        public int SelectedNumber => Selected + 1;

        // 저장은 "새로 저장" 칸을 먼저 고른다(포커스) — 열자마자 Enter를 눌러도 덮어쓰기가 아니라 새로 저장을 묻게.
        public void Open(bool saveMode, IReadOnlyList<SaveSlotEntry> slots, bool readable, Func<string, bool> canLoad)
        {
            SaveMode = saveMode;
            _canLoad = canLoad;
            Reload(slots, readable);

            Selected = saveMode && HasNewSlotCell ? _entries.Count : FirstSelectable(0, CellCount);
            Page = Selected >= 0 ? Selected / PerPage : 0;
        }

        // 최근 저장이 앞(01). 시각을 읽지 못하는 항목은 뒤로.
        public void Reload(IReadOnlyList<SaveSlotEntry> slots, bool readable)
        {
            Readable = readable;
            _entries.Clear();
            _loadable.Clear();

            if (slots != null)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    if (slots[i] != null)
                        _entries.Add(slots[i]);
                }
            }

            _entries.Sort((a, b) => ParseUtc(b.SavedAtUtc).CompareTo(ParseUtc(a.SavedAtUtc)));
        }

        /// <summary>방금 저장한 기록(언제나 01)을 고른다.</summary>
        public void SelectLatest()
        {
            Page = 0;
            Selected = 0;
        }

        /// <summary>목록이 줄었을 수 있다 — 고른 칸을 남은 칸 안으로.</summary>
        public void ClampSelection() => Selected = Math.Min(Selected, CellCount - 1);

        /// <summary>불러오기에 실패한 슬롯은 다음에 다시 판정한다.</summary>
        public void ForgetLoadable(string slotId) => _loadable.Remove(slotId);

        public bool Select(int indexOnPage)
        {
            int cell = Page * PerPage + indexOnPage;
            if (!IsSelectable(cell))
                return false;

            Selected = cell;
            return true;
        }

        // 쪽을 넘기면 선택도 그 쪽의 첫 칸으로 옮긴다 — 보이지 않는 칸에 실행이 걸리지 않게.
        public bool TurnPage(int delta)
        {
            int next = Math.Clamp(Page + delta, 0, PageCount - 1);
            if (next == Page)
                return false;

            Page = next;
            int first = Page * PerPage;
            Selected = FirstSelectable(first, Math.Min(CellCount, first + PerPage));
            return true;
        }

        public SaveSlotsPage.Model ToModel(bool focus)
        {
            int first = Page * PerPage;
            var cards = new SaveSlotCard.Model[PerPage];

            for (int i = 0; i < cards.Length; i++)
                cards[i] = CardFor(first + i);

            SaveSlotsPage.Hint hint =
                !Readable ? SaveSlotsPage.Hint.Unreadable
                : !SaveMode && _entries.Count == 0 ? SaveSlotsPage.Hint.NoSaves
                : SaveSlotsPage.Hint.Ready;

            return new SaveSlotsPage.Model
            {
                Hint = hint,
                Page = Page,
                PageCount = PageCount,
                CanPrevious = Page > 0,
                CanNext = Page < PageCount - 1,
                Cards = cards,
                FocusCard = focus && Selected >= first && Selected < first + cards.Length ? Selected - first : -1,
            };
        }

        private SaveSlotCard.Model CardFor(int cell)
        {
            bool selected = cell == Selected;

            if (IsFilledCell(cell))
            {
                SaveSlotEntry entry = _entries[cell];

                return new SaveSlotCard.Model
                {
                    Filled = true,
                    Number = cell + 1,
                    Title = string.IsNullOrEmpty(entry.Label) ? entry.Preview : entry.Label,
                    SavedAtUtc = ParseUtc(entry.SavedAtUtc),
                    Chapter = entry.ChapterId,
                    PlaySeconds = entry.PlaySeconds,
                    Selectable = IsSelectable(cell),
                    Selected = selected,
                };
            }

            bool isNew = IsNewSlotCell(cell);

            return new SaveSlotCard.Model
            {
                Filled = false,
                IsNewSlot = isNew,
                Selectable = isNew,
                Selected = selected && isNew,
            };
        }

        private int CellCount => _entries.Count + (HasNewSlotCell ? 1 : 0);
        private bool HasNewSlotCell => SaveMode && Readable;
        private int PageCount => Math.Max(1, (CellCount + PerPage - 1) / PerPage);

        private bool IsFilledCell(int cell) => cell >= 0 && cell < _entries.Count;
        private bool IsNewSlotCell(int cell) => HasNewSlotCell && cell == _entries.Count;
        private bool IsOnPage(int cell) => cell >= 0 && cell / PerPage == Page;

        private bool IsSelectable(int cell)
        {
            if (SaveMode)
                return IsFilledCell(cell) || IsNewSlotCell(cell);

            return IsFilledCell(cell) && CanLoad(_entries[cell].Id);
        }

        private int FirstSelectable(int from, int end)
        {
            for (int cell = from; cell < end; cell++)
            {
                if (IsSelectable(cell))
                    return cell;
            }

            return -1;
        }

        // 본문을 실제로 읽어 보는 판정이라 비싸다 — 슬롯마다 한 번만 묻는다.
        private bool CanLoad(string id)
        {
            if (string.IsNullOrEmpty(id) || _canLoad == null)
                return false;

            if (!_loadable.TryGetValue(id, out bool loadable))
            {
                loadable = _canLoad(id);
                _loadable[id] = loadable;
            }

            return loadable;
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
    }
}
