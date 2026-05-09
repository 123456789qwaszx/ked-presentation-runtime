using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

public sealed class AlbumUIRoot : UIRoot<AlbumUIRoot.Refs>
{
    public event Action OnCloseRequested;

    #region Refs

    public enum Refs
    {
        AlbumBG_Image,
        Preview_Image,

        ContentRoot_Rect,

        CloseButton_BWidget,
    }

    [Header("Prefab")]
    [SerializeField] private VNAlbumSlotButton _slotButtonPrefab;

    private Image _albumBg;
    private Image _previewImage;

    private RectTransform _contentRoot;

    private ButtonWidget _close;

    #endregion

    private bool _valid;

    protected override void Initialize()
    {
        _albumBg = View.Image(Refs.AlbumBG_Image);
        _previewImage = View.Image(Refs.Preview_Image);

        _contentRoot = View.Rect(Refs.ContentRoot_Rect);

        _close = View.Widget<ButtonWidget>(Refs.CloseButton_BWidget);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif

        _close.SetLabel("돌아가기");
        _close.OnClicked -= HandleClose;
        _close.OnClicked += HandleClose;

        ClearPreview();
    }

    #region Event Handlers

    private void HandleClose()
    {
        OnCloseRequested?.Invoke();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (!_valid) return;

        _close.OnClicked -= HandleClose;
    }

    #endregion

    public void Rebuild(IReadOnlyList<VNAlbumItemSO> items, Func<string, bool> isUnlocked)
    {
        if (!_valid) return;

        ClearSlots();
        ClearPreview();

        if (items == null)
            return;

        for (int i = 0; i < items.Count; i++)
        {
            VNAlbumItemSO item = items[i];

            if (item == null)
                continue;

            bool unlocked = isUnlocked != null && isUnlocked(item.key);

            VNAlbumSlotButton button = Instantiate(_slotButtonPrefab, _contentRoot);
            button.Bind(item, unlocked, HandleAlbumItemClicked);
        }
    }

    private void HandleAlbumItemClicked(VNAlbumItemSO item)
    {
        if (!_valid) return;
        if (item == null) return;

        SetPreview(item.cgSprite);
    }

    private void SetPreview(Sprite sprite)
    {
        if (_previewImage == null)
            return;

        _previewImage.sprite = sprite;
        _previewImage.enabled = sprite != null;
    }

    private void ClearPreview()
    {
        if (_previewImage == null)
            return;

        _previewImage.sprite = null;
        _previewImage.enabled = false;
    }

    private void ClearSlots()
    {
        if (_contentRoot == null)
            return;

        for (int i = _contentRoot.childCount - 1; i >= 0; i--)
            Destroy(_contentRoot.GetChild(i).gameObject);
    }

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _albumBg, Refs.AlbumBG_Image);
        AppendMissing(ref missing, _previewImage, Refs.Preview_Image);

        AppendMissing(ref missing, _contentRoot, Refs.ContentRoot_Rect);

        AppendMissing(ref missing, _close, Refs.CloseButton_BWidget);
        //AppendMissing(ref missing, _slotButtonPrefab, nameof(_slotButtonPrefab));

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[AlbumUIRoot] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}