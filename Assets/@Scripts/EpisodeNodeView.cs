using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EpisodeNodeView : IDisposable
{
    private readonly EpisodeNodeRigRefs _refs;

    private string _episodeId = "";
    private EpisodeNodeLinkModel? _upperLink;
    private EpisodeNodeLinkModel? _lowerLink;

    public event Action<string> MainClicked;
    public event Action<string, EpisodeNodeLinkSlot, EpisodeNodeLinkModel> LinkClicked;

    public EpisodeNodeView(EpisodeNodeRigRefs refs)
    {
        _refs = refs;

        if (_refs == null)
            return;

        if (_refs.MainCardHit_Button != null)
            _refs.MainCardHit_Button.onClick.AddListener(HandleMainClicked);

        if (_refs.UpperLinkHit_Button != null)
            _refs.UpperLinkHit_Button.onClick.AddListener(HandleUpperClicked);

        if (_refs.LowerLinkHit_Button != null)
            _refs.LowerLinkHit_Button.onClick.AddListener(HandleLowerClicked);
    }

    public void Dispose()
    {
        if (_refs != null)
        {
            if (_refs.MainCardHit_Button != null)
                _refs.MainCardHit_Button.onClick.RemoveListener(HandleMainClicked);

            if (_refs.UpperLinkHit_Button != null)
                _refs.UpperLinkHit_Button.onClick.RemoveListener(HandleUpperClicked);

            if (_refs.LowerLinkHit_Button != null)
                _refs.LowerLinkHit_Button.onClick.RemoveListener(HandleLowerClicked);
        }

        MainClicked = null;
        LinkClicked = null;

        _episodeId = "";
        _upperLink = null;
        _lowerLink = null;
    }

    public void Present(in EpisodeNodeModel model)
    {
        _episodeId = model.EpisodeId ?? "";
        _upperLink = model.UpperLink;
        _lowerLink = model.LowerLink;

        ApplyRootLayout(model);
        ApplyMainCard(model);
        ApplyLink(
            _refs.UpperLink_Root,
            _refs.UpperLinkTitle_Text,
            _refs.UpperLinkHit_Button,
            _refs.UpperLinkBG_Image,
            model.UpperLink,
            model.UpperLinkBg);

        ApplyLink(
            _refs.LowerLink_Root,
            _refs.LowerLinkTitle_Text,
            _refs.LowerLinkHit_Button,
            _refs.LowerLinkBG_Image,
            model.LowerLink,
            model.LowerLinkBg);

        ApplyStates(model);
        ApplyEnding(model);
    }

    private void ApplyRootLayout(in EpisodeNodeModel model)
    {
        RectTransform root = _refs.RigRoot;

        if (root == null)
            return;

        root.anchoredPosition = model.AnchoredPos;

        if (model.Size.x > 0f && model.Size.y > 0f)
            root.sizeDelta = model.Size;
    }

    private void ApplyMainCard(in EpisodeNodeModel model)
    {
        SetText(_refs.MainCardIndexText_Text, model.IndexText);
        SetText(_refs.MainCardTitle_Text, model.Title);

        SetSprite(_refs.MainCardBG_Image, model.MainBg);
        SetSprite(_refs.MainCardIndexIcon_Image, model.MainIcon);

        if (_refs.MainCardBG_Image != null)
        {
            _refs.MainCardBG_Image.color = new Color(1f, 1f, 1f, 0.65f);
            _refs.MainCardBG_Image.raycastTarget = false;
        }

        if (_refs.MainCardHit_Button != null)
            _refs.MainCardHit_Button.interactable = model.Interactable && !model.Locked;
    }

    private void ApplyLink(
        RectTransform root,
        TMP_Text titleText,
        Button button,
        Image bgImage,
        EpisodeNodeLinkModel? link,
        Sprite bgSprite)
    {
        bool hasLink = link.HasValue;

        if (root != null)
            root.gameObject.SetActive(hasLink);

        if (!hasLink)
        {
            SetText(titleText, "");

            if (button != null)
                button.interactable = false;

            return;
        }

        EpisodeNodeLinkModel value = link.Value;

        SetText(titleText, value.DisplayTitle);
        SetSprite(bgImage, bgSprite);
        if (bgImage != null)
        {
            bgImage.color = new Color(1f, 1f, 1f, 0.45f);
            bgImage.raycastTarget = false;
        }

        if (button != null)
            button.interactable = value.Interactable;
    }

    private void ApplyStates(in EpisodeNodeModel model)
    {
        SetVisible(_refs.StateRoot_Selected, model.Selected, false);
        SetVisible(_refs.StateRoot_Current, model.IsCurrent, false);
        SetVisible(_refs.StateRoot_Completed, model.Completed, false);
        SetVisible(_refs.StateRoot_Locked, model.Locked, model.Locked);
    }

    private void ApplyEnding(in EpisodeNodeModel model)
    {
        bool isEnding = model.Role == EpisodeNodeRole.Ending;

        SetVisible(_refs.EndingBadge_Root, isEnding, false);
        SetText(_refs.EndingBadge_Text, isEnding ? "ENDING" : "");
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

        if (!_upperLink.HasValue)
            return;

        LinkClicked?.Invoke(_episodeId, EpisodeNodeLinkSlot.Upper, _upperLink.Value);
    }

    private void HandleLowerClicked()
    {
        if (string.IsNullOrEmpty(_episodeId))
            return;

        if (!_lowerLink.HasValue)
            return;

        LinkClicked?.Invoke(_episodeId, EpisodeNodeLinkSlot.Lower, _lowerLink.Value);
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value ?? "";
    }

    private static void SetSprite(Image image, Sprite sprite)
    {
        if (image == null || sprite == null)
            return;

        image.sprite = sprite;
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