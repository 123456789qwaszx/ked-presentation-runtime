using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UIRefValidation;

// 앨범 그리드의 항목 하나.
// 받은 표시 모델을 그릴 뿐, 해금 판정이나 앨범 데이터 저장은 모른다.
public sealed class VNAlbumSlotButton
    : UIBase<VNAlbumSlotButton.Refs>
{
    public event Action<AlbumEntryViewModel> Clicked;

    #region Refs

    public enum Refs
    {
        AlbumSlotButton_Button,

        Thumbnail_Image,
        TitleLabel_Text,

        LockedOverlay_Root,
    }

    private Button _button;

    private Image _thumbnailImage;
    private TMP_Text _titleLabel;

    private RectTransform _lockedOverlay;

    #endregion

    private bool _valid;

    private AlbumEntryViewModel _model;

    protected override void OnInitialize()
    {
        CacheRefs();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();

        if (!_valid)
            return;
#else
        _valid = true;
#endif

        BindHandlers();
    }

    private void CacheRefs()
    {
        _button =
            View.Button(Refs.AlbumSlotButton_Button);

        _thumbnailImage =
            View.Image(Refs.Thumbnail_Image);

        _titleLabel =
            View.Text(Refs.TitleLabel_Text);

        _lockedOverlay =
            View.Rect(Refs.LockedOverlay_Root);
    }

    private void BindHandlers()
    {
        BindEvent(
            _button,
            PressAlbumSlotButton);
    }

    #region Present

    public void Present(AlbumEntryViewModel model)
    {
        if (!_valid)
            return;

        _model = model;

        if (model == null)
        {
            PresentEmpty();
            return;
        }

        PresentThumbnail(model);
        PresentTitle(model);

        _lockedOverlay.gameObject.SetActive(
            !model.IsUnlocked);

        _button.interactable =
            model.IsUnlocked;
    }

    private void PresentThumbnail(AlbumEntryViewModel model)
    {
        Sprite thumbnail =
            model.IsUnlocked
                ? model.Thumbnail
                : null;

        _thumbnailImage.sprite = thumbnail;
        _thumbnailImage.enabled = thumbnail != null;
    }

    private void PresentTitle(AlbumEntryViewModel model)
    {
        _titleLabel.text =
            model.IsUnlocked
                ? model.Title
                : "???";
    }

    private void PresentEmpty()
    {
        _thumbnailImage.sprite = null;
        _thumbnailImage.enabled = false;

        _titleLabel.text = "";

        _lockedOverlay.gameObject.SetActive(true);

        _button.interactable = false;
    }

    #endregion

    #region Handlers

    private void PressAlbumSlotButton(PointerEventData _)
    {
        if (_model == null ||
            !_model.IsUnlocked)
        {
            return;
        }

        Clicked?.Invoke(_model);
    }

    #endregion

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(
            ref missing,
            _button,
            Refs.AlbumSlotButton_Button);

        AppendMissing(
            ref missing,
            _thumbnailImage,
            Refs.Thumbnail_Image);

        AppendMissing(
            ref missing,
            _titleLabel,
            Refs.TitleLabel_Text);

        AppendMissing(
            ref missing,
            _lockedOverlay,
            Refs.LockedOverlay_Root);

        if (missing.Length > 0)
        {
            Debug.LogWarning(
                $"[VNAlbumSlotButton] Missing refs:\n{missing}",
                this);

            return false;
        }

        return true;
    }
}