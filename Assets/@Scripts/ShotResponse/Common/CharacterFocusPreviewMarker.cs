#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class CharacterFocusPreviewMarker : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════
    //  Inner Types
    // ═══════════════════════════════════════════════════════════════

    [Serializable]
    public sealed class Point
    {
        public bool enabled = true;

        [Header("Identity")]
        public string label = "";

        [Header("Focus")]
        public CharacterFocusPreset focusPreset = CharacterFocusPreset.Face;

        [Tooltip("focusPreset이 Custom일 때 사용할 custom point key입니다. 예: hand_right, phone, weapon")]
        public string customFocusKey = "";

        [Header("Visual")]
        public Color color = Color.white;
        public float radius = 12f;

        [Header("Final Offset")]
        [Tooltip("프리셋/DB 보정 후 이 포인트에만 마지막으로 더하는 수동 보정값입니다.")]
        public Vector2 finalOffset = Vector2.zero;

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(label))
                    return label.Trim();

                if (focusPreset == CharacterFocusPreset.Custom && !string.IsNullOrWhiteSpace(customFocusKey))
                    return customFocusKey.Trim();

                return focusPreset.ToString();
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Serialized Fields
    // ═══════════════════════════════════════════════════════════════

    [Tooltip("focus 계산 기준 노드(CharSlot_Scale). 비워두거나 구버전 노드를 가리키면 자동으로 재바인딩됩니다. 리졸버의 MeasureRect와 동일해야 프리뷰가 실제 focus와 일치합니다.")]
    [SerializeField] private RectTransform focusRect;

    [Tooltip("비워두면 이 오브젝트의 parent RectTransform을 표시 기준으로 사용합니다. 보통 Character_ExtensionsRoot입니다.")]
    [SerializeField] private RectTransform previewRoot;

    [Header("Tuning")]
    [Tooltip("캐릭터/포즈별 focus 보정 DB입니다.")]
    [SerializeField] private CharacterFocusTuningDBSO focusTuningDb;

    [Header("Tuning Key")]
    [Tooltip("CharacterFocusTuningDBSO 조회에 사용할 캐릭터 키입니다. 런타임에서는 CastCharacterCommand가 characterKey를 주입합니다.")]
    [SerializeField] private string roleKey = "";

    [Header("Points")]
    [SerializeField] private List<Point> points = new List<Point>
    {
        new Point
        {
            label    = "Feet",
            focusPreset = CharacterFocusPreset.Feet,
            color    = new Color(0.4f, 0.8f, 1f, 0.9f),
            radius   = 8f
        },
        new Point
        {
            label    = "Body",
            focusPreset = CharacterFocusPreset.Body,
            color    = new Color(0.4f, 1f, 0.4f, 0.9f),
            radius   = 10f
        },
        new Point
        {
            label    = "Bust",
            focusPreset = CharacterFocusPreset.Bust,
            color    = new Color(1f, 0.8f, 0.25f, 0.9f),
            radius   = 11f
        },
        new Point
        {
            label    = "Face",
            focusPreset = CharacterFocusPreset.Face,
            color    = new Color(1f, 0.35f, 0.35f, 0.95f),
            radius   = 13f
        }
    };

    [Header("Game View Draw")]
    [SerializeField] private bool drawInGameView  = true;
    [SerializeField] private bool hideInPlayMode  = false;

    [Header("Editor Draw")]
    [SerializeField] private bool drawInSceneView = true;
    [SerializeField] private bool drawLabels      = true;

    // ═══════════════════════════════════════════════════════════════
    //  Runtime Cache
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// true일 때만 LateUpdate에서 마커를 다시 계산합니다.
    /// Inspector 변경, ref 재탐색, focusRect 이동이 감지되면 세워집니다.
    /// </summary>
    private bool _isDirty = true;

    /// <summary>roleKey로 만든 tuning key 캐시.</summary>
    private string _cachedTuningKey;

    /// <summary>마커 GameObject 이름 → RectTransform 맵.</summary>
    private readonly Dictionary<string, RectTransform> _markerMap = new (StringComparer.Ordinal);

    // ── Editor-only cache ──────────────────────────────────────────
