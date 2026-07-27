using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

/// <summary>
/// 담당 메이드 배정 패널.
/// 대응력 3종과 현재 누적 부담 3종을 나란히 보여주고, 예약 확정 이후에만 요구 타입을 노출한다.
/// </summary>
public sealed class MaidAssignmentPanel : UIPanel<MaidAssignmentPanel.Refs>, IManagedUI
{
    public event Action<string> OnMaidSelected;

    #region Refs
    public enum Refs
    {
        AssignBG_Root,
        AssignBG_Image,

        Monster_Root,
        Monster_Name_Text,
        Monster_Species_Text,
        Monster_Demand_Text,

        MaidList_Root,
        MaidList_Content,
        MaidCardPrefab,
    }

    private Image _bgImage;
    private TMP_Text _monsterNameText;
    private TMP_Text _monsterSpeciesText;
    private TMP_Text _monsterDemandText;

    private RectTransform _content;

    [SerializeField] private VNOptionItem _maidCardPrefab;

    private readonly GuesthouseOptionItemList _list = new();
    private readonly List<GuesthouseOptionEntry> _entries = new();
    private readonly List<string> _maidIds = new();
    private bool _valid;
    #endregion

    protected override void OnInitialize()
    {
        _bgImage = View.Image(Refs.AssignBG_Image);

        _monsterNameText = View.Text(Refs.Monster_Name_Text);
        _monsterSpeciesText = View.Text(Refs.Monster_Species_Text);
        _monsterDemandText = View.Text(Refs.Monster_Demand_Text);

        _content = View.Rect(Refs.MaidList_Content);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif

        _list.Configure(_maidCardPrefab, _content);

        _list.OnSubmitted -= HandleCardSubmitted;
        _list.OnSubmitted += HandleCardSubmitted;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        _list.OnSubmitted -= HandleCardSubmitted;
        _list.Clear();
    }

    public void Present(MonsterProfileV3 monster, IReadOnlyList<MaidStateV3> candidates, CampaignStateV3 campaign)
    {
        if (!_valid || monster == null || candidates == null)
            return;

        ApplyMonster(monster, campaign);
        ApplyCandidates(candidates, campaign);
    }

    private void ApplyMonster(MonsterProfileV3 monster, CampaignStateV3 campaign)
    {
        UnderstandingTier tier = campaign.Understanding.GetTier(monster.MonsterId, campaign.Tuning);

        if (_monsterNameText != null)
            _monsterNameText.text = tier >= UnderstandingTier.Partial ? monster.DisplayName : "미확인 손님";

        if (_monsterSpeciesText != null)
            _monsterSpeciesText.text = tier >= UnderstandingTier.Partial
                ? campaign.Content.GetProtocol(monster.Species)?.DisplayName ?? monster.Species.ToString()
                : "계열: 미확인";

        if (_monsterDemandText == null)
            return;

        // 요구 유형은 통화 확정(일부 파악) 이후에 공개된다. (§8.2)
        _monsterDemandText.text = tier >= UnderstandingTier.Partial
            ? $"요구 유형: {BurdenAxes.ToAptitudeLabel(monster.DemandAxis)}"
            : "요구 유형: 미확인";
    }

    private void ApplyCandidates(IReadOnlyList<MaidStateV3> candidates, CampaignStateV3 campaign)
    {
        _entries.Clear();
        _maidIds.Clear();

        for (int i = 0; i < candidates.Count; i++)
        {
            MaidStateV3 maid = candidates[i];

            // 이탈/배정 차단 인원은 목록에 남기되 고를 수 없게 한다. 사라지면 왜 없는지 알 수 없다.
            _entries.Add(new GuesthouseOptionEntry(
                BuildLabel(maid),
                isAvailable: maid.CanBeAssigned(campaign.CurrentDayNumber)));

            _maidIds.Add(maid.MaidId);
        }

        _list.Rebuild(_entries);
    }

    private static string BuildLabel(MaidStateV3 maid)
    {
        AxisTriple aptitude = maid.Aptitude;
        AxisTriple gauge = maid.Gauge.Snapshot();

        return $"{maid.DisplayName}\n" +
               $"육체 {aptitude.Physical} / 정신 {aptitude.Mental} / 감응 {aptitude.Empathic}\n" +
               $"상처 {gauge.Physical}  스트레스 {gauge.Mental}  충동 {gauge.Empathic}   (0~200)";
    }

    private void HandleCardSubmitted(int index)
    {
        if (index < 0 || index >= _maidIds.Count)
            return;

        OnMaidSelected?.Invoke(_maidIds[index]);
    }

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _bgImage, Refs.AssignBG_Image);
        AppendMissing(ref missing, _monsterNameText, Refs.Monster_Name_Text);
        AppendMissing(ref missing, _content, Refs.MaidList_Content);
        AppendMissing(ref missing, _maidCardPrefab, Refs.MaidCardPrefab);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[MaidAssignmentPanel] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}
