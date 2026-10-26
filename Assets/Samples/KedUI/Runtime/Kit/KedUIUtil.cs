using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ked.Samples.UI
{
    internal static class KedUIUtil
    {
        // 클릭(포인터·키보드 Submit 모두)을 한 번 묶는다. 화면의 OnInitialize에서만 부른다 — 열 때마다 묶지 않는다.
        public static void OnClick(Button button, Action action)
        {
            if (button == null || action == null)
                return;

            button.onClick.AddListener(() => action());
        }

        public static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value ?? string.Empty;
        }

        public static void SetActive(Component component, bool active)
        {
            if (component != null && component.gameObject.activeSelf != active)
                component.gameObject.SetActive(active);
        }

        public static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
                go.SetActive(active);
        }

        // 버튼을 보이고/감추고, 막고/연다. 그림 상태(KedButton)가 있으면 그것을 거쳐 투명도도 맞춘다.
        public static void SetButton(Button button, bool visible, bool interactable)
        {
            if (button == null)
                return;

            SetActive(button, visible);

            if (button.TryGetComponent(out KedButton look))
                look.SetInteractable(interactable);
            else
                button.interactable = interactable;
        }

        public static void SetCanvasGroup(CanvasGroup group, bool visible, bool interactive)
        {
            if (group == null)
                return;

            group.alpha = visible ? 1f : 0f;
            group.interactable = visible && interactive;
            group.blocksRaycasts = visible && interactive;
        }

        // 키보드로 고를 첫 버튼. 포인터로 연 화면에서도 Space·Enter가 엉뚱한(아래 화면의) 버튼에 가지 않게 한다.
        public static void Focus(Selectable selectable)
        {
            EventSystem system = EventSystem.current;
            if (system == null)
                return;

            system.SetSelectedGameObject(
                selectable != null && selectable.isActiveAndEnabled && selectable.IsInteractable()
                    ? selectable.gameObject
                    : null);
        }

        public static void ClearFocus()
        {
            EventSystem system = EventSystem.current;
            if (system != null)
                system.SetSelectedGameObject(null);
        }

        // 두 자리 슬롯 번호("01").
        public static string TwoDigits(int number) => number.ToString("00");

        // UI 글자 표(Localizer)에서 읽는다. 표가 아직 없으면 키를 그대로 — 빠진 글자가 화면에서 바로 보이게.
        public static string Text(Localizer texts, string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            return texts != null ? texts.Get(key) : key;
        }

        public static string Format(Localizer texts, string key, params object[] args)
        {
            return texts != null ? texts.Format(key, args) : key;
        }
    }
}
