using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

public sealed class AlbumMenuPanel : UIPanel<AlbumMenuPanel.Refs>
{
    public event Action CloseClicked;

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

    protected override void OnInitialize()
    {
        _albumBg = View.Image(Refs.AlbumBG_Image);
        _previewImage = View.Image(Refs.Preview_Image);

        _contentRoot = View.Rect(Refs.ContentRoot_Rect);

        _close = View.Widget<ButtonWidget>(Refs.CloseButton_BWidget);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid)
            return;
#else
        _valid = true;
#endif

        _close.SetLabel("돌아가기");
        _close.OnClicked -= HandleClose;
        _close.OnClicked += HandleClose;

        _previewImage.sprite = null;
        _previewImage.enabled = false;
    }


    public void Rebuild(IReadOnlyList<VNAlbumItemSO> items, Func<string, bool> isUnlocked)
    {
        if (!_valid)
            return;

        for (int i = _contentRoot.childCount - 1; i >= 0; i--)
            Destroy(_contentRoot.GetChild(i).gameObject);
        
        _previewImage.sprite = null;
        _previewImage.enabled = false;

        if (items == null)
            return;

        for (int i = 0; i < items.Count; i++)
        {
            VNAlbumItemSO item = items[i];
            
            if (item == null || item.key == null)
                continue;

            bool unlocked = isUnlocked(item.key);

            VNAlbumSlotButton button = Instantiate(_slotButtonPrefab, _contentRoot);
            button.Bind(item, unlocked, HandleAlbumItemClicked);
        }
    }
    
    private void HandleClose() => CloseClicked?.Invoke();

    private void HandleAlbumItemClicked(VNAlbumItemSO item)
    {
        if (!_valid)
            return;

        _previewImage.sprite = item.cgSprite;
        _previewImage.enabled = item.cgSprite != null;
    }
    
    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _albumBg, Refs.AlbumBG_Image);
        AppendMissing(ref missing, _previewImage, Refs.Preview_Image);

        AppendMissing(ref missing, _contentRoot, Refs.ContentRoot_Rect);

        AppendMissing(ref missing, _close, Refs.CloseButton_BWidget);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[AlbumUIRoot] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}