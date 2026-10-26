using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ked.Samples.UI.Editor
{
    /// <summary>
    /// 선택지 항목 — 본체의 VNOptionItem을 새 그림(choice_normal, 760×74)으로 감싼 프리팹. 선택 가능·불가는 재생 쪽이
    /// 정하고(VNOptionItem.Bind), 이 프리팹은 그 상태를 그린다: 포커스 = 청록 테두리, 불가 = 투명도.
    /// </summary>
    internal static class OptionItemSpec
    {
        public static GameObject Build(KedUIKit kit)
        {
            var go = new GameObject("KedOptionItem", typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(760f, 74f);

            var root = new KedNode(rect, new Rect(420, 310, 760, 74));

            Image background = kit.Image(root, "choice_normal", raycast: true);
            CanvasGroup group = go.AddComponent<CanvasGroup>();

            LayoutElement element = go.AddComponent<LayoutElement>();
            element.preferredWidth = 760f;
            element.preferredHeight = 74f;

            KedNode label = kit.Fill(root, "Label", 40f, 6f, 50f, 8f);
            TextMeshProUGUI labelText = kit.Text(label, "", 25, KedColors.Navy, TextAlignmentOptions.Center, bold: true,
                overflow: TextOverflowModes.Ellipsis);

            KedNode effect = kit.Node(root, "EffectText", 940, 360, 225, 20);
            TextMeshProUGUI effectText = kit.Text(effect, "", 13, KedColors.Muted, TextAlignmentOptions.Right);

            KedNode indicator = kit.Fill(root, "SelectionIndicator", -4f, -4f, -4f, -4f);
            kit.Outline(indicator, KedColors.Turquoise, 3f);
            indicator.Go.SetActive(false);

            VNOptionItem item = go.AddComponent<VNOptionItem>();
            item.transition = Selectable.Transition.None;
            item.targetGraphic = background;

            var so = new SerializedObject(item);
            Set(so, "_label", labelText);
            Set(so, "_effectText", effectText);
            Set(so, "_canvasGroup", group);
            Set(so, "_selectionIndicator", indicator.Go);
            SetFloat(so, "_disabledAlpha", 0.45f);
            SetFloat(so, "_normalAlpha", 1f);
            SetFloat(so, "_hoveredAlpha", 1f);
            so.ApplyModifiedPropertiesWithoutUndo();

            return go;
        }

        private static void Set(SerializedObject so, string name, Object value)
        {
            SerializedProperty property = so.FindProperty(name);
            if (property == null)
            {
                Debug.LogWarning($"[Ked UI 빌더] VNOptionItem.{name} 칸이 없다 — 본체가 바뀌었는지 확인한다.");
                return;
            }

            property.objectReferenceValue = value;
        }

        private static void SetFloat(SerializedObject so, string name, float value)
        {
            SerializedProperty property = so.FindProperty(name);
            if (property == null)
            {
                Debug.LogWarning($"[Ked UI 빌더] VNOptionItem.{name} 칸이 없다 — 본체가 바뀌었는지 확인한다.");
                return;
            }

            property.floatValue = value;
        }
    }
}
