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

    private readonly DungeonCafeOptionItemList _list = new();
    private readonly List<DungeonCafeOptionEntry> _entries = new();
    private readonly List<MonsterProfile> _monsters = new();
    private CampaignState _campaign;
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

    public void Present(CampaignState campaign)
    {
        if (!_valid || campaign == null)
            return;

        _campaign = campaign;

        if (_titleText != null)
            _titleText.text = "업무수첩";

        CollectRevealed(campaign);
        ApplyEntries();

        if (_emptyText != null)
            _emptyText.gameObject.SetActive(_monsters.Count == 0);

        if (_detailRoot != null)
            _detailRoot.SetActive(_monsters.Count > 0);

        if (_monsters.Count > 0)
            ShowDetail(0);
    }

    /// <summary>통화로 확정된 개체(일부 파악 이상)만 수첩에 남는다. (§8.2)</summary>
    private void CollectRevealed(CampaignState campaign)
    {
        _monsters.Clear();

        IReadOnlyList<MonsterProfile> all = campaign.Content.Monsters;
        for (int i = 0; i < all.Count; i++)
        {
            UnderstandingTier tier = campaign.Understanding.GetTier(all[i].MonsterId, campaign.Tuning);
            if (tier >= UnderstandingTier.Partial)
                _monsters.Add(all[i]);
        }
    }

    private void ApplyEntries()
    {
        _entries.Clear();

        for (int i = 0; i < _monsters.Count; i++)
        {
            UnderstandingTier tier = _campaign.Understanding.GetTier(_monsters[i].MonsterId, _campaign.Tuning);
            _entries.Add(new DungeonCafeOptionEntry($"{_monsters[i].DisplayName}  ({ToTierLabel(tier)})"));
        }

        _list.Rebuild(_entries);
    }

    private void ShowDetail(int index)
    {
        if (index < 0 || index >= _monsters.Count)
            return;

        MonsterProfile monster = _monsters[index];
        UnderstandingTier tier = _campaign.Understanding.GetTier(monster.MonsterId, _campaign.Tuning);

        if (_detailNameText != null)
            _detailNameText.text = monster.DisplayName;

        if (_detailSpeciesText != null)
            _detailSpeciesText.text = $"계열: {ToSpeciesLabel(monster.Species)}";

        if (_detailDemandText != null)
            _detailDemandText.text = $"요구 유형: {BurdenAxes.ToAptitudeLabel(monster.DemandAxis)}";

        if (_detailNotesText != null)
            _detailNotesText.text = BuildNotes(monster, tier);
    }

    /// <summary>이해도 4단계 공개. (§8.2: 일부=요구/만족 / 고도=범위/특이 / 완전=보정/심층)</summary>
    private string BuildNotes(MonsterProfile monster, UnderstandingTier tier)
    {
        StringBuilder builder = new();

        Append(builder, monster.ReservationPostText);
        Append(builder, $"요구 만족도 {monster.RequiredSatisfaction}");

        if (tier >= UnderstandingTier.Advanced)
        {
            Append(builder, "부하 범위 공개: 승인 화면에서 옵션별 범위가 보입니다");
            if (monster.SpecialRule != MonsterSpecialRule.None)
                Append(builder, $"특이 규칙: {ToSpecialRuleLabel(monster.SpecialRule)}");
        }

        if (tier >= UnderstandingTier.Complete)
        {
            string mod = monster.LoadModifier == 0
                ? "부하 보정 없음"
                : $"부하 보정 {monster.LoadModifier:+0;-0}{(monster.LoadModifierHeavyOnly ? " (강 옵션 한정)" : string.Empty)}";
            Append(builder, mod);
            Append(builder, "완전 파악: 심층 주사위 -5 적용 중");
        }

        return builder.ToString();
    }

    private static void Append(StringBuilder builder, string line)
    {
        if (string.IsNullOrEmpty(line))
            return;

        if (builder.Length > 0)
            builder.Append('\n');

        builder.Append("/ ").Append(line);
    }

    private static string ToTierLabel(UnderstandingTier tier) => tier switch
    {
        UnderstandingTier.Complete => "완전 파악",
        UnderstandingTier.Advanced => "고도 파악",
        UnderstandingTier.Partial => "일부 파악",
        _ => "미확인",
    };

    private static string ToSpecialRuleLabel(MonsterSpecialRule rule) => rule switch
    {
        MonsterSpecialRule.HeavyReactionEcho => "중 옵션의 반응이 강 등급으로 산정",
        MonsterSpecialRule.TighteningGrip => "3비트째 부하 보정 +4",
        MonsterSpecialRule.RepetitionBoredom => "같은 강도 연속 승인 시 반응 하향",
        MonsterSpecialRule.AxisMasquerade => "요구축 위장 (고도 파악 시 해제)",
        MonsterSpecialRule.Reverb => "적용 부하의 20%를 감응으로 반향",
        MonsterSpecialRule.DangerCraving => "붕괴 80~99에서 반응 상향",
        MonsterSpecialRule.Overstay => "종료 시 붕괴 ≥80이면 추가 비트 강제",
        MonsterSpecialRule.OverstayVeil => "추가 비트 강제 + 심층 첫 회수 무효",
        _ => rule.ToString(),
    };

    private static string ToSpeciesLabel(MonsterSpecies species)
    {
        return species switch
        {
            MonsterSpecies.ParasiticEquipment => "기생 장비종",
            MonsterSpecies.MemoryDevourer => "기억 포식종",
            MonsterSpecies.ResonanceAmplifier => "감응 증폭종",
            MonsterSpecies.PredatoryBinder => "포식/구속종",
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
