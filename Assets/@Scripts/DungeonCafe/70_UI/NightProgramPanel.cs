using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

/// <summary>
/// 밤 처리 선택 패널. (v3 §5~§6)
///
/// manageCount 명까지 (메이드, 안정/관리 붕괴) 를 지정하고, 나머지는 방치된다.
/// 후보는 (메이드 x 처리 방식) 을 평탄화해 보여준다.
/// - 안정: 항상 가능
/// - 관리 붕괴: 최고 축 붕괴 80~99 일 때만 목록에 오른다 (§6.1)
/// 항목을 누르면 계획에 담기고, [확정] 을 누르면 그대로 제출된다.
/// 정원을 채우면 자동 확정한다. 아무도 고르지 않고 확정하면 전원 방치다.
/// </summary>
public sealed class NightProgramPanel : UIPanel<NightProgramPanel.Refs>, IManagedUI
{
    public event Action<IReadOnlyList<NightChoiceV3>> OnPlanConfirmed;

    #region Refs
    public enum Refs
    {
        NightBG_Root,
        NightBG_Image,

        Title_Text,
        Summary_Text,

        PlanList_Root,
        PlanList_Content,
        PlanPrefab,
    }

    private Image _bgImage;
    private TMP_Text _titleText;
    private TMP_Text _summaryText;
    private RectTransform _content;

    [SerializeField] private VNOptionItem _planPrefab;

    private readonly GuesthouseOptionItemList _list = new();
    private readonly List<GuesthouseOptionEntry> _entries = new();
    private readonly List<NightChoiceV3> _candidates = new();   // 목록 항목 -> 선택지 (확정 항목은 Kind=None)
    private readonly List<NightChoiceV3> _plan = new();

    private NightPlanRequestV3 _request;
    private bool _valid;
    private bool _locked;
    #endregion

    protected override void OnInitialize()
    {
        _bgImage = View.Image(Refs.NightBG_Image);
        _titleText = View.Text(Refs.Title_Text);
        _summaryText = View.Text(Refs.Summary_Text);
        _content = View.Rect(Refs.PlanList_Content);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif

        _list.Configure(_planPrefab, _content);

        _list.OnSubmitted -= HandleSubmitted;
        _list.OnSubmitted += HandleSubmitted;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        _list.OnSubmitted -= HandleSubmitted;
        _list.Clear();
    }

    public void Present(NightPlanRequestV3 request)
    {
        if (!_valid || request == null)
            return;

        _request = request;
        _plan.Clear();
        _locked = false;

        if (_titleText != null)
            _titleText.text = $"{request.DayNumber}일차 야간 처리";

        Rebuild();
        _list.SetLocked(false);
    }

    private void Rebuild()
    {
        _entries.Clear();
        _candidates.Clear();

        if (!_list.IsReady)
            return;

        if (_summaryText != null)
        {
            string requests = BuildQuirkRequestLine();
            _summaryText.text =
                $"오늘 {_request.ManageCount}명까지 개입할 수 있습니다. ({_plan.Count}/{_request.ManageCount})\n" +
                "선택하지 않은 인원은 방치 판정을 받습니다." +
                (requests.Length > 0 ? $"\n{requests}" : string.Empty);
        }

        // 확정 항목이 맨 앞. 죄책감은 목록 아래(방치될 이름들)가 만든다.
        _entries.Add(new GuesthouseOptionEntry(
            _plan.Count == 0 ? "▶ 전원 방치하고 밤을 넘긴다" : "▶ 이대로 확정 (나머지 방치)"));
        _candidates.Add(new NightChoiceV3(null, NightChoiceKind.None));

        for (int i = 0; i < _request.Maids.Count; i++)
        {
            MaidStateV3 maid = _request.Maids[i];
            if (maid.IsLost)
                continue;

            bool alreadyPlanned = IsPlanned(maid.MaidId);
            maid.Gauge.HighestAxis(out int highest);

            AddCandidate(
                new NightChoiceV3(maid.MaidId, NightChoiceKind.Care),
                BuildCareLabel(maid, highest, alreadyPlanned),
                available: !alreadyPlanned && !_locked);

            if (_request.CanRelease(maid))
                AddCandidate(
                    new NightChoiceV3(maid.MaidId, NightChoiceKind.ManagedRelease),
                    BuildReleaseLabel(maid, highest, alreadyPlanned),
                    available: !alreadyPlanned && !_locked);
        }

        _list.Rebuild(_entries);
    }

