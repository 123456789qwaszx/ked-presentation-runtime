using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

/// <summary>
/// 밤 처리 선택 패널.
///
/// 후보는 (메이드 × 축 × 처리 방식) 조합을 평탄화해서 보여준다.
/// 회복은 항상 가능하고, 관리 붕괴는 해당 축의 붕괴도가 기준 이상일 때만 목록에 오른다.
/// 하루에 한 건만 적용되므로 목록에서 하나를 고르면 그대로 확정된다.
/// </summary>
public sealed class NightProgramPanel : UIPanel<NightProgramPanel.Refs>, IManagedUI
{
    public event Action<NightPlan> OnPlanSelected;

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

    [SerializeField] private ChoiceBoxView _planPrefab;

    private readonly List<ChoiceBoxView> _spawned = new();
    private readonly List<NightPlan> _plans = new();
    private bool _valid;
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

        if (_planPrefab != null)
            _planPrefab.gameObject.SetActive(false);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        ClearPlans();
    }

    public void Present(NightPlanRequest request)
    {
        if (!_valid || request == null)
            return;

        if (_titleText != null)
            _titleText.text = $"{request.DayNumber}일차 야간 처리";

        if (_summaryText != null)
            _summaryText.text = "오늘 한 명에게만 개입할 수 있습니다.";

        BuildPlans(request);
    }

    private void BuildPlans(NightPlanRequest request)
    {
        ClearPlans();

        if (_planPrefab == null || _content == null)
            return;

        for (int i = 0; i < request.Maids.Count; i++)
        {
            MaidRuntimeState maid = request.Maids[i];

            if (maid.IsLost)
                continue;

            for (int a = 0; a < BurdenAxes.Count; a++)
            {
                BurdenAxis axis = BurdenAxes.FromIndex(a);

                if (maid.Burden.Get(axis) <= 0)
                    continue;

                AddPlan(new NightPlan(NightProgramKind.Care, maid.MaidId, axis), maid, request);

                if (request.CanRunManagedRelease(maid, axis))
                    AddPlan(new NightPlan(NightProgramKind.ManagedRelease, maid.MaidId, axis), maid, request);
            }
        }
    }

    private void AddPlan(NightPlan plan, MaidRuntimeState maid, NightPlanRequest request)
    {
        ChoiceBoxView view = UnityEngine.Object.Instantiate(_planPrefab, _content);
        view.gameObject.SetActive(true);
        view.Present(index: _plans.Count, label: BuildLabel(plan, maid, request));

        view.OnClicked -= HandlePlanClicked;
        view.OnClicked += HandlePlanClicked;

        _spawned.Add(view);
        _plans.Add(plan);
    }

    private static string BuildLabel(NightPlan plan, MaidRuntimeState maid, NightPlanRequest request)
    {
        int current = maid.Burden.Get(plan.Axis);
        string axisLabel = BurdenAxes.ToBurdenLabel(plan.Axis);

        if (plan.Kind == NightProgramKind.Care)
        {
            int after = Math.Max(0, current - request.Tuning.CareReduction);
            return $"{maid.DisplayName} · {axisLabel} 회복\n{current} → {after}";
        }

        int released = current * request.Tuning.ManagedReleaseRetainPercent / 100;

        return $"{maid.DisplayName} · {axisLabel} 관리 붕괴\n" +
               $"{current} → {released}   숙련 +{request.Tuning.ManagedReleaseExperience}";
    }

    private void HandlePlanClicked(int index)
    {
        if (index < 0 || index >= _plans.Count)
            return;

        OnPlanSelected?.Invoke(_plans[index]);
    }

    private void ClearPlans()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            ChoiceBoxView view = _spawned[i];

            if (view == null)
                continue;

            view.OnClicked -= HandlePlanClicked;
            UnityEngine.Object.Destroy(view.gameObject);
        }

        _spawned.Clear();
        _plans.Clear();
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
