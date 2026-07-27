using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

// 하루 업무 종료 리포트.
// 결산 수치를 항목별로 분리해 보여주고, 밤 구간으로 넘어가는 확인만 받는다.
public sealed class DayReportPanel : UIPanel<DayReportPanel.Refs>
{
    public event Action OnConfirmed;

    #region Refs
    public enum Refs
    {
        ReportBG_Root,
        ReportBG_Image,

        Report_Title_Text,
        Report_Energy_Text,
        Report_Progress_Text,
        Report_Incident_Text,
        Report_Note_Text,

        ReportConfirmButton,
    }

    private Image _bgImage;
    private TMP_Text _titleText;
    private TMP_Text _energyText;
    private TMP_Text _progressText;
    private TMP_Text _incidentText;
    private TMP_Text _noteText;
    private Button _confirmButton;

    private bool _valid;
    #endregion

    protected override void OnInitialize()
    {
        _bgImage = View.Image(Refs.ReportBG_Image);
        _titleText = View.Text(Refs.Report_Title_Text);
        _energyText = View.Text(Refs.Report_Energy_Text);
        _progressText = View.Text(Refs.Report_Progress_Text);
        _incidentText = View.Text(Refs.Report_Incident_Text);
        _noteText = View.Text(Refs.Report_Note_Text);
        _confirmButton = View.Button(Refs.ReportConfirmButton);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif

        _confirmButton.onClick.RemoveListener(HandleConfirm);
        _confirmButton.onClick.AddListener(HandleConfirm);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_confirmButton != null)
            _confirmButton.onClick.RemoveListener(HandleConfirm);
    }

    public void Present(CampaignStateV3 campaign, DayStateV3 day, bool quotaMet)
    {
        if (!_valid || campaign == null || day == null)
            return;

        if (_titleText != null)
            _titleText.text = $"{day.DayNumber}일차 업무 종료";

        if (_energyText != null)
            _energyText.text = $"금일 획득 욕구  {campaign.Ledger.Today} / {day.Plan.Quota}";

        if (_progressText != null)
            _progressText.text =
                $"보유 {campaign.Ledger.Held}  ·  누적 {campaign.Ledger.Lifetime}  ·  가게 Lv{campaign.ShopLevel}";

        if (_incidentText != null)
            _incidentText.text = quotaMet
                ? "할당 달성"
                : $"할당 미달  (경고 {campaign.BankruptcyCount} / {campaign.Tuning.BankruptcyLimit})";

        if (_noteText != null)
            _noteText.text = BuildNote(campaign, quotaMet);
    }

    private static string BuildNote(CampaignStateV3 campaign, bool quotaMet)
    {
        if (!quotaMet)
        {
            int remain = campaign.Tuning.BankruptcyLimit - campaign.BankruptcyCount;
            return remain <= 1
                ? "다음 미달이면 폐업입니다."
                : $"기준에 미치지 못했습니다. 미달 {remain}회가 남았습니다.";
        }

        return "기준 욕구를 확보했습니다. 밤 처리로 넘어갑니다.";
    }

    private void HandleConfirm() => OnConfirmed?.Invoke();

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _bgImage, Refs.ReportBG_Image);
        AppendMissing(ref missing, _titleText, Refs.Report_Title_Text);
        AppendMissing(ref missing, _energyText, Refs.Report_Energy_Text);
        AppendMissing(ref missing, _confirmButton, Refs.ReportConfirmButton);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[DayReportPanel] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}
