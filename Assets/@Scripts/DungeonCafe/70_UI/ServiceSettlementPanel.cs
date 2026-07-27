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

    public void Present(ServiceSessionStateV3 session, in SettlementV3Result result)
    {
        if (!_valid || session == null)
            return;

        if (_titleText != null)
            _titleText.text = $"{session.Monster.DisplayName} 접객 결산";

        if (_reactionText != null)
        {
            _reactionText.text =
                $"반응 점수 {result.ReactionScore}" +
                (session.DepthReactionScore > 0 ? $"  (심층 중 {session.DepthReactionScore} — 미산입)" : string.Empty);
        }

        if (_multiplierText != null)
        {
            // 절벽 노출: 미달 하향이 있었으면 기준 배율과 함께 보여준다. (§2.4)
            string downgrade = result.WasDowngraded
                ? $"  (미달 하향 ×{result.BaseMultiplier:0.0} → ×{result.AppliedMultiplier:0.0})"
                : string.Empty;

            _multiplierText.text =
                $"{BurdenAxes.ToBurdenLabel(session.Monster.DemandAxis)} {result.EndCollapse} " +
                $"-> {result.BandLabel} x {result.AppliedMultiplier:0.0}{downgrade}";
        }

        if (_energyText != null)
            _energyText.text = $"획득 욕구 {result.Energy}";

        if (_satisfactionText != null)
        {
            _satisfactionText.text =
                $"만족도 {result.Satisfaction} / {result.RequiredSatisfaction} " +
                (result.SatisfactionMet ? "(성사)" : "(미달)");
        }

        if (_masteryText != null)
            _masteryText.text = BuildMasteryText(session);

        if (_incidentText != null)
            _incidentText.text = BuildOutcomeText(session, result);
    }

    /// <summary>숙련 XP = 완화 전 원본 부하 누적. (§12.3)</summary>
    private string BuildMasteryText(ServiceSessionStateV3 session)
    {
        _builder.Clear();

        for (int i = 0; i < BurdenAxes.Count; i++)
        {
            BurdenAxis axis = BurdenAxes.FromIndex(i);
            int gain = session.AccumulatedRawLoad[axis];

            if (gain <= 0)
                continue;

            if (_builder.Length > 0)
                _builder.Append("  ");

            _builder.Append(BurdenAxes.ToMasteryLabel(axis)).Append(" +").Append(gain);
        }

        return _builder.Length > 0 ? _builder.ToString() : "숙련 경험 없음";
    }

    private static string BuildOutcomeText(ServiceSessionStateV3 session, in SettlementV3Result result)
    {
        string axisLabel = BurdenAxes.ToBurdenLabel(session.DepthAxis);

        return result.Kind switch
        {
            SettlementOutcomeKind.DepthEscape =>
                $"붕괴심층 진입 ({axisLabel}) · {session.DepthBeatCount}비트 만에 회수 — 결산 ×0.5",
            SettlementOutcomeKind.TotalCollapse =>
                session.Maid.IsLost
                    ? $"완전 붕괴 ({axisLabel}) · 담당자 영구 이탈"
                    : $"완전 붕괴 ({axisLabel}) · 생환권 사용 — 수입 0",
            _ => string.Empty,
        };
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
