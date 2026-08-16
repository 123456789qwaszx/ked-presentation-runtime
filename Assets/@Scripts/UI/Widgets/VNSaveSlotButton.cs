// using System;
// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;
//
// public sealed class VNSaveSlotButton : MonoBehaviour
// {
//     [SerializeField] private Button _button;
//
//     [Header("Texts")]
//     [SerializeField] private TMP_Text _slotLabel;
//     [SerializeField] private TMP_Text _chapterLabel;
//     [SerializeField] private TMP_Text _previewLabel;
//     [SerializeField] private TMP_Text _timeLabel;
//     [SerializeField] private TMP_Text _playtimeLabel;
//
//     private int _slotIndex;
//     private Action<int> _onClick;
//
//     private void Reset()
//     {
//         _button = GetComponent<Button>();
//     }
//
//     public void Bind(int slotIndex, VNSaveSlotMeta meta, bool isSaveMode, Action<int> onClick)
//     {
//         _slotIndex = slotIndex;
//         _onClick = onClick;
//
//         if (meta == null)
//             meta = VNSaveSlotMeta.Empty("");
//
//         if (_slotLabel != null)
//             _slotLabel.text = $"Slot {slotIndex:D2}";
//
//         if (_chapterLabel != null)
//             _chapterLabel.text = meta.isEmpty ? "Empty" : meta.chapterLabel;
//
//         if (_previewLabel != null)
//             _previewLabel.text = meta.isEmpty ? "" : meta.linePreview;
//
//         if (_timeLabel != null)
//             _timeLabel.text = meta.isEmpty ? "" : meta.savedAt;
//
//         if (_playtimeLabel != null)
//             _playtimeLabel.text = meta.isEmpty ? "" : meta.FormatPlaytime();
//
//         if (_button != null)
//         {
//             _button.onClick.RemoveListener(HandleClick);
//             _button.onClick.AddListener(HandleClick);
//
//             // Load 모드에서는 빈 슬롯 비활성화.
//             _button.interactable = isSaveMode || !meta.isEmpty;
//         }
//     }
//
//     private void HandleClick()
//     {
//         _onClick?.Invoke(_slotIndex);
//     }
// }