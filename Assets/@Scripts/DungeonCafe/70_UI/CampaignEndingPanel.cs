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

    public void Present(CampaignState campaign, EndingKind ending)
    {
        if (!_valid || campaign == null)
            return;

        if (_titleText != null)
            _titleText.text = ToTitle(ending);

        if (_reasonText != null)
            _reasonText.text = ToReason(campaign, ending);

        if (_speciesText != null)
        {
            // v3 의 파국은 종족 단위 엔딩 노드로 연출되고, 여기서는 낙인 여부만 병기한다. (§15)
            bool hasScar = HasRouteScar(campaign);
            _speciesText.gameObject.SetActive(hasScar);
            if (hasScar)
                _speciesText.text = "루트 파국 낙인: 완전 붕괴가 있었습니다";
        }

        if (_summaryText != null)
        {
            _summaryText.text =
                $"누적 욕구  {campaign.Ledger.Lifetime}\n" +
                $"가게 레벨  {campaign.ShopLevel}\n" +
                $"할당 미달  {campaign.BankruptcyCount}회\n" +
                $"이탈 인원  {CountLost(campaign)}명";
        }

        // 엔딩 노드가 재생되는 동안에는 확인 버튼을 숨긴다.
        SetDismissVisible(false);
    }

    private static string ToTitle(EndingKind ending) => ending switch
    {
        EndingKind.FullHouseMorning => "S - 만실의 아침",
        EndingKind.NormalBusiness => "A - 정상 영업",
        EndingKind.ClosingTime => "B - 폐점 시간",
        EndingKind.Bankruptcy => "폐업",
        EndingKind.EmptyInn => "전멸 - 빈 객잔",
        _ => "…",
    };

    private static string ToReason(CampaignState campaign, EndingKind ending) => ending switch
    {
        EndingKind.FullHouseMorning => "15일 완주 / 전원 생존 / 관계 4단계 달성",
        EndingKind.NormalBusiness => "15일 완주 / 전원 생존",
        EndingKind.ClosingTime => "완주했으나 조건 미달",
        EndingKind.Bankruptcy => $"할당 미달 {campaign.Tuning.BankruptcyLimit}회 누적",
        EndingKind.EmptyInn => "가용 메이드 없음",
        _ => string.Empty,
    };

    private static bool HasRouteScar(CampaignState campaign)
    {
        for (int i = 0; i < campaign.Maids.Count; i++)
            if (campaign.Maids[i].TotalCollapseCount > 0) return true;
        return false;
    }

    private static int CountLost(CampaignState campaign)
    {
        int n = 0;
        for (int i = 0; i < campaign.Maids.Count; i++)
            if (campaign.Maids[i].IsLost) n++;
        return n;
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
