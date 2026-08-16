// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;
// using static UIRefValidation;
//
// public sealed class AlbumMenuPanel : UIPanel<AlbumMenuPanel.Refs>
// {
//     public event Action CloseClicked;
//
//     #region Refs
//
//     public enum Refs
//     {
//         Preview_Root,
//         Preview_Image,
//         Preview_Button,
//
//         ContentRoot_Rect,
//
//         CloseButton_BWidget,
//     }
//
//     [Header("Prefab")]
//     [SerializeField] private VNAlbumSlotButton _slotButtonPrefab;
//
//     private Image _albumBg;
//
//     private CanvasGroup _previewGroup;
//     private Image _previewImage;
//
//     private RectTransform _contentRoot;
//
//     private ButtonWidget _close;
//
//     #endregion
//
//     private bool _valid;
//
//     protected override void OnInitialize()
//     {
//         _previewGroup = View.CanvasGroup(Refs.Preview_Root);
//         _previewImage = View.Image(Refs.Preview_Image);
//
//         _contentRoot = View.Rect(Refs.ContentRoot_Rect);
//
//         _close = View.Widget<ButtonWidget>(Refs.CloseButton_BWidget);
//
// #if UNITY_EDITOR || DEVELOPMENT_BUILD
//         _valid = ValidateRefs();
//         if (!_valid)
//             return;
// #else
//         _valid = true;
// #endif
//
//         _close.SetLabel("돌아가기");
//         _close.OnClicked -= HandleClose;
//         _close.OnClicked += HandleClose;
//
//         View.Button(Refs.Preview_Button).onClick.RemoveListener(HidePreview);
//         View.Button(Refs.Preview_Button).onClick.AddListener(HidePreview);
//
//         HidePreview();
//     }
//
//     public void Rebuild(IReadOnlyList<VNAlbumItemSO> items, Func<string, bool> isUnlocked)
//     {
//         if (!_valid)
//             return;
//
//         ClearSlots();
//         HidePreview();
//
//         if (items == null)
//             return;
//
//         for (int i = 0; i < items.Count; i++)
//         {
//             VNAlbumItemSO item = items[i];
//
//             if (item == null || item.key == null)
//                 continue;
//
//             bool unlocked = isUnlocked != null && isUnlocked(item.key);
//
//             VNAlbumSlotButton button = Instantiate(_slotButtonPrefab, _contentRoot);
//             button.Bind(item, unlocked, HandleAlbumItemClicked);
//         }
//     }
//
//     private void ClearSlots()
//     {
//         for (int i = _contentRoot.childCount - 1; i >= 0; i--)
//             Destroy(_contentRoot.GetChild(i).gameObject);
//     }
//
//     private void ShowPreview(Sprite sprite)
//     {
//         if (sprite == null)
//         {
//             HidePreview();
//             return;
//         }
//
//         _previewImage.sprite = sprite;
//         _previewImage.enabled = true;
//
//         _previewGroup.alpha = 1f;
//         _previewGroup.interactable = true;
//         _previewGroup.blocksRaycasts = true;
//     }
//
//     private void HidePreview()
//     {
//         _previewImage.sprite = null;
//         _previewImage.enabled = false;
//
//         _previewGroup.alpha = 0f;
//         _previewGroup.interactable = false;
//         _previewGroup.blocksRaycasts = false;
//     }
//
//     private void HandleClose()
//     {
//         CloseClicked?.Invoke();
//     }
//
//     private void HandleAlbumItemClicked(VNAlbumItemSO item)
//     {
//         if (!_valid)
//             return;
//
//         ShowPreview(item != null ? item.cgSprite : null);
//     }
//
//     private bool ValidateRefs()
//     {
//         string missing = "";
//
//         AppendMissing(ref missing, _previewGroup, Refs.Preview_Root);
//         AppendMissing(ref missing, _previewImage, Refs.Preview_Image);
//
//         AppendMissing(ref missing, _contentRoot, Refs.ContentRoot_Rect);
//
//         AppendMissing(ref missing, _close, Refs.CloseButton_BWidget);
//
//         if (missing.Length > 0)
//         {
//             Debug.LogWarning($"[AlbumUIRoot] Missing refs:\n{missing}", this);
//             return false;
//         }
//
//         return true;
//     }
// }