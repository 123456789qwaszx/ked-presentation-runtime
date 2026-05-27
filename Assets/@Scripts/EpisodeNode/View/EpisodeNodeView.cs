using System;
using UnityEngine;

public sealed class EpisodeNodeView : IDisposable
{
    public event Action<string> MainClicked;

    private readonly EpisodeNodeRefs _refs;
    private string _episodeId = "";

    public EpisodeNodeView(EpisodeNodeRefs refs)
    {
        _refs = refs;

        if (_refs.MainCardHit_Button != null)
            _refs.MainCardHit_Button.onClick.AddListener(HandleMainClicked);
    }

    private void HandleMainClicked()
    {
        if (string.IsNullOrEmpty(_episodeId))
            return;

        MainClicked?.Invoke(_episodeId);
    }

    public void Present(EpisodeNodeViewData viewData)
    {
        if (viewData == null)
            return;

        _episodeId = viewData.EpisodeId ?? "";

        if (_refs.MainCardTitle_Text != null)
            _refs.MainCardTitle_Text.text = viewData.Title ?? "";

        if (_refs.MainCardIndexText_Text != null)
            _refs.MainCardIndexText_Text.text = viewData.IndexText ?? "";

        ApplyVisualState(viewData.VisualState);
    }

    private void ApplyVisualState(EpisodeNodeVisualState state)
    {
        bool locked = state == EpisodeNodeVisualState.Locked;

        if (_refs.MainCardHit_Button != null)
            _refs.MainCardHit_Button.interactable = !locked;

        if (_refs.MainCard_Root == null)
            return;

        CanvasGroup group = _refs.MainCard_Root.GetComponent<CanvasGroup>();

        if (group == null)
            return;

        group.alpha = locked ? 0.55f : 1f;
        group.interactable = !locked;
        group.blocksRaycasts = !locked;
    }

    public void Dispose()
    {
        if (_refs.MainCardHit_Button != null)
            _refs.MainCardHit_Button.onClick.RemoveListener(HandleMainClicked);

        MainClicked = null;
        _episodeId = "";
    }
}