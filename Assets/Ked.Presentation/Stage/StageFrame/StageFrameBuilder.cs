using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 무대 틀 — 무대 루트 바깥의 창을 세우고, 있는 것은 다시 묶어 컷인 슬롯을 돌려준다.
/// 무대 리그가 먼저 서 있어야 한다 — 창이 그 무대 루트를 감싼다. 틀은 리그를 알고, 리그는 틀을 모른다.
///
///   {stage}_Window              무대 전체를 들고 팝 · 회전한다(컷인). 중립 항등 — 무대 루트와 같은 rect
///     {stage}_Stage_Root        (무대 리그) — 컷인 판 · 팝업 그림은 이 안의 리그다(docs/PLAN_stage-visibility.md)
///
/// 노드 이름은 씬에 저장된 것을 다시 찾기 위한 것이다 — 바깥(대본 · 폴드)이 이름으로 부르지 않는다.
/// 무대 리그와 달리 부수지 않는다. 어긋난 자식만 경고하고 치우고, 묶을 때마다 중립을 다시 세운다.
/// </summary>
public static class StageFrameBuilder
{
    public static StageCutInSlot Build(StageRigRefs stage)
    {
        RectTransform window = EnsureWindow(stage.Root, stage.StageKey);
        return EnsureCutIn(window, stage.Root, stage.StageKey, stage.Mask);
    }

    private static string WindowName(string stageKey) => $"{stageKey}_Window";

    // 창은 무대 루트의 **부모**다 — 무대 전체를 들고 움직이되 리그 좌표 계산(무대 루트까지)에는 들지 않는다.
    // 옛 씬(무대 루트가 mount 바로 아래)은 그 자리에 창을 끼워 넣는다 — 부수지 않는다(무대 루트는 창과 같은 rect로 선다).
    private static RectTransform EnsureWindow(RectTransform root, string stageKey)
    {
        string windowName = WindowName(stageKey);
        RectTransform parent = root.parent as RectTransform;

        if (parent != null && parent.name == windowName)
            return parent;

        GameObject go = new(windowName, typeof(RectTransform));
        RectTransform window = (RectTransform)go.transform;
        window.SetParent(parent, false);

        if (parent != null)
            window.SetSiblingIndex(root.GetSiblingIndex());

        StretchFull(window);
        root.SetParent(window, false);
        StretchFull(root);

        return window;
    }

    // 창 아래는 무대 루트 하나뿐이다. 묶을 때마다 중립으로 돌린다.
    private static StageCutInSlot EnsureCutIn(RectTransform window, RectTransform root, string stageKey, StageMaskSlot mask)
    {
        // 스키마에 없는 자식은 경고하고 치운다 — 창 아래는 무대 루트뿐이다(컷인 판 그림은 무대 안의 리그다).
        for (int i = window.childCount - 1; i >= 0; i--)
        {
            Transform child = window.GetChild(i);

            if (child == root)
                continue;

            Debug.LogWarning($"[StageFrameBuilder] 창 아래에 스키마에 없는 '{child.name}'가 있다. 치운다. stage='{stageKey}'.", window);
            DestroyNode(child);
        }

        StageCutInSlot slot = new(stageKey, window, mask);
        slot.Reset();

        return slot;
    }

    // 파괴 전에 SetParent(null)로 뗀다 — 같은 프레임의 이름 찾기가 삭제 예정 노드를 다시 잡지 않게(StageRigBuilder와 같은 이유, b470fc88).
    private static void DestroyNode(Transform node)
    {
        node.SetParent(null, false);

        if (Application.isPlaying)
            Object.Destroy(node.gameObject);
        else
            Object.DestroyImmediate(node.gameObject);
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localEulerAngles = Vector3.zero;
    }
}
