#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게스트하우스 패널들의 계층을 Refs enum 으로부터 생성한다.
///
/// UIBase.BindObjects() 는 enum 멤버 이름과 '완전히 같은 이름'의 자식 GameObject 를 재귀 탐색해 묶는다.
/// 이름이 하나만 어긋나도 경고 없이 null 로 바인딩되므로, 손으로 만들면 오타 한 글자에 몇 시간이 날아간다.
/// 그래서 이름의 출처를 enum 하나로 고정하고 계층을 코드로 찍어낸다.
///
/// enum 을 나중에 고쳐도 이 도구를 다시 돌리면 부족한 자식만 채워진다.
/// 이미 있는 오브젝트는 건드리지 않으므로, 배치를 잡아 둔 뒤에 다시 실행해도 안전하다.
/// </summary>
public static class DungeonCafePanelScaffolder
{
    private const string MenuRoot = "Tools/Guesthouse/패널 계층 생성";

    [MenuItem(MenuRoot + "/전체 생성", priority = 0)]
    private static void CreateAll()
    {
        GameObject parent = ResolveParent();

        if (parent == null)
            return;

        // 진행 순서대로 만든다. 계층을 훑어볼 때 흐름이 그대로 읽힌다.
        Build<ReservationBoardPanel, ReservationBoardPanel.Refs>(parent);
        Build<MonsterCodexPanel, MonsterCodexPanel.Refs>(parent);
        Build<MaidAssignmentPanel, MaidAssignmentPanel.Refs>(parent);
        Build<MaidActionApprovalPanel, MaidActionApprovalPanel.Refs>(parent);
        Build<ServiceSettlementPanel, ServiceSettlementPanel.Refs>(parent);
        Build<DayReportPanel, DayReportPanel.Refs>(parent);
        Build<DepthPanel, DepthPanel.Refs>(parent);
        Build<NightPrepPanel, NightPrepPanel.Refs>(parent);
        Build<NightProgramPanel, NightProgramPanel.Refs>(parent);
        Build<CampaignEndingPanel, CampaignEndingPanel.Refs>(parent);

        Debug.LogWarning(
            "[DungeonCafePanelScaffolder] 패널 10종 생성 완료. "
            + "상태 오버레이는 별도 메뉴로 오버레이 레이어 아래에 만들어야 한다. "
            + "프리합 슬롯은 인스펙터에서 직접 지정한다.");
    }

    /// <summary>
    /// 오버레이는 패널 스택과 다른 레이어에 산다.
    /// UIManager 의 오버레이 레이어를 선택한 상태로 실행해야 한다.
    /// </summary>
    [MenuItem(MenuRoot + "/상태 오버레이", priority = 1)]
    private static void CreateOverlay()
        => BuildSingle<DungeonCafeStatusOverlay, DungeonCafeStatusOverlay.Refs>();

    [MenuItem(MenuRoot + "/예약 게시판", priority = 11)]
    private static void CreateBoard()
        => BuildSingle<ReservationBoardPanel, ReservationBoardPanel.Refs>();

    [MenuItem(MenuRoot + "/업무수첩", priority = 12)]
    private static void CreateCodex()
        => BuildSingle<MonsterCodexPanel, MonsterCodexPanel.Refs>();

    [MenuItem(MenuRoot + "/메이드 배정", priority = 13)]
    private static void CreateAssignment()
        => BuildSingle<MaidAssignmentPanel, MaidAssignmentPanel.Refs>();

    [MenuItem(MenuRoot + "/행동 승인", priority = 14)]
    private static void CreateApproval()
        => BuildSingle<MaidActionApprovalPanel, MaidActionApprovalPanel.Refs>();

    [MenuItem(MenuRoot + "/접객 결산", priority = 15)]
    private static void CreateSettlement()
        => BuildSingle<ServiceSettlementPanel, ServiceSettlementPanel.Refs>();

    [MenuItem(MenuRoot + "/하루 리포트", priority = 16)]
    private static void CreateDayReport()
        => BuildSingle<DayReportPanel, DayReportPanel.Refs>();

    [MenuItem(MenuRoot + "/붕괴심층", priority = 17)]
    private static void CreateDepth()
        => BuildSingle<DepthPanel, DepthPanel.Refs>();

    [MenuItem(MenuRoot + "/밤 상점", priority = 18)]
    private static void CreateNightPrep()
        => BuildSingle<NightPrepPanel, NightPrepPanel.Refs>();

