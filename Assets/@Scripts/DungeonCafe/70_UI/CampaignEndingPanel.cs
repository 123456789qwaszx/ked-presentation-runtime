using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

/// <summary>
/// 엔딩 표시.
///
/// 엔딩 노드가 재생되는 '동안' 떠 있어야 하므로, 표시와 확인 대기를 분리한다.
/// Present 는 즉시 반환하고, 확인 버튼은 노드가 끝난 뒤에 열린다.
/// </summary>
public sealed class CampaignEndingPanel : UIPanel<CampaignEndingPanel.Refs>, IManagedUI
{
    public event Action OnDismissed;

    #region Refs
    public enum Refs
    {
        EndingBG_Root,
        EndingBG_Image,

        Ending_Title_Text,
        Ending_Reason_Text,
        Ending_Species_Text,
        Ending_Summary_Text,

        EndingDismissButton,
    }

    private Image _bgImage;
    private TMP_Text _titleText;
    private TMP_Text _reasonText;
    private TMP_Text _speciesText;
    private TMP_Text _summaryText;
    private Button _dismissButton;

    private bool _valid;
    #endregion

    protected override void OnInitialize()
    {
        _bgImage = View.Image(Refs.EndingBG_Image);
        _titleText = View.Text(Refs.Ending_Title_Text);
        _reasonText = View.Text(Refs.Ending_Reason_Text);
        _speciesText = View.Text(Refs.Ending_Species_Text);
        _summaryText = View.Text(Refs.Ending_Summary_Text);
        _dismissButton = View.Button(Refs.EndingDismissButton);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif

        _dismissButton.onClick.RemoveListener(HandleDismiss);
        _dismissButton.onClick.AddListener(HandleDismiss);

        SetDismissVisible(false);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_dismissButton != null)
            _dismissButton.onClick.RemoveListener(HandleDismiss);
    }

    public void Present(CampaignEndingResult ending, CampaignState campaign)
    {
        if (!_valid || ending == null)
            return;

        if (_titleText != null)
            _titleText.text = ending.Title;

        if (_reasonText != null)
            _reasonText.text = ending.Reason;

        if (_speciesText != null)
        {
            bool hasSpecies = ending.IsBadEnding && ending.CollapseSpecies != MonsterSpecies.None;

            _speciesText.gameObject.SetActive(hasSpecies);

            if (hasSpecies)
                _speciesText.text = $"파멸 계열: {ending.CollapseSpecies}";
        }

        if (_summaryText != null && campaign != null)
        {
            _summaryText.text =
                $"누적 에너지  {campaign.TotalEnergy} / {campaign.Tuning.CampaignEnergyQuota}\n" +
                $"숙련 도달  {campaign.CountTotalMasteryLevels()}\n" +
                $"통제 상실  {campaign.CountTotalIncidents()}건\n" +
                $"이탈 인원  {campaign.CountLostMaids()}명";
        }

        // 엔딩 노드가 재생되는 동안에는 확인 버튼을 숨긴다.
        SetDismissVisible(false);
    }

    /// <summary>엔딩 노드가 끝난 뒤 호출한다.</summary>
    public void AllowDismiss() => SetDismissVisible(true);

    private void SetDismissVisible(bool visible)
    {
        if (_dismissButton != null)
            _dismissButton.gameObject.SetActive(visible);
    }

    private void HandleDismiss() => OnDismissed?.Invoke();

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _bgImage, Refs.EndingBG_Image);
        AppendMissing(ref missing, _titleText, Refs.Ending_Title_Text);
        AppendMissing(ref missing, _reasonText, Refs.Ending_Reason_Text);
        AppendMissing(ref missing, _dismissButton, Refs.EndingDismissButton);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[CampaignEndingPanel] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}