#if UNITY_EDITOR
    /// <summary>
    /// OnDrawGizmos에서 매 프레임 new GUIStyle() 하지 않도록 캐싱합니다.
    /// null이면 다음 Gizmos 드로우 시 재생성합니다.
    /// </summary>
    private GUIStyle _cachedLabelStyle;
#endif

    /// <summary>생성되는 프리뷰 마커 GameObject 이름의 공통 토큰.</summary>
    private const string MarkerNameToken = "__FocusPoint_";

    /// <summary>
    /// focus 계산 기준 노드 이름. CharacterFocusPointResolver가 측정에 쓰는 MeasureRect(CharSlot_Scale)와
    /// 반드시 동일해야 프리뷰가 실제 focus point와 일치한다. (framing response보다 위, response 중립 노드)
    /// </summary>
    private const string FocusBasisNodeName = "CharSlot_Scale";

    private void Reset()
    {
        RectTransform rect = transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot     = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
        }

        ResolveRefs();
        RebuildMarkerMap();
        MarkDirty();
    }

    private void OnEnable()
    {
        ResolveRefs();
        RebuildMarkerMap();
        MarkDirty();
    }

    private void OnValidate()
    {
        // 튜닝 키 캐시 무효화 (roleKey가 바뀌었을 수 있음)
        _cachedTuningKey = null;

        // GUIStyle 캐시 무효화 (drawLabels, color 등이 바뀌었을 수 있음)
#if UNITY_EDITOR
        _cachedLabelStyle = null;
#endif

        ResolveRefs();
        RebuildMarkerMap();

        // OnValidate 안에서 child 생성은 금지. 이미 있는 마커만 갱신.
        if (_isDirty)
            UpdateGameViewMarkers(allowCreate: false);
    }

    /// <summary>
    /// Update 대신 LateUpdate를 사용합니다.
    /// UI RectTransform은 Canvas가 LateUpdate 이후 정렬되므로
    /// 같은 프레임에서 올바른 world position을 읽으려면 LateUpdate가 적합합니다.
    /// </summary>
    private void LateUpdate()
    {
        if (Application.isPlaying && hideInPlayMode)
        {
            SetAllGameViewMarkersVisible(false);
            return;
        }

        if (focusRect != null && focusRect.hasChanged)
            focusRect.hasChanged = false;

        if (focusRect == null || previewRoot == null)
            ResolveRefs();

        UpdateGameViewMarkers(allowCreate: true);
    }

    [ContextMenu("Rebuild Game View Focus Markers")]
    public void RebuildGameViewMarkers()
    {
        ResolveRefs();
        RebuildMarkerMap();
        UpdateGameViewMarkers(allowCreate: true);
    }

    [ContextMenu("Hide Game View Focus Markers")]
    public void HideGameViewMarkers()
    {
        SetAllGameViewMarkersVisible(false);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Dirty Flag
    // ═══════════════════════════════════════════════════════════════

    private void MarkDirty() => _isDirty = true;
    
    public void SetRoleKey(string newRoleKey)
    {
        newRoleKey = newRoleKey?.Trim();

        if (string.IsNullOrWhiteSpace(newRoleKey))
            return;

        if (string.Equals(roleKey, newRoleKey, StringComparison.Ordinal))
            return;

        roleKey = newRoleKey;
        _cachedTuningKey = null;
        MarkDirty();
    }
    

    /// <summary>
    /// null인 ref만 탐색합니다. 이미 할당된 ref는 건드리지 않습니다.
    /// </summary>
    private void ResolveRefs()
    {
        // focusRect는 CharacterFocusPointResolver의 MeasureRect(CharSlot_Scale)와 같은 노드여야 한다.
        // 비어있거나, 구버전(Character_CastTransform) 또는 다른 노드를 가리키고 있으면 재바인딩한다.
        // (직렬화된 ref를 그대로 두면 코드만 고쳐도 옛 prefab 인스턴스는 계속 어긋난다.)
        if (focusRect == null || !IsFocusBasisRect(focusRect))
            focusRect = FindFocusBasisInParentHierarchy();

        if (previewRoot == null)
            previewRoot = transform.parent as RectTransform;
    }

    private RectTransform GetPreviewRoot() =>
        previewRoot != null ? previewRoot : transform.parent as RectTransform;

    // ═══════════════════════════════════════════════════════════════
    //  Tuning Key Cache
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// roleKey + poseKey로 만든 tuning key를 반환합니다.
    /// OnValidate에서 null로 초기화되므로 변경 시 자동 재생성됩니다.
    /// </summary>
    private string GetOrBuildTuningKey()
    {
        if (_cachedTuningKey == null)
            _cachedTuningKey = (roleKey ?? "").Trim();

        return _cachedTuningKey;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Game View Marker Update
    // ═══════════════════════════════════════════════════════════════

    private void UpdateGameViewMarkers(bool allowCreate)
    {
        if (!drawInGameView)
        {
            SetAllGameViewMarkersVisible(false);
            return;
        }

        if (focusRect == null)
        {
            SetAllGameViewMarkersVisible(false);
            return;
        }

        RectTransform root = GetPreviewRoot();
        if (root == null)
        {
            SetAllGameViewMarkersVisible(false);
            return;
        }

        string tuningKey = GetOrBuildTuningKey();

        for (int i = 0; i < points.Count; i++)
        {
            Point point = points[i];
            if (point == null)
                continue;

            RectTransform marker = EnsureGameViewMarker(point, allowCreate);
            if (marker == null)
                continue;

            if (!point.enabled)
            {
                SetGraphicVisible(marker, false);
                continue;
            }

            // ── 위치 계산 ──────────────────────────────────────────
            Vector2 focusOffset = CharacterFocusTuningResolver.ResolveOffset(
                focusTuningDb,
                tuningKey,
                point.focusPreset,
                point.customFocusKey,
                point.finalOffset);

            Vector3 world = focusRect.TransformPoint(
                new Vector3(focusOffset.x, focusOffset.y, 0f));

            Vector3 local = root.InverseTransformPoint(world);

            // ── 마커 갱신 ──────────────────────────────────────────
            marker.anchoredPosition = new Vector2(local.x, local.y);

            float diameter = Mathf.Max(1f, point.radius) * 2f;
            marker.sizeDelta     = new Vector2(diameter, diameter);
            marker.localScale    = Vector3.one;
            marker.localRotation = Quaternion.identity;

            Image image = marker.GetComponent<Image>();
            if (image != null)
            {
                image.color         = point.color;
                image.raycastTarget = false;
                image.enabled       = true;
            }

            SetGraphicVisible(marker, true);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Marker Lifecycle
    // ═══════════════════════════════════════════════════════════════

    private RectTransform EnsureGameViewMarker(Point point, bool allowCreate)
    {
        if (point == null)
            return null;

        string markerName = BuildMarkerName(point);

        // 1) 딕셔너리 캐시 확인
        if (_markerMap.TryGetValue(markerName, out RectTransform cached) && cached != null)
            return cached;

        // 2) 자식 계층에서 탐색 (map에 없는 경우).
        //    런타임에 rig 빌더가 모든 노드에 role prefix를 붙이므로 마커 이름이
        //    "Leafia___FocusPoint_Bust"처럼 바뀐다. 정확 일치(transform.Find)로는 못 찾아
        //    새 마커를 또 만들어 중복(8개)이 된다. prefix를 무시하고 suffix로 식별해 채택한다.
        RectTransform existingRect = FindExistingMarkerBySuffix(markerName);
        if (existingRect != null)
        {
            _markerMap[markerName] = existingRect;
            return existingRect;
        }

        if (!allowCreate || !CanCreateChildMarkers())
            return null;

        // 3) 새로 생성
        return CreateMarker(markerName);
    }

    private RectTransform CreateMarker(string markerName)
    {
        GameObject go = new GameObject(
            markerName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        // 프리뷰 전용 오브젝트. 씬/프리팹에 직렬화되지 않게 한다.
        // → rig 빌더의 role prefix 부여 대상이 되지 않고, Instantiate 시 baking 중복도 생기지 않는다.
        go.hideFlags = HideFlags.DontSave;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            Undo.RegisterCreatedObjectUndo(go, "Create Focus Preview Marker");
#endif

        RectTransform marker = go.GetComponent<RectTransform>();
        marker.SetParent(transform, false);

        marker.anchorMin     = new Vector2(0.5f, 0.5f);
        marker.anchorMax     = new Vector2(0.5f, 0.5f);
        marker.pivot         = new Vector2(0.5f, 0.5f);
        marker.localScale    = Vector3.one;
        marker.localRotation = Quaternion.identity;

        Image image = marker.GetComponent<Image>();
        image.raycastTarget = false;
        image.sprite        = LoadMarkerSprite();

        _markerMap[markerName] = marker;
        return marker;
    }

    /// <summary>
    /// 마커에 사용할 Sprite를 반환합니다.
    /// Sprite가 없으면 Image는 렌더링되지 않으므로 반드시 할당해야 합니다.
    /// </summary>
    private static Sprite LoadMarkerSprite()
    {
#if UNITY_EDITOR
        // 에디터: Unity 내장 Knob 스프라이트 (원형)
        Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        if (sprite != null)
            return sprite;
#endif
        // 런타임 또는 에디터 로드 실패 시: 1×1 흰 텍스처로 대체
        Texture2D tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
    }

    private static string BuildMarkerName(Point point) =>
        MarkerNameToken + point.DisplayName;

    private bool CanCreateChildMarkers()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (EditorUtility.IsPersistent(gameObject))
            {
                Debug.LogWarning(
                    "[CharacterFocusPreviewMarker] Prefab Asset 안에서는 preview marker child를 만들 수 없습니다. " +
                    "Prefab Instance에서 사용하거나 수동으로 child를 만드세요.",
                    this);
                return false;
            }

            if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                Debug.LogWarning(
                    "[CharacterFocusPreviewMarker] Prefab Asset을 직접 편집 중에는 preview marker child를 만들 수 없습니다.",
                    this);
                return false;
            }
        }
#endif
        return true;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Marker Map
    // ═══════════════════════════════════════════════════════════════

    private void RebuildMarkerMap()
    {
        _markerMap.Clear();

        // 자식 이름은 런타임에 role prefix가 붙어 더럽혀질 수 있다("Leafia___FocusPoint_Bust").
        // 따라서 prefix를 무시하고, 현재 points로부터 만든 canonical 이름("__FocusPoint_Bust")으로
        // 끝나는지로 식별해 canonical 키로 채택한다. 같은 canonical이 둘 이상이면 첫 것만 채택한다.
        for (int i = 0; i < transform.childCount; i++)
        {
            RectTransform child = transform.GetChild(i) as RectTransform;
            if (child == null)
                continue;

            if (child.name.IndexOf(MarkerNameToken, StringComparison.Ordinal) < 0)
                continue;

            string canonical = ResolveCanonicalMarkerName(child.name);
            if (canonical == null)
                continue;

            if (!_markerMap.ContainsKey(canonical))
                _markerMap[canonical] = child;
        }
    }

    /// <summary>자식 이름이 (prefix 무시) 어떤 point의 canonical 마커 이름으로 끝나면 그 canonical을 돌려준다.</summary>
    private string ResolveCanonicalMarkerName(string childName)
    {
        for (int i = 0; i < points.Count; i++)
        {
            Point point = points[i];
            if (point == null)
                continue;

            string canonical = BuildMarkerName(point);
            if (childName.EndsWith(canonical, StringComparison.Ordinal))
                return canonical;
        }
        return null;
    }

    /// <summary>canonical 마커 이름으로 끝나는 자식을 찾는다(role prefix 허용).</summary>
    private RectTransform FindExistingMarkerBySuffix(string canonicalMarkerName)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child != null && child.name.EndsWith(canonicalMarkerName, StringComparison.Ordinal))
                return child as RectTransform;
        }
        return null;
    }

    private void SetAllGameViewMarkersVisible(bool visible)
    {
        // 별도 RebuildMarkerMap() 호출 제거.
        // _markerMap은 OnEnable / RebuildGameViewMarkers에서 이미 최신 상태입니다.
        foreach (KeyValuePair<string, RectTransform> pair in _markerMap)
            SetGraphicVisible(pair.Value, visible);
    }

    private static void SetGraphicVisible(RectTransform marker, bool visible)
    {
        if (marker == null)
            return;

        Graphic graphic = marker.GetComponent<Graphic>();
        if (graphic != null)
            graphic.enabled = visible;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Scene View Gizmos  (Editor only)
    // ═══════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!drawInSceneView)
            return;

        DrawSceneViewFocusPoints();
    }

    private void DrawSceneViewFocusPoints()
    {
        if (focusRect == null)
            return;

        string tuningKey = GetOrBuildTuningKey();

        for (int i = 0; i < points.Count; i++)
        {
            Point point = points[i];
            if (point == null || !point.enabled)
                continue;

            Vector2 focusOffset = CharacterFocusTuningResolver.ResolveOffset(
                focusTuningDb,
                tuningKey,
                point.focusPreset,
                point.customFocusKey,
                point.finalOffset);

            Vector3 world = focusRect.TransformPoint(
                new Vector3(focusOffset.x, focusOffset.y, 0f));

            DrawSceneViewPoint(world, point);
        }
    }

    private void DrawSceneViewPoint(Vector3 world, Point point)
    {
        Color prevColor = Handles.color;
        Handles.color   = point.color;

        float radius = Mathf.Max(1f, point.radius);
        Handles.DrawSolidDisc(world, Vector3.forward, radius);
        Handles.DrawWireDisc(world, Vector3.forward, radius + 2f);

        if (drawLabels)
        {
            // GUIStyle을 매 프레임 new하지 않고 캐시합니다.
            // color가 포인트마다 달라서 공유할 수 없으므로 color만 교체합니다.
            EnsureLabelStyle();
            _cachedLabelStyle.normal.textColor = point.color;

            Handles.Label(
                world + new Vector3(radius + 4f, radius + 4f, 0f),
                point.DisplayName,
                _cachedLabelStyle);
        }

        Handles.color = prevColor;
    }

    /// <summary>
    /// _cachedLabelStyle이 null일 때만 새로 생성합니다.
    /// OnValidate에서 null로 리셋되므로 Inspector 변경 시 자동 갱신됩니다.
    /// </summary>
    private void EnsureLabelStyle()
    {
        if (_cachedLabelStyle != null)
            return;

        _cachedLabelStyle = new GUIStyle(EditorStyles.boldLabel);
    }
