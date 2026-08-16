// using System;
// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;
//
// public sealed class MessengerOptionButtonView : MonoBehaviour
// {
//     [Header("Refs")]
//     [SerializeField] private Button button;
//     [SerializeField] private TMP_Text label;
//
//     private Action _onClick;
//
//     public void SetText(string text)
//     {
//         if (label != null)
//             label.text = text ?? string.Empty;
//     }
//
//     public void SetOnClick(Action onClick)
//     {
//         _onClick = onClick;
//
//         if (button == null)
//             return;
//
//         button.onClick.RemoveAllListeners();
//         button.onClick.AddListener(OnClicked);
//     }
//
//     public void SetInteractable(bool interactable)
//     {
//         if (button != null)
//             button.interactable = interactable;
//     }
//
//     public void OnClicked()
//     {
//         _onClick?.Invoke();
//     }
// }