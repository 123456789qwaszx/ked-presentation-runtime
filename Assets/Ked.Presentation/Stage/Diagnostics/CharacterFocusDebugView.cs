#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections.Generic;
using System.Text;
using Ked.Presentation.Core;
using UnityEngine;
using UnityEngine.UI;

// ─────────────────────────────────────────────────────────────────────
// 인물 표지 — 캐스팅된 슬롯마다 초점 넷 · 슬롯의 자리 · 겨눔을 그린다(docs/PLAN_move-axes.md §10).
//
//   · 초점(Feet · Body · Bust · Face) — 그림의 점. 배역 앵커만큼 슬롯의 자리에서 떨어져 선다.
//   · 슬롯의 자리(십자) — CharSlot_Track(place) + Move_X · _Y(left · right · up · down)가 놓이는 점과 그 값.
//     움직임이 쌓이면 대본에서 "지금 어디인가"가 바로 읽히지 않는다 — 여기서 읽는다(§4.3).
//   · 겨눔 — 마우스 자리에 겨눈 초점(기본 Bust)을 두는 값: move_reset 슬롯 0fr → place 슬롯 x y.
//     식은 코어 NodeTargets.PlaceForFocusAt이다(자리를 옮기면 초점이 평행이동한다 — M2).
//
// 값은 카메라 중립의 무대 좌표로 잰다(measureCameraNeutral) — place에 적는 값이고 격자(ScreenFocusGridOverlay, F1)와 같은 좌표다.
// 개발용 — 씬의 DebugView 묶음은 태그 EditorOnly라 빌드에 들어가지 않는다.
// ─────────────────────────────────────────────────────────────────────
[DisallowMultipleComponent]
public sealed class CharacterFocusDebugView : MonoBehaviour
{
    [Serializable]
    public sealed class Point
    {
        public bool enabled = true;

        [Header("Identity")]
        public string label = "";

        [Header("Focus")]
        public CharacterFocusPreset focusPreset = CharacterFocusPreset.Face;

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

                return focusPreset.ToString();
            }
        }
    }

#if UNITY_EDITOR
    private readonly struct ScenePoint
    {
        public readonly Vector3 world;
        public readonly Point point;
        public readonly string label;

        public ScenePoint(
            Vector3 world,
            Point point,
            string label)
        {
            this.world = world;
            this.point = point;
            this.label = label;
        }
    }
