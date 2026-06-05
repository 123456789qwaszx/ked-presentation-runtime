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

    [Tooltip("비워두면 부모 계층에서 CharSlot_Scale 이름의 RectTransform을 자동으로 찾습니다. (focus 계산 기준 = response 중립 노드)")]
    [SerializeField] private RectTransform focusRect;

    [Tooltip("비워두면 이 오브젝트의 parent RectTransform을 표시 기준으로 사용합니다. 보통 Character_ExtensionsRoot입니다.")]
    [SerializeField] private RectTransform previewRoot;

    [Header("Tuning")]
    [Tooltip("캐릭터/포즈별 focus 보정 DB입니다.")]
    [SerializeField] private CharacterFocusTuningDBSO focusTuningDb;

    [Header("Tuning Key")]
    [Tooltip("예: leafia. autoResolveRoleKeyFromName이 true이면 런타임에 오브젝트 이름에서 자동 추출됩니다.")]
    [SerializeField] private string roleKey = "";

    [Tooltip("예: pose_wide. 비워두면 roleKey만 사용합니다.")]
    [SerializeField] private string poseKey = "";

    [Tooltip(
        "true이면 런타임 생성 시 오브젝트 이름의 '_FocusMarker' 앞 접두사를 roleKey로 자동 적용합니다.\n" +
        "예) Albedo_FocusMarker → albedo / Leafia_FocusMarker → leafia\n" +
        "false이면 Inspector에 입력된 roleKey를 그대로 사용합니다.")]
    [SerializeField] private bool autoResolveRoleKeyFromName = true;

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

    /// <summary>roleKey + poseKey로 만든 tuning key 캐시.</summary>
    private string _cachedTuningKey;

    /// <summary>마커 GameObject 이름 → RectTransform 맵.</summary>
    private readonly Dictionary<string, RectTransform> _markerMap =
        new Dictionary<string, RectTransform>(StringComparer.Ordinal);

    // ── Editor-only cache ──────────────────────────────────────────
#if UNITY_EDITOR
    /// <summary>
    /// OnDrawGizmos에서 매 프레임 new GUIStyle() 하지 않도록 캐싱합니다.
    /// null이면 다음 Gizmos 드로우 시 재생성합니다.
    /// </summary>
    private GUIStyle _cachedLabelStyle;
#endif

    // ═══════════════════════════════════════════════════════════════
    //  Constants
    // ═══════════════════════════════════════════════════════════════

    /// <summary>런타임 오브젝트 이름에서 roleKey를 추출할 때 기준이 되는 접미사.</summary>
    private const string FocusMarkerSuffix = "_FocusMarker";

    // ═══════════════════════════════════════════════════════════════
    //  Unity Messages
    // ═══════════════════════════════════════════════════════════════

    // private void Awake()
    // {
    //     // 런타임 생성 시 오브젝트 이름에서 roleKey를 자동으로 추출합니다.
    //     // Awake는 OnEnable보다 먼저 호출되므로 이후 로직이 올바른 키를 사용합니다.
    //     if (Application.isPlaying && autoResolveRoleKeyFromName)
    //         TryResolveRoleKeyFromName();
    // }
    private void Start()
    {
        if (Application.isPlaying && autoResolveRoleKeyFromName)
            TryResolveRoleKeyFromName();
    }

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
        // 튜닝 키 캐시 무효화 (roleKey / poseKey가 바뀌었을 수 있음)
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

    // ═══════════════════════════════════════════════════════════════
    //  Role Key Auto-Resolution
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 오브젝트 이름의 <c>_FocusMarker</c> 앞 접두사를 소문자로 변환해 <see cref="roleKey"/>에 적용합니다.<br/>
    /// 예) <c>Albedo_FocusMarker</c> → <c>albedo</c><br/>
    /// 예) <c>Leafia_FocusMarker</c> → <c>leafia</c><br/>
    /// 이름에 접미사가 없으면 아무 것도 하지 않습니다.
    /// </summary>
    private void TryResolveRoleKeyFromName()
    {
        string objName = gameObject.name;

        int suffixIndex = objName.IndexOf(
            FocusMarkerSuffix,
            StringComparison.OrdinalIgnoreCase);

        // 접미사가 없거나 맨 앞에 붙어있는 경우(접두사가 빈 문자열)는 무시
        if (suffixIndex <= 0)
            return;

        // string extracted = objName
        //     .Substring(0, suffixIndex)
        //     .ToLowerInvariant();
        
        string extracted = objName
            .Substring(0, suffixIndex);

        // 이미 같은 값이면 dirty / cache 무효화 생략
        if (string.Equals(roleKey, extracted, StringComparison.Ordinal))
            return;

        roleKey          = extracted;
        _cachedTuningKey = null; // tuning key 캐시 무효화
        MarkDirty();

// #if UNITY_EDITOR
//         Debug.Log(
//             $"[CharacterFocusPreviewMarker] roleKey auto-resolved: \"{objName}\" → \"{roleKey}\"",
//             this);
// #endif
    }



    /// <summary>
    /// null인 ref만 탐색합니다. 이미 할당된 ref는 건드리지 않습니다.
    /// </summary>
    private void ResolveRefs()
    {
        if (focusRect == null)
            focusRect = FindInParentHierarchy("CharSlot_Scale");   // ← Character_CastTransform 에서 변경

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
            _cachedTuningKey = CharacterFocusTuningResolver.BuildTuningKey(roleKey, poseKey);

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

        // 2) 자식 계층에서 탐색 (map에 없는 경우)
        Transform existing = transform.Find(markerName);
        if (existing != null)
        {
            RectTransform existingRect = existing as RectTransform;
            if (existingRect != null)
            {
                _markerMap[markerName] = existingRect;
                return existingRect;
            }
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
        "__FocusPoint_" + point.DisplayName;

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

        for (int i = 0; i < transform.childCount; i++)
        {
            RectTransform child = transform.GetChild(i) as RectTransform;
            if (child == null)
                continue;

            if (!child.name.StartsWith("__FocusPoint_", StringComparison.Ordinal))
                continue;

            _markerMap[child.name] = child;
        }
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
    
    /// <summary>
    /// 스포너에서 이름을 세팅한 직후 명시적으로 호출할 수 있습니다.
    /// Start() 타이밍보다 늦게 이름이 결정되는 경우에 사용합니다.
    /// </summary>
    public void RefreshRoleKeyFromName()
    {
        if (autoResolveRoleKeyFromName)
            TryResolveRoleKeyFromName();
    }
    
    // var go = Instantiate(focusMarkerPrefab);
    // go.name = "Albedo_FocusMarker";
    // go.GetComponent<CharacterFocusPreviewMarker>().RefreshRoleKeyFromName();
}