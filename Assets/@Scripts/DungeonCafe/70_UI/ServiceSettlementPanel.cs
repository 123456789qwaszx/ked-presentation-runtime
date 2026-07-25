using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

/// <summary>
/// 접객 결산 패널.
/// 반응 점수 합계와 붕괴 배율을 분리해 보여주고, 곱셈 결과를 에너지로 제시한다.
/// </summary>
public sealed class ServiceSettlementPanel : UIPanel<ServiceSettlementPanel.Refs>, IManagedUI
{
    public event Action OnConfirmed;

    #region Refs
    public enum Refs
    {
        SettleBG_Root,
        SettleBG_Image,

        Title_Text,
        Reaction_Text,
        Multiplier_Text,
        Energy_Text,
        Satisfaction_Text,
        Mastery_Text,
        Incident_Text,

        ConfirmButton,
    }

    private Image _bgImage;
    private TMP_Text _titleText;
    private TMP_Text _reactionText;
    private TMP_Text _multiplierText;
    private TMP_Text _energyText;
    private TMP_Text _satisfactionText;
    private TMP_Text _masteryText;
    private TMP_Text _incidentText;
    private Button _confirmButton;

    private readonly StringBuilder _builder = new();
    private bool _valid;
    #endregion

    protected override void OnInitialize()
    {
        _bgImage = View.Image(Refs.SettleBG_Image);
        _titleText = View.Text(Refs.Title_Text);
        _reactionText = View.Text(Refs.Reaction_Text);
        _multiplierText = View.Text(Refs.Multiplier_Text);
        _energyText = View.Text(Refs.Energy_Text);
        _satisfactionText = View.Text(Refs.Satisfaction_Text);
        _masteryText = View.Text(Refs.Mastery_Text);
        _incidentText = View.Text(Refs.Incident_Text);
        _confirmButton = View.Button(Refs.ConfirmButton);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif

        if (_confirmButton != null)
            _confirmButton.onClick.AddListener(HandleConfirmClicked);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_confirmButton != null)
            _confirmButton.onClick.RemoveListener(HandleConfirmClicked);
    }

    public void Present(ServiceSettlementResult result)
    {
        if (!_valid || result == null)
            return;

        if (_titleText != null)
            _titleText.text = $"{result.MonsterDisplayName} 접객 결산";

        if (_reactionText != null)
        {
            _reactionText.text =
                $"크게 만족 {result.GreatlySatisfiedCount} / 만족 {result.SatisfiedCount} / 무반응 {result.NoResponseCount}\n" +
                $"기본 반응 점수 {result.BaseReactionScore}";
        }

        if (_multiplierText != null)
        {
            _multiplierText.text =
                $"{BurdenAxes.ToBurdenLabel(result.DemandAxis)} {result.DemandCollapse} " +
                $"→ {result.MultiplierLabel} ×{result.Multiplier:0.0}";
        }

        if (_energyText != null)
            _energyText.text = $"에너지 {result.Energy}";

        if (_satisfactionText != null)
        {
            _satisfactionText.text =
                $"만족도 {result.Satisfaction} / {result.RequiredSatisfaction} " +
                (result.IsSatisfactionMet ? "(성사)" : "(미달)");
        }

        if (_masteryText != null)
            _masteryText.text = BuildMasteryText(result);

        if (_incidentText != null)
            _incidentText.text = BuildIncidentText(result);
    }

    private string BuildMasteryText(ServiceSettlementResult result)
    {
        _builder.Clear();

        for (int i = 0; i < BurdenAxes.Count; i++)
        {
            BurdenAxis axis = BurdenAxes.FromIndex(i);
            int gain = result.MasteryGain[axis];

            if (gain <= 0)
                continue;

            if (_builder.Length > 0)
                _builder.Append("  ");

            _builder.Append(BurdenAxes.ToMasteryLabel(axis)).Append(" +").Append(gain);
        }

        return _builder.Length > 0 ? _builder.ToString() : "숙련 경험 없음";
    }

    private static string BuildIncidentText(ServiceSettlementResult result)
    {
        if (!result.IsIncident)
            return string.Empty;

        string axisLabel = BurdenAxes.ToBurdenLabel(result.ControlLossAxis);

        return result.IsMaidLost
            ? $"통제 상실 ({axisLabel}) · 담당자 회수 실패"
            : $"통제 상실 ({axisLabel}) · 후유증 {result.ResidualBurden}";
    }

    private void HandleConfirmClicked()
    {
        OnConfirmed?.Invoke();
    }

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _bgImage, Refs.SettleBG_Image);
        AppendMissing(ref missing, _titleText, Refs.Title_Text);
        AppendMissing(ref missing, _reactionText, Refs.Reaction_Text);
        AppendMissing(ref missing, _multiplierText, Refs.Multiplier_Text);
        AppendMissing(ref missing, _energyText, Refs.Energy_Text);
        AppendMissing(ref missing, _confirmButton, Refs.ConfirmButton);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[ServiceSettlementPanel] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}
