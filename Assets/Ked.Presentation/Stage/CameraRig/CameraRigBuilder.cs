using UnityEngine;

/// <summary>
/// 카메라 리그를 스키마대로 세우고, 있는 것은 검증한다. 무대를 모른다 — 부모 하나와 무대 키만 받는다.
/// 부수고 다시 세우는 것은 부르는 쪽(StageRigBuilder)의 일이다: 무대가 통째로 한 단위라서,
/// 카메라만 어긋나도 무대 전체를 다시 세운다.
/// </summary>
public sealed class CameraRigBuilder
{
    /// <summary>parent 아래에 카메라 리그를 세운다. 있는 노드는 그대로 묶는다(먼저 IsValid로 검증한 계층이거나, 빈 부모다).</summary>
    public CameraRigRefs Build(RectTransform parent, string stageKey)
    {
        RectTransform[] rects = new RectTransform[CameraRigSchema.Nodes.Length];

        foreach (CameraRigSchema.NodeDef node in CameraRigSchema.Nodes)
        {
            RectTransform nodeParent = node.Parent.HasValue ? rects[(int)node.Parent.Value] : parent;
            rects[(int)node.Id] = EnsureRect(nodeParent, CameraRigSchema.NodeName(stageKey, node.Id));
        }

        return new CameraRigRefs(rects);
    }

    /// <summary>
    /// parent 아래의 계층이 스키마 그대로인가 — 사슬이 빠짐없이 서 있고, 전부 항등 stretch이고,
    /// 마운트를 뺀 노드마다 자식이 다음 노드 하나뿐이다. 마운트의 자식(층)은 무대 리그가 본다.
    /// </summary>
    public bool IsValid(RectTransform parent, string stageKey, out RectTransform mount)
    {
        mount = null;

        RectTransform current = parent;

        foreach (CameraRigSchema.NodeDef node in CameraRigSchema.Nodes)
        {
            if (current.childCount != 1)
                return false;

            RectTransform child = current.GetChild(0) as RectTransform;

            if (child == null || child.name != CameraRigSchema.NodeName(stageKey, node.Id) || !IsIdentityStretch(child))
                return false;

            current = child;
        }

        mount = current;
        return true;
    }

    private static RectTransform EnsureRect(RectTransform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name == name)
                return (RectTransform)child;
        }

        GameObject go = new(name, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localEulerAngles = Vector3.zero;

        return rt;
    }

    private static bool IsIdentityStretch(RectTransform rect)
        => rect.anchorMin == Vector2.zero
           && rect.anchorMax == Vector2.one
           && rect.pivot == new Vector2(0.5f, 0.5f)
           && rect.anchoredPosition == Vector2.zero
           && rect.sizeDelta == Vector2.zero
           && rect.localScale == Vector3.one
           && rect.localEulerAngles == Vector3.zero;
}
