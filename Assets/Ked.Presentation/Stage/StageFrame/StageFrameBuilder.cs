using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// 무대 틀 — 무대 루트 바깥의 창 · 컷인 판 · 그림 팝업을 세우고, 있는 것은 다시 묶어 슬롯 둘을 돌려준다.
/// 무대 리그가 먼저 서 있어야 한다 — 창이 그 무대 루트를 감싼다. 틀은 리그를 알고, 리그는 틀을 모른다.
///
///   {stage}_Window              무대 전체를 들고 팝 · 회전한다(컷인). 중립 항등 — 무대 루트와 같은 rect
///     {stage}_Panel             컷인 판 그림. 무대 루트보다 뒤(형제 0)
///     {stage}_Stage_Root        (무대 리그)
///   {stage}_Popup               창의 다음 형제. 뒷막과 그림을 한 덩어리로(CanvasGroup)
///     {stage}_Popup_Backdrop    화면 전체 어두운 막
///     {stage}_Popup_Image       그림
///
/// 노드 이름은 씬에 저장된 것을 다시 찾기 위한 것이다 — 바깥(대본 · 폴드)이 이름으로 부르지 않는다.
/// 무대 리그와 달리 부수지 않는다. 어긋난 자식만 경고하고 치우고, 묶을 때마다 자리와 중립을 다시 세운다.
/// </summary>
public sealed class StageFrameBuilder
{
    public void Build(StageRigRefs stage, out StageCutInSlot cutIn, out StagePopupSlot popup)
    {
        RectTransform window = EnsureWindow(stage.Root, stage.StageKey);
        cutIn = EnsureCutIn(window, stage.Root, stage.StageKey, stage.Mask);
        popup = EnsurePopup(window, stage.StageKey);
    }

    private static string WindowName(string stageKey) => $"{stageKey}_Window";
    private static string PanelName(string stageKey) => $"{stageKey}_Panel";
    private static string PopupName(string stageKey) => $"{stageKey}_Popup";
    private static string PopupBackdropName(string stageKey) => $"{stageKey}_Popup_Backdrop";
    private static string PopupImageName(string stageKey) => $"{stageKey}_Popup_Image";

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

    // 창 아래는 판과 무대 루트 둘뿐이다. 판이 뒤(형제 0), 무대가 앞(형제 1). 묶을 때마다 닫힌 중립으로 돌린다.
    private static StageCutInSlot EnsureCutIn(RectTransform window, RectTransform root, string stageKey, StageMaskSlot mask)
    {
        RectTransform panel = EnsureRect(window, PanelName(stageKey));

        // 스키마에 없는 자식은 경고하고 치운다 — 창 아래는 판과 무대 루트뿐이다.
        for (int i = window.childCount - 1; i >= 0; i--)
        {
            Transform child = window.GetChild(i);

            if (child == panel || child == root)
                continue;

            Debug.LogWarning($"[StageFrameBuilder] 창 아래에 스키마에 없는 '{child.name}'가 있다. 치운다. stage='{stageKey}'.", window);
            DestroyNode(child);
        }

        panel.SetSiblingIndex(0);
        root.SetSiblingIndex(1);

        if (!panel.TryGetComponent(out Image image))
            image = panel.gameObject.AddComponent<Image>();

        image.raycastTarget = false;

        StageCutInSlot slot = new(stageKey, window, panel, image, mask);
        slot.Reset();

        return slot;
    }

    // 그림 팝업은 창의 **다음 형제**다 — 그 무대의 내용 위, 다음 무대 아래. 창 밖이라 컷인 팝(창 배율 · 회전)에 끌려가지 않는다.
    // 그룹 아래는 뒷막(형제 0)과 그림(형제 1)뿐이다. 그룹의 불투명도 하나가 둘을 같이 페이드한다(docs/PLAN_popup.md §2).
    // 묶을 때마다 닫힌 중립으로 돌린다.
    private static StagePopupSlot EnsurePopup(RectTransform window, string stageKey)
    {
        RectTransform mount = window.parent as RectTransform;
        string groupName = PopupName(stageKey);

        RectTransform group = mount != null ? FindDirectChild(mount, groupName) : null;

        if (group == null)
        {
            GameObject go = new(groupName, typeof(RectTransform));
            group = (RectTransform)go.transform;
            group.SetParent(mount, false);
        }

        StretchFull(group);

        // 창 바로 다음 자리 — 먼저 맨 뒤로 보낸 뒤 창 번호를 읽는다(그룹이 창 앞에 있었으면 창 번호가 하나 줄어든다).
        if (mount != null)
        {
            group.SetAsLastSibling();
            group.SetSiblingIndex(window.GetSiblingIndex() + 1);
        }

        RectTransform backdrop = EnsureRect(group, PopupBackdropName(stageKey));
        RectTransform picture = EnsureRect(group, PopupImageName(stageKey));

        // 스키마에 없는 자식은 경고하고 치운다 — 그룹 아래는 뒷막과 그림뿐이다.
        for (int i = group.childCount - 1; i >= 0; i--)
        {
            Transform child = group.GetChild(i);

            if (child == backdrop || child == picture)
                continue;

            Debug.LogWarning($"[StageFrameBuilder] 그림 팝업 아래에 스키마에 없는 '{child.name}'가 있다. 치운다. stage='{stageKey}'.", group);
            DestroyNode(child);
        }

        backdrop.SetSiblingIndex(0);
        picture.SetSiblingIndex(1);
        StretchFull(backdrop);

        if (!group.TryGetComponent(out CanvasGroup canvasGroup))
            canvasGroup = group.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (!backdrop.TryGetComponent(out Image backdropImage))
            backdropImage = backdrop.gameObject.AddComponent<Image>();

        if (!picture.TryGetComponent(out Image pictureImage))
            pictureImage = picture.gameObject.AddComponent<Image>();

        backdropImage.raycastTarget = false;
        pictureImage.raycastTarget = false;

        StagePopupSlot slot = new(stageKey, group, canvasGroup, backdropImage, picture, pictureImage);
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

    private static RectTransform EnsureRect(RectTransform parent, string name)
    {
        RectTransform existing = FindDirectChild(parent, name);

        if (existing != null)
            return existing;

        GameObject go = new(name, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        StretchFull(rt);

        return rt;
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

    private static RectTransform FindDirectChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name == name)
                return (RectTransform)child;
        }

        return null;
    }
}
