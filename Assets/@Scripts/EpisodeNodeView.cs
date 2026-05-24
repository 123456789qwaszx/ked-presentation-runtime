using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EpisodeNodeView
{
    private readonly EpisodeNodeRigRefs _refs;

    private string _episodeId = "";
    private string _upperTarget = "";
    private string _lowerTarget = "";

    public event Action<string> MainClicked;
    public event Action<string, LinkKind, string> BranchClicked;

    public EpisodeNodeView(EpisodeNodeRigRefs refs)
    {
        _refs = refs;

        if (_refs.MainCardHit_Button != null)
            _refs.MainCardHit_Button.onClick.AddListener(HandleMainClicked);

        if (_refs.UpperAttachmentHit_Button != null)
            _refs.UpperAttachmentHit_Button.onClick.AddListener(HandleUpperClicked);

        if (_refs.LowerAttachmentHit_Button != null)
            _refs.LowerAttachmentHit_Button.onClick.AddListener(HandleLowerClicked);
    }

    public void Dispose()
    {
        if (_refs.MainCardHit_Button != null)
            _refs.MainCardHit_Button.onClick.RemoveListener(HandleMainClicked);

        if (_refs.UpperAttachmentHit_Button != null)
            _refs.UpperAttachmentHit_Button.onClick.RemoveListener(HandleUpperClicked);

        if (_refs.LowerAttachmentHit_Button != null)
            _refs.LowerAttachmentHit_Button.onClick.RemoveListener(HandleLowerClicked);
    }

    public void Present(in EpisodeNodeModel model)
    {
        _episodeId = model.EpisodeId ?? "";
        _upperTarget = "";
        _lowerTarget = "";

        SetText(_refs.MainCardIndexText_Text, model.IndexText);
        SetText(_refs.MainCardTitle_Text, model.Title);

        if (_refs.MainCardHit_Button != null)
            _refs.MainCardHit_Button.interactable = model.Interactable && !model.Locked;

        PresentAttachment(
            _refs.UpperAttachment_Root,
            _refs.UpperAttachmentTitle_Text,
            _refs.UpperAttachmentHit_Button,
            model.UpperAttachment,
            ref _upperTarget
        );

        PresentAttachment(
            _refs.LowerAttachment_Root,
            _refs.LowerAttachmentTitle_Text,
            _refs.LowerAttachmentHit_Button,
            model.LowerAttachment,
            ref _lowerTarget
        );

        SetVisible(_refs.StateRoot_Selected, model.Selected, false);
        SetVisible(_refs.StateRoot_Current, model.IsCurrent, false);
        SetVisible(_refs.StateRoot_Completed, model.Completed, false);
        SetVisible(_refs.StateRoot_Locked, model.Locked, model.Locked);

        bool isEnding = model.Kind == EpisodeNodeKind.Ending;
        SetVisible(_refs.EndingBadge_Root, isEnding, false);

        if (isEnding)
            SetText(_refs.EndingBadge_Text, "ENDING");
        else
            SetText(_refs.EndingBadge_Text, "");
    }

    private void PresentAttachment(
        RectTransform root,
        TMP_Text titleText,
        Button button,
        EpisodeAttachmentModel? attachment,
        ref string targetId)
    {
        bool hasAttachment = attachment.HasValue;

        if (root != null)
            root.gameObject.SetActive(hasAttachment);

        if (!hasAttachment)
        {
            if (button != null)
                button.interactable = false;

            targetId = "";
            return;
        }

        EpisodeAttachmentModel value = attachment.Value;

        targetId = value.HostEpisodeId;

        SetText(titleText, value.DisplayTitle);

        if (button != null)
            button.interactable = value.IsInteractable;
    }

    private void HandleMainClicked()
    {
        if (string.IsNullOrEmpty(_episodeId))
            return;

        MainClicked?.Invoke(_episodeId);
    }

    private void HandleUpperClicked()
    {
        if (string.IsNullOrEmpty(_episodeId))
            return;

        if (string.IsNullOrEmpty(_upperTarget))
            return;

        BranchClicked?.Invoke(_episodeId, LinkKind.BranchUpper, _upperTarget);
    }

    private void HandleLowerClicked()
    {
        if (string.IsNullOrEmpty(_episodeId))
            return;

        if (string.IsNullOrEmpty(_lowerTarget))
            return;

        BranchClicked?.Invoke(_episodeId, LinkKind.AttachmentLower, _lowerTarget);
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value ?? "";
    }

    private static void SetVisible(
        CanvasGroup group,
        bool visible,
        bool blockRaycasts)
    {
        if (group == null)
            return;

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = blockRaycasts;
    }
}