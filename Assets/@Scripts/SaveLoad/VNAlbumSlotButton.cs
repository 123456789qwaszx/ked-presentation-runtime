using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class VNAlbumSlotButton : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _thumbnailImage;
    [SerializeField] private TMP_Text _titleLabel;
    [SerializeField] private GameObject _lockedOverlay;

    private VNAlbumItemSO _item;
    private bool _unlocked;
    private Action<VNAlbumItemSO> _onClick;

    private void Reset()
    {
        _button = GetComponent<Button>();
    }

    public void Bind(
        VNAlbumItemSO item,
        bool unlocked,
        Action<VNAlbumItemSO> onClick)
    {
        _item = item;
        _unlocked = unlocked;
        _onClick = onClick;

        if (_thumbnailImage != null)
        {
            _thumbnailImage.sprite = unlocked && item != null ? item.GetThumbnail() : null;
            _thumbnailImage.enabled = unlocked && item != null && item.GetThumbnail() != null;
        }

        if (_titleLabel != null)
            _titleLabel.text = unlocked && item != null ? item.title : "???";

        if (_lockedOverlay != null)
            _lockedOverlay.SetActive(!unlocked);

        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleClick);
            _button.onClick.AddListener(HandleClick);
            _button.interactable = unlocked;
        }
    }

    private void HandleClick()
    {
        if (!_unlocked)
            return;

        _onClick?.Invoke(_item);
    }
}