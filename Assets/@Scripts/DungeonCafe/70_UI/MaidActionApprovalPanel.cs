using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

/// <summary>
/// 행동 승인 패널.
/// - 메이드가 제안한 행동 후보를 나열한다
/// - 대응력 초과/한계 돌파 가능성을 후보별로 표시한다
/// - 통제 신호가 거부된 뒤에는 입력을 막는다
/// </summary>
public sealed class MaidActionApprovalPanel : UIPanel<MaidActionApprovalPanel.Refs>, IManagedUI
{
    public event Action<int> OnOptionApproved;

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

    [SerializeField] private ChoiceBoxView _optionPrefab;

    private readonly List<ChoiceBoxView> _spawned = new();
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

        if (_optionPrefab != null)
            _optionPrefab.gameObject.SetActive(false);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        ClearOptions();
    }

    public void Present(ServiceApprovalRequest request)
    {
        if (!_valid || request == null)
            return;

        _locked = request.ControlStatus == ControlAuthorityStatus.Lost;

        ApplyHeader(request);
        ApplyBurden(request.Maid);
        ApplyOptions(request);
    }

    /// <summary>통제 상실 이후에는 남아 있는 목록을 그대로 두되 입력만 차단한다.</summary>
    public void LockForControlLoss()
    {
        _locked = true;

        if (_controlStatusText != null)
            _controlStatusText.text = "관리자 통제 신호가 거부되었습니다";
    }

    private void ApplyHeader(ServiceApprovalRequest request)
    {
        if (_maidNameText != null)
            _maidNameText.text = request.Maid.DisplayName;

        if (_controlStatusText == null)
            return;

        _controlStatusText.text = request.ControlStatus switch
        {
            ControlAuthorityStatus.Strained => "경고: 붕괴 한계 근접",
            ControlAuthorityStatus.Lost => "관리자 통제 신호가 거부되었습니다",
            _ => "행동 승인권 위임 중",
        };
    }

    private void ApplyBurden(MaidRuntimeState maid)
    {
        SetBurdenText(_physicalText, maid, BurdenAxis.Physical);
        SetBurdenText(_mentalText, maid, BurdenAxis.Mental);
        SetBurdenText(_empathicText, maid, BurdenAxis.Empathic);
    }

    private static void SetBurdenText(TMP_Text target, MaidRuntimeState maid, BurdenAxis axis)
    {
        if (target == null)
            return;

        target.text = $"{BurdenAxes.ToBurdenLabel(axis)} {maid.Burden.Get(axis)} / {maid.Burden.GetLimit(axis)}";
    }

    private void ApplyOptions(ServiceApprovalRequest request)
    {
        ClearOptions();

        if (_optionPrefab == null || _content == null)
            return;

        for (int i = 0; i < request.Options.Count; i++)
        {
            ChoiceBoxView view = UnityEngine.Object.Instantiate(_optionPrefab, _content);
            view.gameObject.SetActive(true);
            view.Present(index: i, label: BuildLabel(request, i));

            view.OnClicked -= HandleOptionClicked;
            view.OnClicked += HandleOptionClicked;

            _spawned.Add(view);
        }
    }

    private static string BuildLabel(ServiceApprovalRequest request, int index)
    {
        ServiceActionOption option = request.Options[index];

        string suffix = string.Empty;

        if (request.WouldBreachLimit(index))
            suffix = "  [한계 초과 위험]";
        else if (request.IsBeyondAptitude(index))
            suffix = "  [대응력 부족]";

        return $"{option.ProposalText}{suffix}";
    }

    private void HandleOptionClicked(int index)
    {
        if (_locked)
            return;

        _locked = true;
        OnOptionApproved?.Invoke(index);
    }

    private void ClearOptions()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            ChoiceBoxView view = _spawned[i];

            if (view == null)
                continue;

            view.OnClicked -= HandleOptionClicked;
            UnityEngine.Object.Destroy(view.gameObject);
        }

        _spawned.Clear();
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
