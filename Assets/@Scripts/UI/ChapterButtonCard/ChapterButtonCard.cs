using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ChapterButtonCard : UIBase<ChapterButtonCard.Refs>
{
    public enum Refs
    {
        Card_Root,
        CardMotion,
        CardRoatation,
        CardScale,

        Bg_Root,
        Bg_Pad,
        Bg_Image,

        BgOverlay_Root,
        BgOverlay_Pad,
        BgOverlay_Image,

        Index_Root,
        Index_Anchor,
        Index_Text,

        HeadingBlock_Root,

        ChapterIndexLabel_Root,
        ChapterIndexLabel_Image,
        ChapterIndexLabel_Text,

        ChapterTitleLabel_Root,
        ChapterTitleLabelBG_Image,
        ChapterTitleLabelIcon_Image,
        ChapterTitleLabel_Text,

        EpisodeHeadingLabel_Root,
        EpisodeHeadingLabel_Image,
        EpisodeHeadingLabel_Text,

        Hit_Button,
    }

    private Action<ChapterButtonCard> _onPressed;
    private Action<ChapterButtonCard> _onReleased;
    private Action<ChapterButtonCard> _onClicked;

    private ScrollRect _dragScrollRect;
    private bool _isPressed;
    private bool _isDragging;

    public int ChapterId { get; private set; } = -1;

    protected override void OnInitialize()
    {
        BindInputEvents();
    }

    public void SetHandlers(
        Action<ChapterButtonCard> onPressed,
        Action<ChapterButtonCard> onReleased,
        Action<ChapterButtonCard> onClicked)
    {
        _onPressed = onPressed;
        _onReleased = onReleased;
        _onClicked = onClicked;
    }

    public void SetDragScrollRect(ScrollRect scrollRect)
    {
        _dragScrollRect = scrollRect;
    }

    public void ClearHandlers()
    {
        _onPressed = null;
        _onReleased = null;
        _onClicked = null;
        _dragScrollRect = null;
    }

    public void Present(in ChapterButtonCardModel model)
    {
        ChapterId = model.ChapterId;

        SetText(View.Text(Refs.Index_Text), model.IndexText);
        SetText(View.Text(Refs.ChapterIndexLabel_Text), model.ChapterIndexLabel);
        SetText(View.Text(Refs.ChapterTitleLabel_Text), model.ChapterTitle);
        SetText(View.Text(Refs.EpisodeHeadingLabel_Text), model.EpisodeHeading);

        SetSprite(View.Image(Refs.Bg_Image), model.Bg);
        SetSprite(View.Image(Refs.BgOverlay_Image), model.BgOverlay);
        SetSprite(View.Image(Refs.ChapterIndexLabel_Image), model.ChapterIndexLabelSprite);
        SetSprite(View.Image(Refs.EpisodeHeadingLabel_Image), model.EpisodeHeadingLabelSprite);
        SetSprite(View.Image(Refs.ChapterTitleLabelIcon_Image), model.TitleIcon);

        SetInteractable(model.Interactable && !model.Locked);
    }

    private void SetInteractable(bool interactable)
    {
        Button hit = View.Button(Refs.Hit_Button);

        if (hit != null)
            hit.interactable = interactable;
    }

    private void BindInputEvents()
    {
        Button hit = View.Button(Refs.Hit_Button);

        if (hit == null)
            return;

        BindEvent(hit, HandlePointerDown, ETouchEvent.PointerDown);
        BindEvent(hit, HandlePointerUp, ETouchEvent.PointerUp);
        BindEvent(hit, HandleClicked, ETouchEvent.Click);

        BindEvent(hit, HandleBeginDrag, ETouchEvent.BeginDrag);
        BindEvent(hit, HandleDrag, ETouchEvent.Drag);
        BindEvent(hit, HandleEndDrag, ETouchEvent.EndDrag);
    }

    private void HandlePointerDown(PointerEventData _)
    {
        _isDragging = false;

        if (!CanInteract())
            return;

        _isPressed = true;
        _onPressed?.Invoke(this);
    }

    private void HandlePointerUp(PointerEventData _)
    {
        if (!_isPressed)
            return;

        _isPressed = false;
        _onReleased?.Invoke(this);
    }

    private void HandleClicked(PointerEventData _)
    {
        if (_isDragging)
            return;

        if (!CanInteract())
            return;

        _onClicked?.Invoke(this);
    }

    private void HandleBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;

        if (_isPressed)
        {
            _isPressed = false;
            _onReleased?.Invoke(this);
        }

        if (_dragScrollRect != null)
            _dragScrollRect.OnBeginDrag(eventData);
    }

    private void HandleDrag(PointerEventData eventData)
    {
        _isDragging = true;

        if (_dragScrollRect != null)
            _dragScrollRect.OnDrag(eventData);
    }

    private void HandleEndDrag(PointerEventData eventData)
    {
        if (_dragScrollRect != null)
            _dragScrollRect.OnEndDrag(eventData);
    }

    private bool CanInteract()
    {
        if (ChapterId < 0)
            return false;

        Button hit = View.Button(Refs.Hit_Button);

        if (hit == null)
            return false;

        return hit.interactable;
    }

    private static void SetText(TMP_Text target, string text)
    {
        if (target != null)
            target.text = text;
    }

    private static void SetSprite(Image image, Sprite sprite)
    {
        if (image == null || sprite == null)
            return;

        image.sprite = sprite;
    }
}