#endif

    // ═══════════════════════════════════════════════════════════════
    //  Hierarchy Search Utilities
    // ═══════════════════════════════════════════════════════════════

    private RectTransform FindInParentHierarchy(string targetName)
    {
        Transform current = transform.parent;
        while (current != null)
        {
            RectTransform found = FindChildRecursive(current, targetName);
            if (found != null)
                return found;

            current = current.parent;
        }
        return null;
    }

    private static RectTransform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null)
            return null;

        if (string.Equals(root.name, targetName, StringComparison.Ordinal))
            return root as RectTransform;

        for (int i = 0; i < root.childCount; i++)
        {
            RectTransform found = FindChildRecursive(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    /// <summary>focus 기준 노드를 부모 계층에서 찾는다. role prefix("Albedo_CharSlot_Scale")도 허용.</summary>
    private RectTransform FindFocusBasisInParentHierarchy()
    {
        RectTransform exact = FindInParentHierarchy(FocusBasisNodeName);
        if (exact != null)
            return exact;

        // 런타임 rig는 모든 노드 이름에 role prefix가 붙으므로 정확 일치가 실패할 수 있다.
        return FindInParentHierarchyBySuffix(FocusBasisNodeName);
    }

    private static bool IsFocusBasisRect(RectTransform rect)
    {
        if (rect == null)
            return false;

        // 정확 일치 또는 prefix가 붙은 "..._CharSlot_Scale" 모두 기준 노드로 인정.
        return string.Equals(rect.name, FocusBasisNodeName, StringComparison.Ordinal)
            || rect.name.EndsWith(FocusBasisNodeName, StringComparison.Ordinal);
    }

    private RectTransform FindInParentHierarchyBySuffix(string suffix)
    {
        Transform current = transform.parent;
        while (current != null)
        {
            RectTransform found = FindChildRecursiveBySuffix(current, suffix);
            if (found != null)
                return found;

            current = current.parent;
        }
        return null;
    }

    private static RectTransform FindChildRecursiveBySuffix(Transform root, string suffix)
    {
        if (root == null)
            return null;

        if (root.name.EndsWith(suffix, StringComparison.Ordinal))
            return root as RectTransform;

        for (int i = 0; i < root.childCount; i++)
        {
            RectTransform found = FindChildRecursiveBySuffix(root.GetChild(i), suffix);
            if (found != null)
                return found;
        }

        return null;
    }
}