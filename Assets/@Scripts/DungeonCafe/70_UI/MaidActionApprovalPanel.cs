using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

/// <summary>
/// 행동 승인 패널. (v3 §2)
/// - 메이드가 제안한 3옵션(약/중/강)을 나열한다
/// - 이해도에 따라 부하 범위를 옵션별로 표시한다 (§2.5: 고도=범위, 완전=개체 보정 포함)
/// - 사용 가능한 낮 능력을 토글로 나열하고, 옵션 승인 시 함께 제출한다 (§11)
/// - 통제 신호가 거부된 뒤에는 입력을 막는다
///
/// 후보 목록은 VNOptionItem 으로 그린다. Yarn 선택지와 조작감이 같아야 하기 때문이다.
/// </summary>
public sealed class MaidActionApprovalPanel : UIPanel<MaidActionApprovalPanel.Refs>, IManagedUI
{
    /// <summary>(옵션 인덱스, 함께 사용할 능력 id 목록).</summary>
    public event Action<int, IReadOnlyList<string>> OnOptionApproved;

    #region Refs
    public enum Refs
    {
        ApprovalBG_Root,
        ApprovalBG_Image,

        Header_Root,
        Header_MaidName_Text,
        Header_ControlStatus_Text,

        Burden_Root,
        Burden_Physical_Text,
        Burden_Mental_Text,
        Burden_Empathic_Text,

        OptionList_Root,
        OptionList_Content,

        OptionPrefab,
    }

    private Image _bgImage;
    private TMP_Text _maidNameText;
    private TMP_Text _controlStatusText;

    private TMP_Text _physicalText;
    private TMP_Text _mentalText;
    private TMP_Text _empathicText;

    private RectTransform _content;

    [SerializeField] private VNOptionItem _optionPrefab;

    private readonly GuesthouseOptionItemList _list = new();
    private readonly List<GuesthouseOptionEntry> _entries = new();

    private ApprovalRequestV3 _request;
    private readonly List<string> _abilityIds = new();          // 목록 뒤에 붙는 능력 항목의 id
    private readonly HashSet<string> _toggledAbilities = new(StringComparer.Ordinal);

    private bool _valid;
    private bool _locked;
    #endregion

    protected override void OnInitialize()
    {
        _bgImage = View.Image(Refs.ApprovalBG_Image);

        _maidNameText = View.Text(Refs.Header_MaidName_Text);
        _controlStatusText = View.Text(Refs.Header_ControlStatus_Text);

        _physicalText = View.Text(Refs.Burden_Physical_Text);
        _mentalText = View.Text(Refs.Burden_Mental_Text);
        _empathicText = View.Text(Refs.Burden_Empathic_Text);

        _content = View.Rect(Refs.OptionList_Content);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif

        _list.Configure(_optionPrefab, _content);

        _list.OnSubmitted -= HandleOptionSubmitted;
        _list.OnSubmitted += HandleOptionSubmitted;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        _list.OnSubmitted -= HandleOptionSubmitted;
        _list.Clear();
    }

    public void Present(ApprovalRequestV3 request)
    {
        if (!_valid || request == null)
            return;

        _request = request;
        _locked = false;
        _toggledAbilities.Clear();

        ApplyHeader(request);
        ApplyGauge(request.Session.Maid);
        RebuildEntries();

        _list.SetLocked(false);
    }

    /// <summary>통제 상실 이후에는 남아 있는 목록을 그대로 두되 입력만 차단한다.</summary>
    public void LockForControlLoss()
    {
        _locked = true;
        _list.SetLocked(true);

        if (_controlStatusText != null)
            _controlStatusText.text = "관리자 통제 신호가 거부되었습니다";
    }

