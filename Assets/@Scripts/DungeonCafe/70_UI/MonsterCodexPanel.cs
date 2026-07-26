using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

/// <summary>
/// 업무수첩.
///
/// 비밀 해금물이 아니라 업무 기록이다. 통화로 확정된 개체만 실린다.
/// 열람 전용이며 여기서 배정이나 승인을 하지 않는다.
/// </summary>
public sealed class MonsterCodexPanel : UIPanel<MonsterCodexPanel.Refs>
{
    public event Action OnCloseRequested;

    #region Refs
    public enum Refs
    {
        CodexBG_Root,
        CodexBG_Image,

        Codex_Title_Text,
        Codex_Empty_Text,

        CodexList_Root,
        CodexList_Content,

        Detail_Root,
        Detail_Name_Text,
        Detail_Species_Text,
        Detail_Demand_Text,
        Detail_Notes_Text,

        CodexCloseButton,

        CodexEntryPrefab,
    }

    private Image _bgImage;
    private TMP_Text _titleText;
    private TMP_Text _emptyText;
    private RectTransform _content;

    private GameObject _detailRoot;
    private TMP_Text _detailNameText;
    private TMP_Text _detailSpeciesText;
    private TMP_Text _detailDemandText;
    private TMP_Text _detailNotesText;

    private Button _closeButton;

    [SerializeField] private VNOptionItem _codexEntryPrefab;

    private readonly GuesthouseOptionItemList _list = new();
    private readonly List<GuesthouseOptionEntry> _entries = new();
    private readonly List<MonsterProfile> _monsters = new();
    private bool _valid;
    #endregion

    protected override void OnInitialize()
    {
        _bgImage = View.Image(Refs.CodexBG_Image);
        _titleText = View.Text(Refs.Codex_Title_Text);
        _emptyText = View.Text(Refs.Codex_Empty_Text);
        _content = View.Rect(Refs.CodexList_Content);

        RectTransform detailRoot = View.Rect(Refs.Detail_Root);
        _detailRoot = detailRoot != null ? detailRoot.gameObject : null;

        _detailNameText = View.Text(Refs.Detail_Name_Text);
        _detailSpeciesText = View.Text(Refs.Detail_Species_Text);
        _detailDemandText = View.Text(Refs.Detail_Demand_Text);
        _detailNotesText = View.Text(Refs.Detail_Notes_Text);

        _closeButton = View.Button(Refs.CodexCloseButton);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(HandleClose);
            _closeButton.onClick.AddListener(HandleClose);
        }

        // 항목을 눌러도 화면이 닫히지 않는다. 수첩은 골라 보는 화면이다.
        _list.LockOnSubmit = false;
        _list.Configure(_codexEntryPrefab, _content);

        _list.OnSubmitted -= ShowDetail;
        _list.OnSubmitted += ShowDetail;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(HandleClose);

        _list.OnSubmitted -= ShowDetail;
        _list.Clear();
    }

    public void Present(IReadOnlyList<ServiceBookingState> bookings)
    {
        if (!_valid)
            return;

        if (_titleText != null)
            _titleText.text = "업무수첩";

        CollectRevealed(bookings);
        ApplyEntries();

        if (_emptyText != null)
            _emptyText.gameObject.SetActive(_monsters.Count == 0);

        if (_detailRoot != null)
            _detailRoot.SetActive(_monsters.Count > 0);

        if (_monsters.Count > 0)
            ShowDetail(0);
    }

    /// <summary>통화로 확정된 개체만 수첩에 남는다.</summary>
    private void CollectRevealed(IReadOnlyList<ServiceBookingState> bookings)
    {
        _monsters.Clear();

        if (bookings == null)
            return;

        for (int i = 0; i < bookings.Count; i++)
        {
            if (!bookings[i].IsConfirmed)
                continue;

            _monsters.Add(bookings[i].Monster);
        }
    }

    private void ApplyEntries()
    {
        _entries.Clear();

        for (int i = 0; i < _monsters.Count; i++)
            _entries.Add(new GuesthouseOptionEntry(_monsters[i].DisplayName));

        _list.Rebuild(_entries);
    }

    private void ShowDetail(int index)
    {
        if (index < 0 || index >= _monsters.Count)
            return;

        MonsterProfile monster = _monsters[index];

        if (_detailNameText != null)
            _detailNameText.text = monster.DisplayName;

        if (_detailSpeciesText != null)
            _detailSpeciesText.text = $"계열: {ToSpeciesLabel(monster.Species)}";

        if (_detailDemandText != null)
            _detailDemandText.text = $"요구 유형: {BurdenAxes.ToAptitudeLabel(monster.DemandAxis)}";

        if (_detailNotesText != null)
            _detailNotesText.text = BuildNotes(monster);
    }

    private static string BuildNotes(MonsterProfile monster)
    {
        StringBuilder builder = new();

        for (int i = 0; i < monster.CodexNotes.Count; i++)
        {
            if (builder.Length > 0)
                builder.Append('\n');

            builder.Append("· ");
            builder.Append(monster.CodexNotes[i]);
        }

        return builder.ToString();
    }

    private static string ToSpeciesLabel(MonsterSpecies species)
    {
        return species switch
        {
            MonsterSpecies.ParasiticEquipment => "기생 장비종",
            MonsterSpecies.MemoryDevourer => "기억 포식종",
            MonsterSpecies.ResonanceAmplifier => "감응 증폭종",
            MonsterSpecies.PredatoryBinder => "포식·구속종",
            _ => "미분류",
        };
    }

    private void HandleClose() => OnCloseRequested?.Invoke();

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _bgImage, Refs.CodexBG_Image);
        AppendMissing(ref missing, _titleText, Refs.Codex_Title_Text);
        AppendMissing(ref missing, _content, Refs.CodexList_Content);
        AppendMissing(ref missing, _closeButton, Refs.CodexCloseButton);
        AppendMissing(ref missing, _codexEntryPrefab, Refs.CodexEntryPrefab);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[MonsterCodexPanel] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}
