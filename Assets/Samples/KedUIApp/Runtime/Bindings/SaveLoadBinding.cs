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
    internal sealed class SaveLoadBinding : ScreenBinding
    {
        public enum Mode
        {
            Save,
            Load,
        }

        private readonly SaveLoadPanel _panel;
        private readonly List<SaveSlotEntry> _entries = new();
        private readonly Dictionary<string, bool> _loadable = new(StringComparer.Ordinal);

        private Mode _mode;
        private bool _readable;
        private int _page;
        private int _selected = -1;
        private bool _busy;

        public SaveLoadBinding(KedUIFlow flow, KedUIServices services)
            : base(flow, services)
        {
            _panel = Own(flow.Require<SaveLoadPanel>());
        }

        private ManualSaveFlow Saves => Services.Saves;

        public void Open(Mode mode)
        {
            if (Saves == null)
                return;

            _mode = mode;
            _busy = false;
            Reload();

            // 저장은 "새로 저장" 칸을 먼저 고른다 — 열자마자 실행 버튼을 눌러도 기록을 덮어쓰지 않게.
            _selected = mode == Mode.Save && HasNewSlotCell
                ? _entries.Count
                : FirstSelectable();

            _page = _selected >= 0 ? _selected / SaveLoadPanel.SlotsPerPage : 0;

            Flow.PushPanel<SaveLoadPanel>(panel =>
            {
                BindView(panel, ApplyBindings);
                Redraw(focus: true);
            });
        }

        private void ApplyBindings(SaveLoadPanel panel)
        {
            AddBinding(panel,
                p => p.BackClicked += Flow.ClosePanel,
                p => p.BackClicked -= Flow.ClosePanel);

            AddBinding(panel,
                p => p.CloseClicked += Flow.CloseAllPanels,
                p => p.CloseClicked -= Flow.CloseAllPanels);

            AddBinding(panel,
                p => p.PreviousClicked += PreviousPage,
                p => p.PreviousClicked -= PreviousPage);

            AddBinding(panel,
                p => p.NextClicked += NextPage,
                p => p.NextClicked -= NextPage);

            AddBinding(panel,
                p => p.CardClicked += SelectCard,
                p => p.CardClicked -= SelectCard);

            AddBinding(panel,
                p => p.CardDoubleClicked += ActOnCard,
                p => p.CardDoubleClicked -= ActOnCard);

            AddBinding(panel,
                p => p.ActionClicked += ActOnSelected,
                p => p.ActionClicked -= ActOnSelected);
        }

        private void Reload()
        {
            _entries.Clear();
            _loadable.Clear();

            _readable = Saves.TryReadSlots(out IReadOnlyList<SaveSlotEntry> slots);

            if (slots != null)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    if (slots[i] != null)
                        _entries.Add(slots[i]);
                }
            }

            // 최근 저장이 앞(01). 시각을 읽지 못하는 항목은 뒤로.
            _entries.Sort((a, b) => ParseUtc(b.SavedAtUtc).CompareTo(ParseUtc(a.SavedAtUtc)));
        }

        private int CellCount => _entries.Count + (HasNewSlotCell ? 1 : 0);
        private bool HasNewSlotCell => _mode == Mode.Save && _readable;
        private int PageCount => Mathf.Max(1, Mathf.CeilToInt(CellCount / (float)SaveLoadPanel.SlotsPerPage));

        private bool IsFilledCell(int cell) => cell >= 0 && cell < _entries.Count;
        private bool IsNewSlotCell(int cell) => HasNewSlotCell && cell == _entries.Count;

        private bool IsSelectableCell(int cell)
        {
            if (_mode == Mode.Save)
                return IsFilledCell(cell) || IsNewSlotCell(cell);

            return IsFilledCell(cell) && CanLoad(_entries[cell].Id);
        }

        private int FirstSelectable()
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
                loadable = Saves.CanLoad(id);
                _loadable[id] = loadable;
            }

            return loadable;
        }

        private void PreviousPage() => TurnPage(-1);
        private void NextPage() => TurnPage(+1);

        // 쪽을 넘기면 선택도 그 쪽의 첫 칸으로 옮긴다 — 보이지 않는 칸에 실행이 걸리지 않게.
        private void TurnPage(int delta)
        {
            int next = Mathf.Clamp(_page + delta, 0, PageCount - 1);
            if (next == _page)
                return;

            _page = next;
            _selected = FirstSelectableOnPage(_page);
            Redraw(focus: false);
        }

        private int FirstSelectableOnPage(int page)
        {
            int first = page * SaveLoadPanel.SlotsPerPage;
            int end = Mathf.Min(CellCount, first + SaveLoadPanel.SlotsPerPage);

            for (int cell = first; cell < end; cell++)
            {
                if (IsSelectableCell(cell))
                    return cell;
            }

            return -1;
        }

        private bool IsOnCurrentPage(int cell) =>
            cell >= 0 && cell / SaveLoadPanel.SlotsPerPage == _page;

        private void SelectCard(int indexOnPage)
        {
            int cell = _page * SaveLoadPanel.SlotsPerPage + indexOnPage;
            if (!IsSelectableCell(cell))
                return;

            _selected = cell;
            Redraw(focus: false);
        }

        private void ActOnCard(int indexOnPage)
        {
            SelectCard(indexOnPage);
            ActOnSelected();
        }

        private void Redraw(bool focus)
        {
            bool save = _mode == Mode.Save;
            int first = _page * SaveLoadPanel.SlotsPerPage;

            var cards = new SaveSlotCard.Model[SaveLoadPanel.SlotsPerPage];

            for (int i = 0; i < cards.Length; i++)
                cards[i] = CardFor(first + i);

            string hint;
            if (!_readable)
                hint = Text("slots.hint.unreadable");
            else if (!save && _entries.Count == 0)
                hint = Text("slots.hint.none");
            else
                hint = Text(save ? "slots.hint.save" : "slots.hint.load");

            bool actionEnabled = !_busy && IsOnCurrentPage(_selected) && IsSelectableCell(_selected) &&
                                 (!save || Saves.CanSave);

            _panel.Present(new SaveLoadPanel.Model
            {
                Title = Text(save ? "slots.save.title" : "slots.load.title"),
                Subtitle = Text(save ? "slots.save.subtitle" : "slots.load.subtitle"),
                TabLabel = Text("slots.tab.manual"),
                PageInfo = Format("slots.pageInfo", KedUIUtil.TwoDigits(_page + 1), KedUIUtil.TwoDigits(PageCount)),
                PageNumbers = PageNumbers(),
                Hint = hint,
                ActionLabel = Text(save ? "slots.save.action" : "slots.load.action"),
                ActionEnabled = actionEnabled,
                CanPrevious = _page > 0,
                CanNext = _page < PageCount - 1,
                Cards = cards,
                FocusCard = focus && _selected >= first && _selected < first + cards.Length
                    ? _selected - first
                    : -1,
            });
        }

        private SaveSlotCard.Model CardFor(int cell)
        {
            bool selected = cell == _selected;

            if (IsFilledCell(cell))
            {
                SaveSlotEntry entry = _entries[cell];

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
            int start = Mathf.Clamp(_page - window / 2, 0, Mathf.Max(0, count - window));
            int end = Mathf.Min(count, start + window);

            var parts = new List<string>(window);
            for (int p = start; p < end; p++)
            {
                string number = (p + 1).ToString(CultureInfo.InvariantCulture);
                parts.Add(p == _page ? $"<color=#13BDC5>{number}</color>" : number);
            }

            return string.Join("   ", parts);
        }

        private void ActOnSelected()
        {
            if (_busy || !IsOnCurrentPage(_selected) || !IsSelectableCell(_selected))
                return;

            if (_mode == Mode.Save)
            {
                if (!Saves.CanSave)
                    return;

                if (IsNewSlotCell(_selected))
                {
                    SaveInto(null);
                    return;
                }

                SaveSlotEntry entry = _entries[_selected];
                Flow.Confirm(new ConfirmPanel.Request
                {
                    Title = Text("confirm.overwrite.title"),
                    Body = Format("confirm.overwrite.body", KedUIUtil.TwoDigits(_selected + 1)),
                    AcceptLabel = Text("confirm.overwrite.accept"),
                    CancelLabel = Text("common.cancel"),
                    OnAccept = () => SaveInto(entry.Id),
                });
                return;
            }

            SaveSlotEntry target = _entries[_selected];

            // 재생 중이면 지금 장면을 벗어난다는 것을 확인한다. 타이틀에서는 잃을 진행이 없다.
            if (Services.IsRunning)
            {
                Flow.Confirm(new ConfirmPanel.Request
                {
                    Title = Text("confirm.load.title"),
                    Body = Format("confirm.load.body", KedUIUtil.TwoDigits(_selected + 1)),
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
                    ? Saves.TryCreate()
                    : Saves.TryOverwrite(overwriteId);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                saved = false;
            }

            Reload();

            if (saved)
            {
                _page = 0;
                _selected = 0;
                Redraw(focus: true);

                Flow.Notice(
                    Text("notice.saved.title"),
                    Format("notice.saved.body", KedUIUtil.TwoDigits(1)),
                    NoticePanel.Kind.Success);
                return;
            }

            _selected = Mathf.Min(_selected, CellCount - 1);
            Redraw(focus: true);

            Flow.Notice(
                Text("notice.saveFailed.title"),
                Text("notice.saveFailed.body"),
                NoticePanel.Kind.Failure);
        }

        private async void LoadFrom(string slotId)
        {
            if (_busy)
                return;

            _busy = true;
            Redraw(focus: false);

            bool transitionStarted = false;

            try
            {
                Task load = Saves.LoadAsync(slotId, () =>
                {
                    transitionStarted = true;
                    Services.StopFastForward();
                    Flow.CloseAllPanels();
                });

                await load;
            }
            catch (Exception error)
            {
                Debug.LogException(error);
            }
            finally
            {
                _busy = false;
            }

            // 회차 전환까지 가지 못했다(본문을 읽지 못함 등) — 화면은 열린 채로 두고 알린다.
            if (!transitionStarted && Flow.IsTop(_panel))
            {
                _loadable.Remove(slotId);
                Redraw(focus: true);

                Flow.Notice(
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
