using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

/// <summary>
/// 예약 게시판.
///
/// 아직 확정되지 않은 문의이므로 종족과 요구 유형을 밝히지 않는다.
/// 플레이어가 게시 문구만 보고 추론하는 것이 이 화면의 목적이다.
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

    public void Present(int dayNumber, IReadOnlyList<ServiceBookingState> bookings)
    {
        _locked = false;

        if (_titleText != null) 
            _titleText.text = $"{dayNumber}일차 예약 문의";
        
        if (_guideText != null) 
            _guideText.text = "문의를 골라 전화를 걸면 상대가 확정됩니다.";

        _entries.Clear();

        for (int i = 0; i < bookings.Count; i++)
        {// 확정 전에는 개체 이름과 종족을 감춘다. 게시 문구만 노출.
            string bookingLabel;
            
            if (bookings[i].IsCodexRevealed)
                bookingLabel = $"{bookings[i].Monster.DisplayName}\n{bookings[i].Monster.ReservationPostText}";
            else
                bookingLabel = $"미확인 문의\n{bookings[i].Monster.ReservationPostText}";
            
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