    [MenuItem(MenuRoot + "/밤 처리", priority = 19)]
    private static void CreateNight()
        => BuildSingle<NightProgramPanel, NightProgramPanel.Refs>();

    [MenuItem(MenuRoot + "/엔딩", priority = 20)]
    private static void CreateEnding()
        => BuildSingle<CampaignEndingPanel, CampaignEndingPanel.Refs>();

    // ------------------------------------------------------------
    // 생성
    // ------------------------------------------------------------
    private static void BuildSingle<TPanel, TRefs>()
        where TPanel : MonoBehaviour
        where TRefs : struct, Enum
    {
        GameObject parent = ResolveParent();

        if (parent == null)
            return;

        Build<TPanel, TRefs>(parent);
    }

    private static void Build<TPanel, TRefs>(GameObject parent)
        where TPanel : MonoBehaviour
        where TRefs : struct, Enum
    {
        string panelName = typeof(TPanel).Name;

        GameObject root = FindChild(parent.transform, panelName);

        if (root == null)
        {
            root = NewUIObject(panelName, parent.transform);
            Undo.RegisterCreatedObjectUndo(root, "Create DungeonCafe Panel");
        }

        Stretch(root);

        EnsureComponent<CanvasGroup>(root);
        EnsureComponent<TPanel>(root);

        // 패널 자신은 UIManager 가 CanvasGroup 으로 껐다 켜므로 활성 상태로 둔다.
        // SetActive(false) 로 숨기면 Awake 가 돌지 않아 Refs 가 만들어지지 않는다.
        root.SetActive(true);

        Dictionary<string, Transform> made = new();

        foreach (string member in Enum.GetNames(typeof(TRefs)))
        {
            if (IsPrefabSlot(member))
                continue;

            Transform host = ResolveHost(root.transform, member, made);
            Transform child = EnsureChild(host, member);

            ApplyComponents(child.gameObject, member);
            made[member] = child;
        }

        EditorUtility.SetDirty(root);
    }

    /// <summary>`Header_MaidName_Text` 는 `Header_Root` 아래로 넣는다. 없으면 패널 직속.</summary>
    private static Transform ResolveHost(
        Transform panelRoot,
        string member,
        Dictionary<string, Transform> made)
    {
        int split = member.IndexOf('_');

        if (split <= 0)
            return panelRoot;

        string groupRoot = member[..split] + "_Root";

        if (groupRoot == member)
            return panelRoot;

        if (made.TryGetValue(groupRoot, out Transform host) && host != null)
            return host;

        Transform existing = FindChildTransform(panelRoot, groupRoot);

        return existing != null ? existing : panelRoot;
    }

    /// <summary>이름 접미사로 필요한 컴포넌트를 정한다. 규칙을 한 곳에 모아 둔다.</summary>
    private static void ApplyComponents(GameObject go, string member)
    {
        if (member.EndsWith("_Text", StringComparison.Ordinal))
        {
            TextMeshProUGUI text = EnsureComponent<TextMeshProUGUI>(go);
            text.text = member;
            text.fontSize = 28f;
            text.color = Color.white;
            Stretch(go);
            return;
        }

        if (member.EndsWith("_Image", StringComparison.Ordinal))
        {
            Image image = EnsureComponent<Image>(go);

            // 게이지는 fillAmount 로 그려야 하므로 Filled 타입이 필요하다.
            // Simple 이면 DungeonCafeStatusOverlay.SetGauge 가 조용히 무시된다.
            bool isGauge = member.EndsWith("_Gauge_Image", StringComparison.Ordinal);

            if (isGauge)
            {
                image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Horizontal;
                image.fillAmount = 0f;
                image.color = new Color(0.85f, 0.25f, 0.3f, 0.9f);
            }
            else
            {
                image.color = new Color(0f, 0f, 0f, 0.75f);
            }

            Stretch(go);
            return;
        }

        if (member.EndsWith("Button", StringComparison.Ordinal))
        {
            EnsureComponent<Image>(go);
            EnsureComponent<Button>(go);
            SetSize(go, new Vector2(240f, 72f));

            Transform label = EnsureChild(go.transform, "Label");
            TextMeshProUGUI text = EnsureComponent<TextMeshProUGUI>(label.gameObject);
            text.text = "확인";
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.black;
            Stretch(label.gameObject);
            return;
        }

        if (member.EndsWith("_Content", StringComparison.Ordinal))
        {
            BuildScrollContent(go);
            return;
        }

        // `*_Root` 를 포함한 나머지는 자리만 잡는 컨테이너다.
        Stretch(go);
    }

