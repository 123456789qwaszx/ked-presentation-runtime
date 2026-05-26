using System;
using UnityEngine;

public sealed class EpisodeNodeView : IDisposable
{
    public event Action<string> MainClicked;

    private readonly EpisodeNodeRefs _refs;
    private string _episodeId;

    public EpisodeNodeView(EpisodeNodeRefs refs)
    {
        _refs = refs;
        _refs.MainCardHit_Button.onClick.AddListener(HandleMainClicked);
    }
    
    private void HandleMainClicked() => MainClicked?.Invoke(_episodeId);

    public void Present(EpisodeNodeViewData viewData)
    {
        if (viewData == null)
            return;

        _episodeId = viewData.EpisodeId ?? "";
        _refs.MainCardTitle_Text.text = viewData.Title ?? "";
        _refs.MainCardIndexText_Text.text = viewData.IndexText ?? "";

        ApplyVisualState(viewData.VisualState);
    }

    private void ApplyVisualState(EpisodeNodeVisualState state)
    {
        bool locked = state == EpisodeNodeVisualState.Locked;

        _refs.MainCardHit_Button.interactable = !locked;

        if (_refs.MainCard_Root != null)
        {
            CanvasGroup group = _refs.MainCard_Root.GetComponent<CanvasGroup>();

            if (group != null)
            {
                group.alpha = locked ? 0.55f : 1f;
                group.interactable = !locked;
                group.blocksRaycasts = !locked;
            }
        }
    }
    
    public void Dispose()
    {
        if (_refs.MainCardHit_Button != null)
            _refs.MainCardHit_Button.onClick.RemoveListener(HandleMainClicked);

        MainClicked = null;
    }
}