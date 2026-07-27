using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

/// <summary>
/// 예약 게시판. (v3 §1)
///
/// v3 에서 편성은 시스템이 결정론으로 확정한다 — 이 화면은 선택이 아니라 열람이다.
/// 첫 방문 개체는 이름을 감추고 게시 문구만 노출한다. 플레이어가 문구로 추론하는 것이 목적.
/// 어느 카드를 눌러도 게시판 확인으로 간주하고 닫힌다.
/// </summary>
public sealed class ReservationBoardPanel : UIPanel<ReservationBoardPanel.Refs>, IManagedUI
{
    public event Action<int> OnBookingSelected;

    #region Refs
    public enum Refs
    {
        BoardBG_Root,
        BoardBG_Image,

        Board_Title_Text,
        Board_Guide_Text,

        BoardList_Root,
        BoardList_Content,

        BoardCardPrefab,
    }

    private Image _bgImage;
    private TMP_Text _titleText;
    private TMP_Text _guideText;
    private RectTransform _content;

    [SerializeField] private VNOptionItem _boardCardPrefab;

    private readonly GuesthouseOptionItemList _list = new();
    private readonly List<GuesthouseOptionEntry> _entries = new();

    private bool _valid;
    private bool _locked;
    #endregion

    protected override void OnInitialize()
    {
        _bgImage = View.Image(Refs.BoardBG_Image);
        _titleText = View.Text(Refs.Board_Title_Text);
        _guideText = View.Text(Refs.Board_Guide_Text);
        _content = View.Rect(Refs.BoardList_Content);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif

        _list.Configure(_boardCardPrefab, _content);

        _list.OnSubmitted -= HandleCardSubmitted;
        _list.OnSubmitted += HandleCardSubmitted;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        _list.OnSubmitted -= HandleCardSubmitted;
        _list.Clear();
    }

    public void Present(int dayNumber, IReadOnlyList<MonsterProfileV3> bookings, CampaignStateV3 campaign)
    {
        _locked = false;

        if (_titleText != null)
            _titleText.text = $"{dayNumber}일차 예약 게시판";

        if (_guideText != null)
            _guideText.text = "오늘의 예약입니다. 확인하면 순서대로 통화가 이어집니다.";

        _entries.Clear();

        for (int i = 0; i < bookings.Count; i++)
        {
            MonsterProfileV3 monster = bookings[i];

            // 통화 이력이 있는 개체(이해도 일부 파악 이상)만 이름을 밝힌다. (§8.2)
            UnderstandingTier tier = campaign.Understanding.GetTier(monster.MonsterId, campaign.Tuning);

            string bookingLabel = tier >= UnderstandingTier.Partial
                ? $"{monster.DisplayName}\n{monster.ReservationPostText}"
                : $"미확인 문의\n{monster.ReservationPostText}";

            _entries.Add(new GuesthouseOptionEntry(bookingLabel));
        }

        _list.Rebuild(_entries);
    }
    
    private void HandleCardSubmitted(int index)
    {
        if (_locked)
            return;

        _locked = true;
        OnBookingSelected?.Invoke(index);
    }

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _bgImage, Refs.BoardBG_Image);
        AppendMissing(ref missing, _titleText, Refs.Board_Title_Text);
        AppendMissing(ref missing, _content, Refs.BoardList_Content);
        AppendMissing(ref missing, _boardCardPrefab, Refs.BoardCardPrefab);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[ReservationBoardPanel] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}