#endif

    [Header("Marker Root")]
    [Tooltip("마커들이 생성될 DebugView Root입니다. 비워두면 이 오브젝트의 RectTransform을 사용합니다.")]
    [SerializeField] private RectTransform markerRoot;

    [Header("Toggle")]
    [Tooltip("인물 표지를 켜고 끈다(재생 중). 캐스팅된 슬롯 전부를 그린다 — 폴드 밖(대사창 초상 · 컷인 판 · 그림 팝업)은 뺀다.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F2;
    [SerializeField] private bool visible;

    [Header("Values (docs/PLAN_move-axes.md §10)")]
    [Tooltip("켜면 카메라 중립의 무대 좌표로 잰다 — place에 적는 값이다. 끄면 지금 화면에 보이는 자리(라이브 카메라)로 잰다.")]
    [SerializeField] private bool measureCameraNeutral = true;
    [Tooltip("슬롯의 자리(Track + Move)에 십자와 값을 그린다.")]
    [SerializeField] private bool drawSlotMarker = true;
    [Tooltip("마우스 자리에 겨눈 초점을 두는 place 값을 인물마다 적는다.")]
    [SerializeField] private bool aimWithMouse = true;
    [SerializeField] private CharacterFocusPreset aimPreset = CharacterFocusPreset.Bust;
    [SerializeField] private Color slotMarkerColor = new(1f, 1f, 1f, 0.95f);
    [SerializeField] private Color textColor = new(1f, 1f, 1f, 1f);
    [SerializeField] private Color textBackground = new(0f, 0f, 0f, 0.6f);
    [SerializeField] private int fontSize = 13;

    [Header("Tuning")]
    [SerializeField] private CharacterFocusTuningDBSO focusTuningDb;

    [Header("Points")]
    [SerializeField] private List<Point> points = new()
    {
        new Point
        {
            label = "Feet",
            focusPreset = CharacterFocusPreset.Feet,
            color = new Color(0.4f, 0.8f, 1f, 0.9f),
            radius = 13f,
        },
        new Point
        {
            label = "Body",
            focusPreset = CharacterFocusPreset.Body,
            color = new Color(0.4f, 1f, 0.4f, 0.9f),
            radius = 14f,
        },
        new Point
        {
            label = "Bust",
            focusPreset = CharacterFocusPreset.Bust,
            color = new Color(1f, 0.8f, 0.25f, 0.9f),
            radius = 15f,
        },
        new Point
        {
            label = "Face",
            focusPreset = CharacterFocusPreset.Face,
            color = new Color(1f, 0.35f, 0.35f, 0.95f),
            radius = 16f,
        },
    };

    [Header("Game View Draw")]
    [SerializeField] private bool drawInGameView = true;
    [SerializeField] private bool hideInPlayMode = false;

    [Header("Scene View Draw")]
    [SerializeField] private bool drawInSceneView = true;
    [SerializeField] private bool drawLabels = true;

    private PresentationStage _stage;
    private IStageViewNeutral _stageView;

    // 그리기는 OnGUI — LateUpdate가 모으고 OnGUI가 그린다.
    private readonly struct Caption
    {
        public readonly Vector2 Gui;
        public readonly string Value;
        public readonly Vector2 Offset;

        public Caption(Vector2 gui, string value, Vector2 offset)
        {
            Gui = gui;
            Value = value;
            Offset = offset;
        }
    }

    private readonly List<Vector2> _slotMarkers = new();
    private readonly List<Caption> _texts = new();
    private readonly List<string> _aimLines = new();
    private readonly List<KeyValuePair<string, CharacterRigRefs>> _rigEntries = new();
    private readonly List<RectTransform> _chainRects = new();
    private readonly StringBuilder _line = new();

    private Vector2 _mouseGui;
    private Texture2D _pixel;
    private Texture2D _background;
    private GUIStyle _textStyle;

    private readonly Dictionary<string, RectTransform> _markerMap =
        new(StringComparer.Ordinal);

    private readonly HashSet<string> _visibleMarkerKeysThisFrame =
        new(StringComparer.Ordinal);

#if UNITY_EDITOR
    private readonly List<ScenePoint> _scenePoints = new();
    private GUIStyle _cachedLabelStyle;
#endif

    private static Sprite _cachedMarkerSprite;

    private const string MarkerNameToken = "__FocusPoint_";

    /// <param name="stageView">카메라 중립 — 주면 값을 샷과 상관없는 무대 좌표로 잰다(배치 커맨드가 쓰는 것과 같다).</param>
    public void Initialize(
        PresentationStage stage,
        CharacterFocusTuningDBSO tuningDb,
        IStageViewNeutral stageView = null)
    {
        _stage = stage;
        _stageView = stageView;

        if (tuningDb != null)
            focusTuningDb = tuningDb;

        ResolveMarkerRoot();
        HideAllMarkers();
    }

    private void Reset()
    {
        ResolveMarkerRoot();
    }

    private void OnEnable()
    {
        ResolveMarkerRoot();
    }

    private void OnValidate()
    {
        ResolveMarkerRoot();

        // 인스펙터에서 바꾼 색 · 글 크기를 다음 그리기에 쓴다.
        if (_background != null)
        {
            _background.SetPixel(0, 0, textBackground);
            _background.Apply();
        }

        _textStyle = null;

#if UNITY_EDITOR
        _cachedLabelStyle = null;
#endif
    }

    private void LateUpdate()
    {
        if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
            visible = !visible;

        ClearTexts();

        if (!visible)
        {
            ClearScenePoints();
            HideAllMarkers();
            return;
        }

        if (Application.isPlaying && hideInPlayMode)
        {
            ClearScenePoints();
            HideAllMarkers();
            return;
        }

        if (!drawInGameView && !drawInSceneView)
        {
            ClearScenePoints();
            HideAllMarkers();
            return;
        }

        UpdateFocusMarkers();
    }

    [ContextMenu("Hide Focus Debug Markers")]
    public void HideAllMarkers()
    {
        foreach (KeyValuePair<string, RectTransform> pair in _markerMap)
            SetMarkerVisible(pair.Value, false);
    }

    private void UpdateFocusMarkers()
    {
        _visibleMarkerKeysThisFrame.Clear();
        ClearScenePoints();

        if (_stage == null)
        {
            HideAllMarkers();
            return;
        }

        ResolveMarkerRoot();

        if (markerRoot == null)
        {
            HideAllMarkers();
            return;
        }

        _mouseGui = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

        // 캐스팅된 슬롯 전부 — 배경(캐스팅하지 않은 슬롯)과 폴드 밖의 리그는 뺀다.
        _rigEntries.Clear();
        _stage.characterRigs.CollectAliveRigEntries(_rigEntries);
        _rigEntries.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));

        for (int i = 0; i < _rigEntries.Count; i++)
        {
            string slotKey = _rigEntries[i].Key;
            CharacterRigRefs rigRefs = _rigEntries[i].Value;

            if (_stage.characterRigs.IsOutsideFold(slotKey) || rigRefs.Stage == null)
                continue;

            // DebugView는 매 프레임 돌기 때문에 경고 로그 없는 조회 API만 사용한다.
            if (!_stage.castRegistry.TryPeekCharacter(slotKey, out string characterKey))
                continue;

            if (string.IsNullOrWhiteSpace(characterKey))
                continue;

            _stage.castRegistry.TryPeekFacing(slotKey, out CharacterFacing facing);

            UpdateCharacterFocusMarkers(
                slotKey,
                characterKey.Trim(),
                rigRefs,
                rigRefs.Stage.Root,
                facing);
        }

        HideStaleMarkers();
    }

    private void UpdateCharacterFocusMarkers(
        string slotKey,
        string characterKey,
        CharacterRigRefs rigRefs,
        RectTransform rigSpaceRoot,
        CharacterFacing facing)
    {
        Camera uiCamera = CameraOf(rigSpaceRoot);

        for (int i = 0; i < points.Count; i++)
        {
            Point point = points[i];

            if (point == null || !point.enabled)
                continue;

            if (focusTuningDb == null || rigRefs.CharacterPortrait_VisualOffset == null)
                continue;

            CharacterFocusPointResult focus = CharacterFocusPointResolver.ResolveFromRigRefs(
                rigRefs,
                rigSpaceRoot,
                characterKey,
                point.focusPreset,
                point.finalOffset,
                focusTuningDb,
                useSettledPlacementTargets: true,
                facing,
                mirrorCommandOffset: true,
                StageView);

            Vector3 world =
                focus.RigSpaceRoot.TransformPoint(
                    new Vector3(
                        focus.FocusPointInRigSpace.x,
                        focus.FocusPointInRigSpace.y,
                        0f));

            string markerKey = BuildMarkerKey(slotKey, point);
            string markerName = BuildMarkerName(slotKey, characterKey, point);

            RegisterScenePoint(world, point, markerName);

            if (!drawInGameView)
                continue;

            // 겨누는 초점 하나에만 좌표를 적는다 — 넷 다 적으면 글이 겹친다.
            if (point.focusPreset == aimPreset)
            {
                _texts.Add(new Caption(
                    ToGui(world, uiCamera),
                    $"{point.DisplayName} {U(focus.FocusPointInRigSpace.x)} {U(focus.FocusPointInRigSpace.y)}",
                    new Vector2(12f, -10f)));
            }

            RectTransform marker = EnsureMarker(markerKey, markerName);

            if (marker == null)
                continue;

            Vector3 local = markerRoot.InverseTransformPoint(world);

            marker.anchoredPosition = new Vector2(local.x, local.y);

            float diameter = Mathf.Max(1f, point.radius) * 2f;

            marker.sizeDelta = new Vector2(diameter, diameter);
            marker.localScale = Vector3.one;
            marker.localRotation = Quaternion.identity;

            Image image = marker.GetComponent<Image>();
            if (image != null)
            {
                image.color = point.color;
                image.raycastTarget = false;
                image.enabled = true;
            }

            SetMarkerVisible(marker, true);
            _visibleMarkerKeysThisFrame.Add(markerKey);
        }

        if (drawInGameView)
            CollectSlotValues(slotKey, characterKey, rigRefs, rigSpaceRoot, facing, uiCamera);
    }

    // ── 슬롯의 자리 · 겨눔 (docs/PLAN_move-axes.md §10) ─────────────────

    private IStageViewNeutral StageView => measureCameraNeutral ? _stageView : null;

    // 값은 place에 적는 u다 — 파서가 u를 px로 바꾸는 그 기준 폭.
    private static string U(float pixels) => UnitToken.FormatUnits(pixels, UnitToken.DefaultReferenceStageWidth);

    private void CollectSlotValues(
        string slotKey,
        string characterKey,
        CharacterRigRefs rigRefs,
        RectTransform rigSpaceRoot,
        CharacterFacing facing,
        Camera uiCamera)
    {
        if (!drawSlotMarker && !aimWithMouse)
            return;

        if (rigRefs.CharSlot_Move_Y == null)
            return;

        // 자리 노드의 정착값 — 장부(트윈 끝값) → 샷 중립 → 라이브 차례(CharacterPlacementTargetLedger.CaptureSettledChain).
        RectNodeState[] chain = rigRefs.PlacementTargets.CaptureSettledChain(
            rigRefs.CharSlot_Move_Y, rigSpaceRoot, _chainRects, StageView);

        Vec2 track = PositionOf(chain, rigRefs.CharSlot_Track);
        Vec2 move = PositionOf(chain, rigRefs.CharSlot_Move_X) + PositionOf(chain, rigRefs.CharSlot_Move_Y);
        Vec2 focusMove = PositionOf(chain, rigRefs.CharFocus_Track);

        if (drawSlotMarker)
        {
            Vec3 origin = RectChainMath.TransformPoint(chain, CharacterPlacementTargetLedger.SpaceOf(rigSpaceRoot), Vec3.Zero);
            Vector2 at = ToGui(rigSpaceRoot.TransformPoint(new Vector3(origin.X, origin.Y, origin.Z)), uiCamera);

            _slotMarkers.Add(at);

            _line.Clear();
            _line.Append(slotKey).Append(' ').Append(characterKey)
                .Append("\nplace ").Append(U(track.X)).Append(' ').Append(U(track.Y));

            if (!IsZero(move))
                _line.Append("\n움직임 +").Append(U(move.X)).Append(' ').Append(U(move.Y));

            if (!IsZero(focusMove))
                _line.Append("\n포커스 +").Append(U(focusMove.X)).Append(' ').Append(U(focusMove.Y));

            _texts.Add(new Caption(at, _line.ToString(), new Vector2(12f, 6f)));
        }

        if (!aimWithMouse || focusTuningDb == null || rigRefs.CharacterPortrait_VisualOffset == null)
            return;

        if (!TryMouseInRoot(rigSpaceRoot, uiCamera, out Vector2 desired))
            return;

        CharacterFocusPointResult aim = CharacterFocusPointResolver.ResolveFromRigRefs(
            rigRefs,
            rigSpaceRoot,
            characterKey,
            aimPreset,
            Vector2.zero,
            focusTuningDb,
            useSettledPlacementTargets: true,
            facing,
            mirrorCommandOffset: true,
            StageView);

        // 움직임을 걷고(move_reset) 다시 세우는 값 — 자리를 옮기면 초점이 평행이동한다(M2).
        Vec2 placed = NodeTargets.PlaceForFocusAt(track, move, aim.FocusPointInRigSpace.ToCore(), desired.ToCore());

        _aimLines.Add($"{slotKey}  move_reset {slotKey} 0fr  ·  place {slotKey} {U(placed.X)}u {U(placed.Y)}u");
    }

    private Vec2 PositionOf(RectNodeState[] chain, RectTransform rect)
    {
        int index = rect != null ? _chainRects.IndexOf(rect) : -1;
        return index >= 0 ? chain[index].AnchoredPosition : Vec2.Zero;
    }

    // 적는 글의 셋째 자리 아래는 0이다.
    private static bool IsZero(Vec2 v) => Mathf.Abs(v.X) < 0.0005f && Mathf.Abs(v.Y) < 0.0005f;

    private static bool TryMouseInRoot(RectTransform root, Camera uiCamera, out Vector2 local)
        => RectTransformUtility.ScreenPointToLocalPointInRectangle(root, Input.mousePosition, uiCamera, out local)
           && root.rect.Contains(local);

    private static Camera CameraOf(RectTransform rect)
    {
        Canvas canvas = rect != null ? rect.GetComponentInParent<Canvas>() : null;

        return canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.rootCanvas.worldCamera
            : null;
    }

    // OnGUI는 왼쪽 위가 원점이다. 화면 좌표는 왼쪽 아래가 원점이다.
    private static Vector2 ToGui(Vector3 world, Camera uiCamera)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, world);
        return new Vector2(screen.x, Screen.height - screen.y);
    }

    private void ClearTexts()
    {
        _slotMarkers.Clear();
        _texts.Clear();
        _aimLines.Clear();
    }

    private void OnGUI()
    {
        if (!visible || Event.current.type != EventType.Repaint)
            return;

        if (_slotMarkers.Count == 0 && _texts.Count == 0 && _aimLines.Count == 0)
            return;

        EnsureTextStyle();

        Color oldColor = GUI.color;

        GUI.color = slotMarkerColor;

        foreach (Vector2 at in _slotMarkers)
        {
            DrawRect(new Rect(at.x - 9f, at.y - 1f, 18f, 2f));
            DrawRect(new Rect(at.x - 1f, at.y - 9f, 2f, 18f));
        }

        foreach (Caption text in _texts)
            DrawText(text.Value, text.Gui + text.Offset);

        if (_aimLines.Count > 0)
        {
            _line.Clear();
            _line.Append("마우스 자리에 ").Append(aimPreset).Append(" — ").Append(toggleKey).Append(" 끄기");

            foreach (string line in _aimLines)
                _line.Append('\n').Append(line);

            DrawText(_line.ToString(), _mouseGui + new Vector2(18f, 18f));
        }

        GUI.color = oldColor;
    }

    private void DrawRect(Rect rect) => GUI.DrawTexture(rect, _pixel);

    // 화면 밖으로 나가지 않게 오른쪽 · 아래를 접는다.
    private void DrawText(string value, Vector2 at)
    {
        GUIContent content = new(value);
        Vector2 size = _textStyle.CalcSize(content);

        float x = Mathf.Min(at.x, Screen.width - size.x);
        float y = Mathf.Min(at.y, Screen.height - size.y);

        Color oldColor = GUI.color;
        GUI.color = Color.white;
        GUI.Label(new Rect(Mathf.Max(0f, x), Mathf.Max(0f, y), size.x, size.y), content, _textStyle);
        GUI.color = oldColor;
    }

    private void EnsureTextStyle()
    {
        if (_pixel == null)
            _pixel = SolidTexture(Color.white);

        if (_background == null)
            _background = SolidTexture(textBackground);

        if (_textStyle != null && _textStyle.fontSize == fontSize)
            return;

        _textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft,
            wordWrap = false,
            padding = new RectOffset(4, 4, 2, 2),
        };

        _textStyle.normal.textColor = textColor;
        _textStyle.normal.background = _background;
    }

    private static Texture2D SolidTexture(Color color)
    {
        Texture2D texture = new(1, 1) { hideFlags = HideFlags.DontSave };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void OnDestroy()
    {
        if (_pixel != null)
            Destroy(_pixel);

        if (_background != null)
            Destroy(_background);
    }

    private RectTransform EnsureMarker(
        string markerKey,
        string markerName)
    {
        if (_markerMap.TryGetValue(markerKey, out RectTransform cached) &&
            cached != null)
        {
            return cached;
        }

        RectTransform existing = FindExistingMarker(markerName);

        if (existing != null)
        {
            _markerMap[markerKey] = existing;
            return existing;
        }

        GameObject go = new GameObject(
            markerName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        // Debug marker 전용. 씬/프리팹에 저장하지 않는다.
        go.hideFlags = HideFlags.DontSave;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            Undo.RegisterCreatedObjectUndo(go, "Create Character Focus Debug Marker");
#endif

        RectTransform marker = go.GetComponent<RectTransform>();
        marker.SetParent(markerRoot, false);

        marker.anchorMin = new Vector2(0.5f, 0.5f);
        marker.anchorMax = new Vector2(0.5f, 0.5f);
        marker.pivot = new Vector2(0.5f, 0.5f);
        marker.localScale = Vector3.one;
        marker.localRotation = Quaternion.identity;

        Image image = marker.GetComponent<Image>();
        image.raycastTarget = false;
        image.sprite = LoadMarkerSprite();

        _markerMap[markerKey] = marker;
        return marker;
    }

    private RectTransform FindExistingMarker(string markerName)
    {
        if (markerRoot == null)
            return null;

        for (int i = 0; i < markerRoot.childCount; i++)
        {
            RectTransform child = markerRoot.GetChild(i) as RectTransform;

            if (child == null)
                continue;

            if (string.Equals(child.name, markerName, StringComparison.Ordinal))
                return child;
        }

        return null;
    }

    private void HideStaleMarkers()
    {
        foreach (KeyValuePair<string, RectTransform> pair in _markerMap)
        {
            if (_visibleMarkerKeysThisFrame.Contains(pair.Key))
                continue;

            SetMarkerVisible(pair.Value, false);
        }
    }

    private static void SetMarkerVisible(
        RectTransform marker,
        bool visible)
    {
        if (marker == null)
            return;

        Graphic graphic = marker.GetComponent<Graphic>();

        if (graphic != null)
            graphic.enabled = visible;
    }

    private void ResolveMarkerRoot()
    {
        if (markerRoot != null)
            return;

        markerRoot = transform as RectTransform;
    }

    private static string BuildMarkerKey(
        string slotKey,
        Point point)
    {
        return slotKey + "/" + point.DisplayName;
    }

    private static string BuildMarkerName(
        string slotKey,
        string characterKey,
        Point point)
    {
        return MarkerNameToken +
               slotKey +
               "_" +
               characterKey +
               "_" +
               point.DisplayName;
    }

    private static Sprite LoadMarkerSprite()
    {
        if (_cachedMarkerSprite != null)
            return _cachedMarkerSprite;

#if UNITY_EDITOR
        Sprite editorSprite =
            AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        if (editorSprite != null)
        {
            _cachedMarkerSprite = editorSprite;
            return _cachedMarkerSprite;
        }
#endif

        Texture2D tex = new Texture2D(
            1,
            1,
            TextureFormat.ARGB32,
            false);

        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        _cachedMarkerSprite = Sprite.Create(
            tex,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f));

        return _cachedMarkerSprite;
    }

    private void ClearScenePoints()
    {
#if UNITY_EDITOR
        _scenePoints.Clear();
#endif
    }

    private void RegisterScenePoint(
        Vector3 world,
        Point point,
        string label)
    {
#if UNITY_EDITOR
        _scenePoints.Add(new ScenePoint(world, point, label));
#endif
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!drawInSceneView)
            return;

        for (int i = 0; i < _scenePoints.Count; i++)
            DrawScenePoint(_scenePoints[i]);
    }

    private void DrawScenePoint(ScenePoint scenePoint)
    {
        Point point = scenePoint.point;

        if (point == null)
            return;

        Color prevColor = Handles.color;
        Handles.color = point.color;

        float radius = Mathf.Max(1f, point.radius);

        Handles.DrawSolidDisc(
            scenePoint.world,
            Vector3.forward,
            radius);

        Handles.DrawWireDisc(
            scenePoint.world,
            Vector3.forward,
            radius + 2f);

        if (drawLabels)
        {
            EnsureLabelStyle();
            _cachedLabelStyle.normal.textColor = point.color;

            Handles.Label(
                scenePoint.world + new Vector3(radius + 4f, radius + 4f, 0f),
                scenePoint.label,
                _cachedLabelStyle);
        }

        Handles.color = prevColor;
    }

    private void EnsureLabelStyle()
    {
        if (_cachedLabelStyle != null)
            return;

        _cachedLabelStyle = new GUIStyle(EditorStyles.boldLabel);
    }
#endif
}