    /// <summary>
    /// 목록 컨테이너를 스크롤 구조로 감싼다.
    /// Refs 에는 Content 만 등록되어 있으므로 Viewport 와 ScrollRect 는 이름을 자유롭게 둔다.
    /// </summary>
    private static void BuildScrollContent(GameObject content)
    {
        Transform parent = content.transform.parent;

        if (parent == null)
            return;

        // Content 는 Viewport 아래에 있어야 마스킹이 걸린다.
        Transform viewport = FindChildTransform(parent, "Viewport");

        if (viewport == null)
        {
            GameObject created = NewUIObject("Viewport", parent);
            viewport = created.transform;
            Undo.RegisterCreatedObjectUndo(created, "Create Viewport");
        }

        Stretch(viewport.gameObject);
        EnsureComponent<Image>(viewport.gameObject).color = new Color(1f, 1f, 1f, 0.01f);
        EnsureComponent<Mask>(viewport.gameObject).showMaskGraphic = false;

        if (content.transform.parent != viewport)
            content.transform.SetParent(viewport, worldPositionStays: false);

        RectTransform rect = content.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = EnsureComponent<VerticalLayoutGroup>(content);
        layout.spacing = 8f;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = EnsureComponent<ContentSizeFitter>(content);
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = EnsureComponent<ScrollRect>(parent.gameObject);
        scroll.content = rect;
        scroll.viewport = viewport as RectTransform;
        scroll.horizontal = false;
        scroll.vertical = true;
    }

    // ------------------------------------------------------------
    // 유틸
    // ------------------------------------------------------------
    private static GameObject ResolveParent()
    {
        GameObject selected = Selection.activeGameObject;

        if (selected == null)
        {
            EditorUtility.DisplayDialog(
                "부모를 선택해 주십시오",
                "UIManager 하위의 패널 레이어를 선택한 뒤 다시 실행해 주십시오.\n\n"
                + "UIManager 는 자기 하위의 IManagedUI 를 전부 자동 등록하므로,\n"
                + "패널은 그 아래 어디에 두어도 됩니다.",
                "확인");

            return null;
        }

        if (selected.GetComponentInParent<Canvas>() == null)
        {
            Debug.LogWarning(
                "[DungeonCafePanelScaffolder] 선택한 오브젝트가 Canvas 아래에 없다. "
                + "UI 로 동작하려면 Canvas 하위여야 한다.",
                selected);
        }

        return selected;
    }

    private static GameObject NewUIObject(string name, Transform parent)
    {
        GameObject go = new(name, typeof(RectTransform));
        go.transform.SetParent(parent, worldPositionStays: false);
        return go;
    }

    private static Transform EnsureChild(Transform parent, string name)
    {
        Transform found = FindChildTransform(parent, name);

        if (found != null)
            return found;

        GameObject created = NewUIObject(name, parent);
        Undo.RegisterCreatedObjectUndo(created, "Create Ref Object");

        return created.transform;
    }

    /// <summary>UIBase 와 같은 규칙으로 찾는다. 재귀 탐색, 비활성 포함.</summary>
    private static Transform FindChildTransform(Transform parent, string name)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name == name)
                return child;

            Transform deep = FindChildTransform(child, name);

            if (deep != null)
                return deep;
        }

        return null;
    }

    private static GameObject FindChild(Transform parent, string name)
    {
        Transform found = FindChildTransform(parent, name);
        return found != null ? found.gameObject : null;
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        if (!go.TryGetComponent(out T component))
            component = Undo.AddComponent<T>(go);

        return component;
    }

    private static void Stretch(GameObject go)
    {
        if (!go.TryGetComponent(out RectTransform rect))
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetSize(GameObject go, Vector2 size)
    {
        if (!go.TryGetComponent(out RectTransform rect))
            return;

        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = size;
    }

    /// <summary>프리팹 슬롯은 씬 자식이 아니라 인스펙터에서 지정한다.</summary>
    private static bool IsPrefabSlot(string member)
        => member.EndsWith("Prefab", StringComparison.Ordinal);
}
#endif
