using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

// 밤 시작 상점/장착 패널.
//
// 구매 가능한 능력과 보유 능력을 한 목록에 표시.
// - 구매 가능 항목: 누르면 구매 예약 (비용 표시, 보유 욕구 초과분은 예약 불가)
// - 보유 항목: 누르면 장착 토글 (슬롯 제한 표시. 전용 능력은 슬롯을 차지하지 않는다)
public sealed class NightPrepPanel : UIPanel<NightPrepPanel.Refs>, IManagedUI
{
    public event Action<IReadOnlyList<string>, IReadOnlyList<string>> OnPrepConfirmed;

    #region Refs
    public enum Refs
    {
        PrepBG_Root,
        PrepBG_Image,

        Title_Text,
        Summary_Text,

        PrepList_Root,
        PrepList_Content,
        PrepPrefab,
    }

    private Image _bgImage;
    private TMP_Text _titleText;
    private TMP_Text _summaryText;
    private RectTransform _content;

    [SerializeField] private VNOptionItem _prepPrefab;

    private readonly DungeonCafeOptionItemList _list = new();
    private readonly List<DungeonCafeOptionEntry> _entries = new();

    private enum EntryKind { Confirm, Purchase, Equip }
    private readonly List<(EntryKind kind, string id)> _slots = new();

    private readonly List<string> _purchases = new();
    private readonly List<string> _equipped = new();

    private NightPrepRequest _request;
    private bool _valid;
    private bool _locked;
    #endregion

    protected override void OnInitialize()
    {
        _bgImage = View.Image(Refs.PrepBG_Image);
        _titleText = View.Text(Refs.Title_Text);
        _summaryText = View.Text(Refs.Summary_Text);
        _content = View.Rect(Refs.PrepList_Content);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif

        _list.Configure(_prepPrefab, _content);

        _list.OnSubmitted -= HandleSubmitted;
        _list.OnSubmitted += HandleSubmitted;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        _list.OnSubmitted -= HandleSubmitted;
        _list.Clear();
    }

    public void Present(NightPrepRequest request)
    {
        if (!_valid || request == null)
            return;

        _request = request;
        _locked = false;

        _purchases.Clear();
        _equipped.Clear();
        for (int i = 0; i < request.Equipped.Count; i++)
            _equipped.Add(request.Equipped[i]);

        if (_titleText != null)
            _titleText.text = "밤 - 상점 / 장착";

        Rebuild();
        _list.SetLocked(false);
    }

    private int PendingCost()
    {
        int cost = 0;
        for (int i = 0; i < _purchases.Count; i++)
        {
            PlayerAbilityDefinition def = Find(_purchases[i]);
            if (def != null) cost += def.DesireCost;
        }
        
        return cost;
    }

    private PlayerAbilityDefinition Find(string id)
    {
        for (int i = 0; i < _request.Purchasable.Count; i++)
            if (_request.Purchasable[i].Id == id) return _request.Purchasable[i];
        return null;
    }

    private void Rebuild()
    {
        _entries.Clear();
        _slots.Clear();

        if (!_list.IsReady)
            return;

        int cost = PendingCost();

        if (_summaryText != null)
            _summaryText.text =
                $"보유 욕구 {_request.HeldDesire}  (구매 예약 -{cost})\n" +
                $"장착 슬롯 {_equipped.Count} / {_request.SlotLimit}";

        _entries.Add(new DungeonCafeOptionEntry(" 이대로 확정"));
        _slots.Add((EntryKind.Confirm, null));

        // 구매 가능
        for (int i = 0; i < _request.Purchasable.Count; i++)
        {
            PlayerAbilityDefinition def = _request.Purchasable[i];
            bool reserved = _purchases.Contains(def.Id);
            bool affordable = reserved || _request.HeldDesire - cost >= def.DesireCost;

            _entries.Add(new DungeonCafeOptionEntry(
                $"{(reserved ? "(O)" : "(X)")} 구매: {def.DisplayName}  (욕구 {def.DesireCost})" +
                (reserved ? "  [예약]" : string.Empty),
                isAvailable: affordable && !_locked));
            _slots.Add((EntryKind.Purchase, def.Id));
        }

        // 보유 -> 장착 토글
        for (int i = 0; i < _request.Owned.Count; i++)
        {
            string id = _request.Owned[i];
            bool on = _equipped.Contains(id);
            bool slotFree = on || _equipped.Count < _request.SlotLimit;

            _entries.Add(new DungeonCafeOptionEntry(
                $"{(on ? "(O)" : "(X)")} 장착: {id}",
                isAvailable: slotFree && !_locked));
            _slots.Add((EntryKind.Equip, id));
        }

        _list.Rebuild(_entries);
    }

    private void HandleSubmitted(int index)
    {
        if (_locked || index < 0 || index >= _slots.Count)
            return;

        (EntryKind kind, string id) = _slots[index];

        switch (kind)
        {
            case EntryKind.Confirm:
                _locked = true;
                _list.SetLocked(true);
                OnPrepConfirmed?.Invoke(new List<string>(_purchases), new List<string>(_equipped));
                return;

            case EntryKind.Purchase:
                if (!_purchases.Remove(id))
                    _purchases.Add(id);
                break;

            case EntryKind.Equip:
                if (!_equipped.Remove(id))
                    _equipped.Add(id);
                break;
        }

        Rebuild();
    }

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _bgImage, Refs.PrepBG_Image);
        AppendMissing(ref missing, _titleText, Refs.Title_Text);
        AppendMissing(ref missing, _content, Refs.PrepList_Content);
        AppendMissing(ref missing, _prepPrefab, Refs.PrepPrefab);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[NightPrepPanel] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}