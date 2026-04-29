using System.Collections.Generic;
using UnityEngine;

public sealed class RollbackHistoryDebugOverlay : MonoBehaviour
{
    [SerializeField] private bool _visible = false;

    private NodeRollbackHistory _history;
    private RollbackRuntimeState _state;

    private Vector2 _scroll;
    private Rect _windowRect;
    private bool _isDragging;
    private Vector2 _dragOffset;
    private bool _initialized;

    private GUIStyle _boxStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _normalStyle;
    private GUIStyle _highlightStyle;
    private GUIStyle _seekingStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _titleBarStyle;
    private bool _stylesInitialized;

    private float Scale => Screen.height / 1080f;

    public void Initialize(NodeRollbackHistory history, RollbackRuntimeState state)
    {
        _history = history;
        _state = state;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
            _visible = !_visible;

        if (!_visible) return;

        // 창 초기 위치 설정
        if (!_initialized)
        {
            float s = Scale;
            float width  = 640f * s;
            float height = Screen.height * 0.75f;
            _windowRect = new Rect(Screen.width - width - 20f * s, 20f * s, width, height);
            _initialized = true;
        }

        HandleDrag();
    }

    private void HandleDrag()
    {
        Vector2 mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

        float s = Scale;
        float titleBarHeight = 44f * s;
        Rect titleBarRect = new Rect(_windowRect.x, _windowRect.y, _windowRect.width, titleBarHeight);

        if (Input.GetMouseButtonDown(0) && titleBarRect.Contains(mousePos))
        {
            _isDragging = true;
            _dragOffset = mousePos - new Vector2(_windowRect.x, _windowRect.y);
        }

        if (Input.GetMouseButtonUp(0))
            _isDragging = false;

        if (_isDragging)
        {
            Vector2 newPos = mousePos - _dragOffset;
            // 화면 밖으로 나가지 않도록 클램프
            newPos.x = Mathf.Clamp(newPos.x, 0f, Screen.width  - _windowRect.width);
            newPos.y = Mathf.Clamp(newPos.y, 0f, Screen.height - _windowRect.height);
            _windowRect.x = newPos.x;
            _windowRect.y = newPos.y;
        }
    }

    private void OnGUI()
    {
        if (!_visible || _history == null) return;

        InitStyles();

        GUI.Box(_windowRect, GUIContent.none, _boxStyle);
        GUILayout.BeginArea(_windowRect);

        float s = Scale;

        // ── 타이틀 바 (드래그 핸들) ──────────────────────
        DrawTitleBar(s);

        GUILayout.Space(6f * s);

        // ── 헤더 정보 ─────────────────────────────────────
        IReadOnlyList<RollbackPoint> points = _history.Points;
        bool isSeeking = _state.IsSeeking;

        string stateLabel = isSeeking
            ? $"<color=#FF6B6B>● SEEKING</color>  →  {_state.TargetLineId}"
            : "<color=#6BFF9E>● IDLE</color>";

        GUILayout.Label($"총 {points.Count}개    {stateLabel}", _headerStyle);
        GUILayout.Space(6f * s);
        DrawSeparator(s);
        GUILayout.Space(6f * s);

        // ── 리스트 ────────────────────────────────────────
        if (points.Count == 0)
        {
            GUILayout.Label("  (비어 있음)", _normalStyle);
        }
        else
        {
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));

