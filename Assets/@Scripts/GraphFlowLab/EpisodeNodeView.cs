using System;
using UnityEngine;

public sealed class EpisodeNodeView : IDisposable
{
    public event Action<string> MainClicked;
    public event Action<string, EpisodeNodeLinkSlot, EpisodeNodeLinkViewData> LinkClicked;

    private readonly EpisodeNodeRigRefs _refs;
    private string _episodeId;

    public EpisodeNodeView(EpisodeNodeRigRefs refs)
    {
        _refs = refs;
        BindButtons();
    }

    public void Present(EpisodeNodeViewData viewData)
    {
        _episodeId = viewData.EpisodeId;

        SetText(_refs.MainCardTitle_Text, viewData.Title);
        ApplyVisualState(viewData.VisualState);

        PresentLink(EpisodeNodeLinkSlot.Upper, viewData.UpperLink);
        PresentLink(EpisodeNodeLinkSlot.Lower, viewData.LowerLink);
    }

    private void ApplyVisualState(EpisodeNodeVisualState state)
    {
        SetGroup(_refs.StateRoot_Selected, state == EpisodeNodeVisualState.Selected);
        SetGroup(_refs.StateRoot_Current, state == EpisodeNodeVisualState.Current);
        SetGroup(_refs.StateRoot_Completed, state == EpisodeNodeVisualState.Completed);
        SetGroup(_refs.StateRoot_Locked, state == EpisodeNodeVisualState.Locked);
    }

    private void PresentLink(EpisodeNodeLinkSlot slot, EpisodeNodeLinkViewData link)
    {
        LinkClicked?.Invoke("LinkClick", slot, link);
        // Upper/Lower refs 선택 후 visible/text 반영
    }

    private void BindButtons()
    {
        if (_refs.MainCardHit_Button != null)
            _refs.MainCardHit_Button.onClick.AddListener(() => MainClicked?.Invoke(_episodeId));
    }

    private static void SetText(TMPro.TMP_Text text, string value)
    {
        if (text != null)
            text.text = value ?? "";
    }

    private static void SetGroup(CanvasGroup group, bool visible)
    {
        if (group == null)
            return;

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    public void Dispose()
    {
        if (_refs.MainCardHit_Button != null)
            _refs.MainCardHit_Button.onClick.RemoveAllListeners();
    }
}