    private void ApplyHeader(ApprovalRequestV3 request)
    {
        MaidStateV3 maid = request.Session.Maid;

        if (_maidNameText != null)
            _maidNameText.text = $"{maid.DisplayName}/ {request.BeatIndex + 1}비트";

        if (_controlStatusText == null)
            return;

        // 절벽 예고: 요구축 붕괴 구간이 곧 통제 상태다. (0~200)
        int v = maid.Gauge.Get(request.Session.Monster.DemandAxis);
        _controlStatusText.text =
            v >= 100 ? "관리자 통제 신호가 거부되었습니다" :
            v >= 80 ? "위험 착지 구간 - 결산 x3.0, 한 발 더 나가면 심층" :
            "행동 승인권 위임 중";
    }

    private void ApplyGauge(MaidStateV3 maid)
    {
        SetGaugeText(_physicalText, maid, BurdenAxis.Physical);
        SetGaugeText(_mentalText, maid, BurdenAxis.Mental);
        SetGaugeText(_empathicText, maid, BurdenAxis.Empathic);
    }

    private static void SetGaugeText(TMP_Text target, MaidStateV3 maid, BurdenAxis axis)
    {
        if (target == null)
            return;

        target.text = $"{BurdenAxes.ToBurdenLabel(axis)} {maid.Gauge.Get(axis)} / 200";
    }

    private void RebuildEntries()
    {
        _entries.Clear();
        _abilityIds.Clear();

        // 옵션 3개 - 라벨에 강도/축/(이해도만큼의) 범위를 싣는다. 위험을 감출지는 시스템이,
        // 감수할지는 관리자가 정한다. 선택 불가로 만들지 않는다.
        for (int i = 0; i < _request.Options.Count; i++)
            _entries.Add(new GuesthouseOptionEntry(BuildOptionLabel(_request.Options[i])));

        // 낮 능력 토글 - 승인과 같은 목록에 산다. 누르면 켜지고 다시 누르면 꺼진다.
        for (int i = 0; i < _request.AvailableAbilityIds.Count; i++)
        {
            string id = _request.AvailableAbilityIds[i];
            _abilityIds.Add(id);
            _entries.Add(new GuesthouseOptionEntry(BuildAbilityLabel(id)));
        }

        _list.Rebuild(_entries);
    }

    private string BuildOptionLabel(in OptionDisplayV3 option)
    {
        string intensity = option.Intensity switch
        {
            OptionIntensity.Heavy => "강",
            OptionIntensity.Medium => "중",
            _ => "약",
        };

        string range = option.ShowsRange
            ? $"  부하 {option.RangeMin}~{option.RangeMax}"
            : "  부하 ???";

        string upgraded = option.UpgradedReaction ? "\n[반응 상향 예고]" : string.Empty;

        return $"[{intensity}] {BurdenAxes.ToBurdenLabel(option.DisplayAxis)} 부하{range}{upgraded}";
    }

    private string BuildAbilityLabel(string abilityId)
    {
        bool on = _toggledAbilities.Contains(abilityId);
        return $"{(on ? "(O)" : "(X)")} 능력: {abilityId}{(on ? "  (사용 예약)" : string.Empty)}";
    }

    private void HandleOptionSubmitted(int index)
    {
        if (_locked || _request == null)
            return;

        int optionCount = _request.Options.Count;

        // 능력 항목: 토글 후 목록만 다시 그린다.
        if (index >= optionCount)
        {
            int abilityIndex = index - optionCount;
            if (abilityIndex < 0 || abilityIndex >= _abilityIds.Count)
                return;

            string id = _abilityIds[abilityIndex];
            if (!_toggledAbilities.Add(id))
                _toggledAbilities.Remove(id);

            RebuildEntries();
            return;
        }

        _locked = true;
        OnOptionApproved?.Invoke(index, new List<string>(_toggledAbilities));
    }

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _bgImage, Refs.ApprovalBG_Image);
        AppendMissing(ref missing, _maidNameText, Refs.Header_MaidName_Text);
        AppendMissing(ref missing, _controlStatusText, Refs.Header_ControlStatus_Text);
        AppendMissing(ref missing, _content, Refs.OptionList_Content);
        AppendMissing(ref missing, _optionPrefab, Refs.OptionPrefab);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[MaidActionApprovalPanel] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}