    private string BuildQuirkRequestLine()
    {
        if (_request.QuirkRequests == null || _request.QuirkRequests.Count == 0)
            return string.Empty;

        var names = new List<string>(_request.QuirkRequests.Count);
        for (int i = 0; i < _request.QuirkRequests.Count; i++)
            names.Add(_request.QuirkRequests[i].maidId);

        return $"※ 먼저 요구하는 이벤트 예약: {string.Join(", ", names)}";
    }

    private bool IsPlanned(string maidId)
    {
        for (int i = 0; i < _plan.Count; i++)
            if (_plan[i].MaidId == maidId) return true;
        return false;
    }

    private void AddCandidate(in NightChoiceV3 choice, string label, bool available)
    {
        _entries.Add(new GuesthouseOptionEntry(label, isAvailable: available));
        _candidates.Add(choice);
    }

    private string BuildCareLabel(MaidStateV3 maid, int highest, bool planned)
    {
        BurdenAxis axis = maid.Gauge.HighestAxis(out _);
        int reduction = _request.Tuning.NightCareReduction;    // 가게 Lv7 상향분은 플로우가 실제 적용
        int after = Math.Max(0, highest - reduction);

        string aftereffect = maid.HasAftereffect ? "  후유증 1단계 해제" : string.Empty;
        string prefix = planned ? "✓ " : string.Empty;

        return $"{prefix}{maid.DisplayName} / 안정\n" +
               $"{BurdenAxes.ToBurdenLabel(axis)} {highest} -> 약 {after}{aftereffect}  관계(신뢰)+";
    }

    private string BuildReleaseLabel(MaidStateV3 maid, int highest, bool planned)
    {
        BurdenAxis axis = maid.Gauge.HighestAxis(out _);
        int retained = highest * _request.Tuning.ManagedReleaseRetainPercent / 100;
        string prefix = planned ? "✓ " : string.Empty;

        return $"{prefix}{maid.DisplayName} / 관리 붕괴 (80~99 한정)\n" +
               $"{BurdenAxes.ToBurdenLabel(axis)} {highest} -> 약 {retained}   " +
               $"밤 수입 +{_request.Tuning.ManagedReleaseNightEnergy}  관계(의존)+";
    }

    private void HandleSubmitted(int index)
    {
        if (_locked || index < 0 || index >= _candidates.Count)
            return;

        NightChoiceV3 choice = _candidates[index];

        // 확정
        if (choice.Kind == NightChoiceKind.None)
        {
            Confirm();
            return;
        }

        if (IsPlanned(choice.MaidId) || _plan.Count >= _request.ManageCount)
            return;

        _plan.Add(choice);

        if (_plan.Count >= _request.ManageCount)
        {
            Confirm();
            return;
        }

        Rebuild();
    }

    private void Confirm()
    {
        if (_locked)
            return;

        _locked = true;
        _list.SetLocked(true);
        OnPlanConfirmed?.Invoke(new List<NightChoiceV3>(_plan));
    }

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _bgImage, Refs.NightBG_Image);
        AppendMissing(ref missing, _titleText, Refs.Title_Text);
        AppendMissing(ref missing, _content, Refs.PlanList_Content);
        AppendMissing(ref missing, _planPrefab, Refs.PlanPrefab);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[NightProgramPanel] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}
