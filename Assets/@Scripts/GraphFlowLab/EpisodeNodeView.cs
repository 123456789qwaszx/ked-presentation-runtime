using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class EpisodeNodeView : IDisposable
{
    public event Action<string> MainClicked;

    private readonly EpisodeNodeRigRefs _refs;
    private string _episodeId;

    public EpisodeNodeView(EpisodeNodeRigRefs refs)
    {
        _refs = refs;
        BindButtons();
    }

    public void Present(EpisodeNodeViewData viewData)
    {
        if (viewData == null)
            return;

        _episodeId = viewData.EpisodeId ?? "";

        SetText(_refs.MainCardTitle_Text, viewData.Title);
        SetText(_refs.MainCardIndexText_Text, viewData.IndexText);

        ApplyVisualState(viewData.VisualState);
    }

    private void ApplyVisualState(EpisodeNodeVisualState state)
    {
        bool locked = state == EpisodeNodeVisualState.Locked;

        SetButtonInteractable(_refs.MainCardHit_Button, !locked);

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

    private void BindButtons()
    {
        if (_refs.MainCardHit_Button != null)
            _refs.MainCardHit_Button.onClick.AddListener(HandleMainClicked);
    }

    private void HandleMainClicked()
    {
        if (string.IsNullOrEmpty(_episodeId))
            return;

        MainClicked?.Invoke(_episodeId);
    }

    private static void SetText(TMPro.TMP_Text text, string value)
    {
        if (text != null)
            text.text = value ?? "";
    }

    private static void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }

    public void Dispose()
    {
        if (_refs.MainCardHit_Button != null)
            _refs.MainCardHit_Button.onClick.RemoveListener(HandleMainClicked);

        MainClicked = null;
    }
}