            for (int i = points.Count - 1; i >= 0; i--)
            {
                RollbackPoint p = points[i];

                bool isCurrent    = i == points.Count - 1;
                bool isSeekTarget = isSeeking
                                    && _state.TargetNodeName == p.nodeName
                                    && _state.TargetLineId   == p.lineId;

                GUIStyle labelStyle = isSeekTarget ? _seekingStyle
                                    : isCurrent    ? _highlightStyle
                                    :                _normalStyle;

                string badge = isSeekTarget ? "▶ TARGET"
                             : isCurrent    ? "★ NOW   "
                             :               $"  [{i:D2}] ";

                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Space(4f * s);

                GUILayout.Label($"{badge}   vi={p.visitedIndex}   frame={p.frame}", labelStyle);
                GUILayout.Label($"          node = {p.nodeName}",                   _normalStyle);
                GUILayout.Label($"          id   = {p.lineId}",                     _normalStyle);
                GUILayout.Label($"          cps  = node[{p.presentationNodeIndex}] step[{p.presentationStepIndex}]", _normalStyle);
                GUILayout.Label($"          \"{Truncate(p.rawText, 48)}\"",         _normalStyle);

                GUILayout.Space(4f * s);
                GUILayout.EndVertical();
                GUILayout.Space(4f * s);
            }

            GUILayout.EndScrollView();
        }

        // ── 하단 ──────────────────────────────────────────
        GUILayout.Space(6f * s);
        DrawSeparator(s);
        GUILayout.Space(6f * s);

        if (GUILayout.Button("닫기  (F9)", _buttonStyle, GUILayout.Height(36f * s)))
            _visible = false;

        GUILayout.Space(10f * s);
        GUILayout.EndArea();
    }

    private void DrawTitleBar(float s)
    {
        float titleBarHeight = 44f * s;

        // 드래그 중 색상 변경으로 피드백
        Color barColor = _isDragging
            ? new Color(0.25f, 0.25f, 0.35f, 1f)
            : new Color(0.15f, 0.15f, 0.22f, 1f);

        Rect barRect = GUILayoutUtility.GetRect(
            _windowRect.width, titleBarHeight,
            GUILayout.ExpandWidth(true));

        GUI.DrawTexture(barRect, MakeTex(barColor));

        // 타이틀 텍스트
        GUI.Label(barRect, "  ☰  Rollback History  —  드래그로 이동  (F9)", _titleBarStyle);
    }

    private void DrawSeparator(float s)
    {
        Rect r = GUILayoutUtility.GetRect(1f, 1f * s, GUILayout.ExpandWidth(true));
        r.height = Mathf.Max(1f, 1f * s);
        GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill,
            false, 1f, new Color(1f, 1f, 1f, 0.18f), 0f, 0f);
    }

    private static string Truncate(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Length <= max ? s : s[..max] + "…";
    }

    private void InitStyles()
    {
        if (_stylesInitialized) return;
        _stylesInitialized = true;

        float s = Scale;

        _boxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTex(new Color(0.07f, 0.07f, 0.10f, 0.94f)) }
        };

        _titleBarStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = Scaled(17, s),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal    = { textColor = new Color(0.85f, 0.85f, 0.85f) }
        };

        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = Scaled(20, s),
            fontStyle = FontStyle.Bold,
            richText  = true,
            normal    = { textColor = Color.white }
        };

        _normalStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Scaled(17, s),
            richText = true,
            normal   = { textColor = new Color(0.78f, 0.78f, 0.78f) }
        };

        _highlightStyle = new GUIStyle(_normalStyle)
        {
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(1f, 0.92f, 0.35f) }
        };

        _seekingStyle = new GUIStyle(_normalStyle)
        {
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(1f, 0.42f, 0.42f) }
        };

        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = Scaled(17, s),
            fontStyle = FontStyle.Bold,
            normal    = { textColor = Color.white }
        };
    }

    private static int Scaled(int baseSize, float scale) =>
        Mathf.Max(10, Mathf.RoundToInt(baseSize * scale));

    // 캐싱 없이 매번 생성하면 메모리 낭비이므로 딕셔너리로 캐싱
    private static readonly Dictionary<Color, Texture2D> TexCache = new();

    private static Texture2D MakeTex(Color color)
    {
        if (TexCache.TryGetValue(color, out Texture2D cached))
            return cached;

        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        TexCache[color] = tex;
        return tex;
    }
}