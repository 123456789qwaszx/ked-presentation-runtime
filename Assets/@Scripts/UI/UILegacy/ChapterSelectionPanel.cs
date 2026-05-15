#nullable enable
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ChapterSelectionPanel : UIPanel<ChapterSelectionPanel.Refs>
{
    public enum Refs
    {
        // ---- Layout / BG
        SafeArea,
        SelectChapterBG_Root,
        SelectChapterBG_Image,

        // ---- Chapter list container + items
        ButtonViewport,
        ChapterButtons,          // container
        ChapterCard01,
        ChapterCard02,
        ChapterCard03,
        ChapterCard04,
        ChapterCard05,
        ChapterCard06,

        // ---- Return block
        ReturnBlock_Root,
        CurrentScreenLabel_Root,
        CurrentScreenLabelBG_Image,
        CurrentScreenLabel_Text,
        CurrentScreenLabelIcon_Image,

        ReturnButton_Root,
        ReturnButton,            // Button component ref
        ReturnButton_Image,

        // ---- Hero / Char block
        CharBlock_Root,
        CharGradient_Root,
        CharGradient_Image,

        Character_Root,
        Character_Image,

        CharHUD_Root,
        CharHUDGradient_Root,
        CharHUDGradient_Image,

        AffinityBadge_Root,
        AffinityBadge_Image,

        CharName_Root,
        CharName_Text,

        ChangePortraitButton_Root,
        ChangePortraitButton,    // Button component ref
        ChangePortraitButton_Image,
        ChangePortraitButton_Text,
    }

    public event Action<int>? OnChapterRequested; // 이제 0..N 가능
    public event Action? OnBackRequested;

    private readonly ChapterButtonCard[] _cards = new ChapterButtonCard[6];

    // 슬롯별로 “현재 표시 중인 ChapterId”를 기억
    private readonly int[] _slotChapterIds = new int[6];

    private int _selectedChapterId = -1;

    protected override void OnInitialize()
    {
        // Cards resolve
        _cards[0] = ResolveCard(Refs.ChapterCard01);
        _cards[1] = ResolveCard(Refs.ChapterCard02);
        _cards[2] = ResolveCard(Refs.ChapterCard03);
        _cards[3] = ResolveCard(Refs.ChapterCard04);
        _cards[4] = ResolveCard(Refs.ChapterCard05);
        _cards[5] = ResolveCard(Refs.ChapterCard06);

        // 클릭은 “슬롯 index”만 캡쳐
        for (int i = 0; i < _cards.Length; i++)
        {
            int slot = i;
            var card = _cards[i];
            if (card == null) continue;

            card.BindClick(() => RequestChapterBySlot(slot));
        }
        
        BindEvent(View.Button(Refs.ReturnButton) ,OnReturn);
    }

    private ChapterButtonCard ResolveCard(Refs r)
    {
        var go = View.Rect(r);
        if (go == null)
        {
            Debug.LogWarning($"[ChapterSelectionPanel] Missing {r} ref.", this);
            return null!;
        }

        var card = go.GetComponent<ChapterButtonCard>();
        if (card == null)
        {
            Debug.LogWarning($"[ChapterSelectionPanel] {r} has no ChapterButtonCard component.", this);
            return null!;
        }

        return card;
    }

    private void RequestChapterBySlot(int slot)
    {
        if (slot < 0 || slot >= _slotChapterIds.Length) return;

        int chapterId = _slotChapterIds[slot];
        if (chapterId < 0) return; // 빈 슬롯이면 무시

        SetSelectedChapter(chapterId);
        OnChapterRequested?.Invoke(chapterId);
    }

    public void PresentChapters(ChapterButtonCardModel[] models, int selectedChapterId = -1)
    {
        _selectedChapterId = selectedChapterId;

        for (int i = 0; i < _cards.Length; i++)
        {
            var card = _cards[i];
            if (card == null) continue;

            if (models != null && i < models.Length)
            {
                var m = models[i];

                // 슬롯에 “진짜 ChapterId” 저장 (여기서 0도 가능)
                _slotChapterIds[i] = m.ChapterId;

                card.Present(m);
                //card.SetSelected(m.ChapterId == _selectedChapterId);
            }
            else
            {
                _slotChapterIds[i] = -1;
                card.Present(ChapterButtonCardModel.Empty());
                //card.SetSelected(false);
            }
        }
    }

    public void SetSelectedChapter(int chapterId)
    {
        _selectedChapterId = chapterId;

        for (int i = 0; i < _cards.Length; i++)
        {
            var card = _cards[i];
            if (card == null) continue;

            // 인덱스(i+1) 비교 금지. 실제 chapterId로 비교
            //card.SetSelected(_slotChapterIds[i] == chapterId);
        }
    }
    
    private void OnReturn(PointerEventData _) => OnBackRequested?.Invoke();
}