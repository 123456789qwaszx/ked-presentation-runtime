using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UIRefValidation;

public sealed class AlbumUIRoot
    : UIRoot<AlbumUIRoot.Refs>
{
    public event Action BackClicked;

    #region Refs

    public enum Refs
    {
        AlbumBG_Image,

        BackButton_Button,

        Content_Root,

        Preview_Root,
        Preview_Image,
        PreviewButton_Button,
    }

    private Image _albumBg;

    private Button _backButton;

    private RectTransform _content;

    private CanvasGroup _previewGroup;
    private Image _previewImage;
    private Button _previewButton;

    #endregion

    [Header("Album")]
    [SerializeField]
    private VNAlbumSlotButton slotButtonPrefab;

    private readonly List<VNAlbumSlotButton>
        _slotButtons = new();

    private bool _valid;

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

        HidePreview();
    }

    private void CacheRefs()
    {
        _albumBg =
            View.Image(Refs.AlbumBG_Image);

        _backButton =
            View.Button(Refs.BackButton_Button);

        _content =
            View.Rect(Refs.Content_Root);

        _previewGroup =
            View.CanvasGroup(Refs.Preview_Root);

        _previewImage =
            View.Image(Refs.Preview_Image);

        _previewButton =
            View.Button(Refs.PreviewButton_Button);
    }

    private void BindHandlers()
    {
        BindEvent(
            _backButton,
            PressBackButton);

        BindEvent(
            _previewButton,
            PressPreviewButton);
    }

    #region Present

    public void Present(
        IReadOnlyList<AlbumEntryViewModel> entries)
    {
        if (!_valid)
            return;

        HidePreview();
        ClearSlots();

        if (entries == null)
            return;

        for (int i = 0; i < entries.Count; i++)
        {
            AlbumEntryViewModel entry = entries[i];

            if (entry == null)
                continue;

            SpawnSlot(entry);
        }
    }

    private void SpawnSlot(AlbumEntryViewModel entry)
    {
        VNAlbumSlotButton slot =
            Instantiate(
                slotButtonPrefab,
                _content);

        slot.EnsureInitialized();

        slot.Present(entry);
        slot.Clicked += HandleAlbumSlotClicked;

        _slotButtons.Add(slot);
    }

    private void ClearSlots()
    {
        for (int i = 0; i < _slotButtons.Count; i++)
        {
            VNAlbumSlotButton slot =
                _slotButtons[i];

            if (slot == null)
                continue;

            slot.Clicked -=
                HandleAlbumSlotClicked;

            Destroy(slot.gameObject);
        }

        _slotButtons.Clear();
    }

    #endregion

    #region Preview

    private void ShowPreview(Sprite sprite)
    {
        if (sprite == null)
        {
            HidePreview();
            return;
        }

        _previewImage.sprite = sprite;
        _previewImage.enabled = true;

        _previewGroup.alpha = 1f;
        _previewGroup.interactable = true;
        _previewGroup.blocksRaycasts = true;
    }

    private void HidePreview()
    {
        if (_previewGroup == null ||
            _previewImage == null)
        {
            return;
        }

        _previewImage.sprite = null;
        _previewImage.enabled = false;

        _previewGroup.alpha = 0f;
        _previewGroup.interactable = false;
        _previewGroup.blocksRaycasts = false;
    }

    #endregion

    #region Handlers

    private void PressBackButton(PointerEventData _)
    {
        // Preview가 떠 있다면 우선 Preview부터 닫는다.
        if (_previewGroup.alpha > 0f)
        {
            HidePreview();
            return;
        }

        BackClicked?.Invoke();
    }

    private void PressPreviewButton(PointerEventData _)
    {
        HidePreview();
    }

    private void HandleAlbumSlotClicked(
        AlbumEntryViewModel entry)
    {
        if (entry == null ||
            !entry.IsUnlocked)
        {
            return;
        }

        ShowPreview(entry.FullImage);
    }

    #endregion

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(
            ref missing,
            _albumBg,
            Refs.AlbumBG_Image);

        AppendMissing(
            ref missing,
            _backButton,
            Refs.BackButton_Button);

        AppendMissing(
            ref missing,
            _content,
            Refs.Content_Root);

        AppendMissing(
            ref missing,
            _previewGroup,
            Refs.Preview_Root);

        AppendMissing(
            ref missing,
            _previewImage,
            Refs.Preview_Image);

        AppendMissing(
            ref missing,
            _previewButton,
            Refs.PreviewButton_Button);

        if (slotButtonPrefab == null)
        {
            if (missing.Length > 0)
                missing += "\n";

            missing += "- slotButtonPrefab";
        }

        if (missing.Length > 0)
        {
            Debug.LogWarning(
                $"[AlbumUIRoot] Missing refs:\n{missing}",
                this);

            return false;
        }

        return true;
    }
}