using System.Text;
using Ked.Presentation.Core;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────
// 화면 지점 덧그림 — 무대를 나눈 금과 교점에 `place 슬롯 x y`에 그대로 적는 값(u)을 적는다
// (docs/PLAN_move-axes.md §10).
//
// 그리는 공간은 본 무대의 Stage_Root다 — 무대 리그에서 안 움직이는 노드이고 리그 좌표 계산의 경계다.
// place의 값은 카메라가 중립일 때의 이 좌표라, 샷이 줌 · 팬하면 인물이 격자에서 벗어나 보인다(격자는 움직이지 않는다).
// 금의 자리는 무대의 크기에서 센다(폭 1920이든 2340이든). 1u의 px는 파서와 같은 것이다(UnitToken.DefaultReferenceStageWidth).
// 이름 있는 지점은 코어 ScreenPointRatios를 부른다 — 비율 · 이름의 사본을 두지 않는다.
//
// 인물마다의 자리 · 초점 · 겨눔은 CharacterFocusDebugView가 그린다(F2).
// 개발용 — 씬의 DebugView 묶음은 태그 EditorOnly라 빌드에 들어가지 않는다.
// ─────────────────────────────────────────────────────────────────────
public sealed class ScreenFocusGridOverlay : MonoBehaviour
{
    [Header("Toggle")]
    [Tooltip("격자를 켜고 끈다(재생 중). Shift와 함께 누르면 이름 있는 지점을 켜고 끈다.")]
    [SerializeField] private KeyCode _toggleKey = KeyCode.F1;
    [SerializeField] private bool _show;
    [SerializeField] private bool _showNamedPoints;

    [Header("Stage")]
    [Tooltip("격자를 그릴 무대 — place의 값은 그 무대의 Stage_Root 좌표다.")]
    [SerializeField] private string _stageKey = "stage00";

    [Header("Grid")]
    [Tooltip("가로로 나누는 수 — 세로 금은 이 수 + 1개(가장자리 포함).")]
    [SerializeField, Min(2)] private int _columns = 4;
    [Tooltip("세로로 나누는 수 — 가로 금은 이 수 + 1개(가장자리 포함).")]
    [SerializeField, Min(2)] private int _rows = 4;
    [Tooltip("마우스 자리의 값을 적는다.")]
    [SerializeField] private bool _showMouse = true;

    [Header("Style")]
    [SerializeField] private Color _lineColor = new(1f, 0.85f, 0f, 0.65f);
    [SerializeField] private Color _edgeColor = new(1f, 0.85f, 0f, 0.35f);
    [SerializeField] private Color _pointColor = new(1f, 0.95f, 0f, 1f);
    [SerializeField] private Color _namedPointColor = new(0.65f, 1f, 1f, 1f);
    [SerializeField] private Color _textColor = new(1f, 0.95f, 0.6f, 1f);
    [SerializeField] private Color _textBackground = new(0f, 0f, 0f, 0.6f);
    [SerializeField] private float _lineThickness = 2f;
    [SerializeField] private float _pointSize = 7f;
    [SerializeField] private int _fontSize = 13;

    private StageRigRegistry _stageRigs;

    private Texture2D _pixel;
    private Texture2D _background;
    private GUIStyle _textStyle;
    private readonly StringBuilder _text = new();

    /// <summary>조립이 무대를 건넨다 — 무대 리그는 스키마가 세우므로 씬에는 격자가 겨눌 노드가 없다.</summary>
    public void Initialize(StageRigRegistry stageRigs) => _stageRigs = stageRigs;

    public void SetVisible(bool visible) => _show = visible;

    public void Toggle() => _show = !_show;

    private void Update()
    {
        if (_toggleKey == KeyCode.None || !Input.GetKeyDown(_toggleKey))
            return;

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            _showNamedPoints = !_showNamedPoints;
        else
            _show = !_show;
    }

    private void OnGUI()
    {
        if (!_show || Event.current.type != EventType.Repaint)
            return;

        if (!TryGetStageRoot(out RectTransform root, out Camera uiCamera))
            return;

        EnsureStyles();

        Color oldColor = GUI.color;

        DrawGrid(root, uiCamera);

        if (_showNamedPoints)
            DrawNamedPoints(root, uiCamera);

        if (_showMouse)
            DrawMouse(root, uiCamera);

        DrawHeader(root);

        GUI.color = oldColor;
    }

    // ── 무대 ─────────────────────────────────────────────────────────

    private bool TryGetStageRoot(out RectTransform root, out Camera uiCamera)
    {
        root = null;
        uiCamera = null;

        if (_stageRigs == null)
            return false;

        foreach (StageRigRefs stage in _stageRigs.All)
        {
            if (stage != null && string.Equals(stage.StageKey, _stageKey, System.StringComparison.Ordinal))
            {
                root = stage.Root;
                break;
            }
        }

        if (root == null)
            return false;

        Canvas canvas = root.GetComponentInParent<Canvas>();

        if (canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.rootCanvas.worldCamera;

        return true;
    }

    // ── 격자 ─────────────────────────────────────────────────────────

    private void DrawGrid(RectTransform root, Camera uiCamera)
    {
        Rect rect = root.rect;

        // 세로 금 — 가장자리(i = 0, N)는 옅게. 값은 위쪽 가장자리에.
        for (int i = 0; i <= _columns; i++)
        {
            float x = rect.xMin + rect.width * i / _columns;
            bool edge = i == 0 || i == _columns;

            GUI.color = edge ? _edgeColor : _lineColor;
            DrawLine(ToGui(root, uiCamera, new Vector2(x, rect.yMin)), ToGui(root, uiCamera, new Vector2(x, rect.yMax)), _lineThickness);

            Vector2 at = ToGui(root, uiCamera, new Vector2(x, rect.yMax));
            DrawText(U(x), at + new Vector2(edge ? (i == 0 ? 4f : -4f) : 0f, 4f), edge ? (i == 0 ? TextAnchor.UpperLeft : TextAnchor.UpperRight) : TextAnchor.UpperCenter);
        }

        // 가로 금 — 값은 왼쪽 가장자리에.
        for (int j = 0; j <= _rows; j++)
        {
            float y = rect.yMin + rect.height * j / _rows;
            bool edge = j == 0 || j == _rows;

            GUI.color = edge ? _edgeColor : _lineColor;
            DrawLine(ToGui(root, uiCamera, new Vector2(rect.xMin, y)), ToGui(root, uiCamera, new Vector2(rect.xMax, y)), _lineThickness);

            if (j == _rows)
                continue; // 위쪽 가장자리는 세로 금의 값과 겹친다 — 그 자리는 비운다

            Vector2 at = ToGui(root, uiCamera, new Vector2(rect.xMin, y));
            DrawText(U(y), at + new Vector2(4f, edge ? -4f : 0f), edge ? TextAnchor.LowerLeft : TextAnchor.MiddleLeft);
        }

        // 안쪽 교점 — place에 그대로 적는 x y.
        for (int i = 1; i < _columns; i++)
        {
            for (int j = 1; j < _rows; j++)
            {
                Vector2 local = new(rect.xMin + rect.width * i / _columns, rect.yMin + rect.height * j / _rows);
                Vector2 at = ToGui(root, uiCamera, local);

                DrawPoint(at, _pointColor, _pointSize);
                DrawText(U(local.x) + " " + U(local.y), at + new Vector2(6f, 6f), TextAnchor.UpperLeft);
            }
        }
    }

    // 코어의 13점 — 초점을 두는 곳(도구의 NodeTargets.TrySolveFocusPlacement가 쓰는 이름 · 좌표). 슬롯의 자리가 아니다.
    private void DrawNamedPoints(RectTransform root, Camera uiCamera)
    {
        Vec2 frame = new(root.rect.width, root.rect.height);

        foreach (string name in ScreenPointRatios.Names)
        {
            if (!ScreenPointRatios.TryResolve(frame, name, out Vec2 point))
                continue;

            Vector2 at = ToGui(root, uiCamera, new Vector2(point.X, point.Y));

            DrawPoint(at, _namedPointColor, _pointSize + 2f);
            DrawText($"{name}  {U(point.X)} {U(point.Y)}", at + new Vector2(0f, -8f), TextAnchor.LowerCenter);
        }
    }

    private void DrawMouse(RectTransform root, Camera uiCamera)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, Input.mousePosition, uiCamera, out Vector2 local))
            return;

        if (!root.rect.Contains(local))
            return;

        Vector2 at = ToGui(root, uiCamera, local);

        GUI.color = _pointColor;
        DrawLine(at + new Vector2(-8f, 0f), at + new Vector2(8f, 0f), 1f);
        DrawLine(at + new Vector2(0f, -8f), at + new Vector2(0f, 8f), 1f);

        DrawText($"{U(local.x)} {U(local.y)}", at + new Vector2(10f, -10f), TextAnchor.LowerLeft);
    }

    private void DrawHeader(RectTransform root)
    {
        Rect rect = root.rect;

        _text.Clear();
        _text.Append("격자 = 슬롯의 자리 (place 슬롯 x y)  ·  ").Append(_stageKey).Append(' ')
            .Append(rect.width.ToString("0")).Append(" × ").Append(rect.height.ToString("0"))
            .Append("  ·  1u = ").Append(PixelsPerUnit.ToString("0.##")).Append("px")
            .Append("  ·  카메라 중립의 좌표 — 샷이 움직이면 인물이 격자에서 벗어나 보인다");

        if (_showNamedPoints)
            _text.Append("\n하늘색 = 이름 있는 지점(초점을 두는 곳 — 도구의 TrySolveFocusPlacement). 슬롯의 자리가 아니다");

        _text.Append('\n').Append(_toggleKey).Append(" 격자 끄기  ·  Shift+").Append(_toggleKey).Append(" 이름 있는 지점");

        // 위쪽 가장자리의 값(세로 금) 아래 — 겹치지 않게 한 줄 내려 가운데에.
        DrawText(_text.ToString(), new Vector2(Screen.width * 0.5f, 30f), TextAnchor.UpperCenter);
    }

    // ── 값 ───────────────────────────────────────────────────────────

    // 파서가 u를 px로 바꾸는 그 기준 폭 — 같은 수가 나와야 대본에 그대로 적을 수 있다.
    private static float PixelsPerUnit => UnitToken.PixelsPerUnit(UnitToken.DefaultReferenceStageWidth);

    private static string U(float pixels) => UnitToken.FormatUnits(pixels, UnitToken.DefaultReferenceStageWidth);

    // ── 그리기 ───────────────────────────────────────────────────────

    private static Vector2 ToGui(RectTransform root, Camera uiCamera, Vector2 local)
    {
        Vector3 world = root.TransformPoint(new Vector3(local.x, local.y, 0f));
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, world);

        // OnGUI는 왼쪽 위가 원점이다. 화면 좌표는 왼쪽 아래가 원점이다.
        return new Vector2(screen.x, Screen.height - screen.y);
    }

    private void DrawText(string text, Vector2 at, TextAnchor anchor)
    {
        GUIContent content = new(text);
        Vector2 size = _textStyle.CalcSize(content);

        float x = anchor switch
        {
            TextAnchor.UpperCenter or TextAnchor.MiddleCenter or TextAnchor.LowerCenter => at.x - size.x * 0.5f,
            TextAnchor.UpperRight or TextAnchor.MiddleRight or TextAnchor.LowerRight => at.x - size.x,
            _ => at.x,
        };

        float y = anchor switch
        {
            TextAnchor.MiddleLeft or TextAnchor.MiddleCenter or TextAnchor.MiddleRight => at.y - size.y * 0.5f,
            TextAnchor.LowerLeft or TextAnchor.LowerCenter or TextAnchor.LowerRight => at.y - size.y,
            _ => at.y,
        };

        GUI.color = Color.white;
        GUI.Label(new Rect(x, y, size.x, size.y), content, _textStyle);
    }

    private void DrawPoint(Vector2 at, Color color, float size)
    {
        GUI.color = color;

        float s = Mathf.Max(1f, size);
        GUI.DrawTexture(new Rect(at.x - s * 0.5f, at.y - s * 0.5f, s, s), _pixel);
    }

    private void DrawLine(Vector2 from, Vector2 to, float thickness)
    {
        Vector2 direction = to - from;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        Matrix4x4 oldMatrix = GUI.matrix;

        GUIUtility.RotateAroundPivot(angle, from);
        GUI.DrawTexture(new Rect(from.x, from.y - thickness * 0.5f, direction.magnitude, thickness), _pixel);

        GUI.matrix = oldMatrix;
    }

    private void EnsureStyles()
    {
        if (_pixel == null)
            _pixel = SolidTexture(Color.white);

        if (_background == null)
            _background = SolidTexture(_textBackground);

        if (_textStyle != null && _textStyle.fontSize == _fontSize)
            return;

        _textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = _fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = false,
            padding = new RectOffset(4, 4, 2, 2),
        };

        _textStyle.normal.textColor = _textColor;
        _textStyle.normal.background = _background;
    }

    private static Texture2D SolidTexture(Color color)
    {
        Texture2D texture = new(1, 1) { hideFlags = HideFlags.DontSave };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void OnValidate()
    {
        // 인스펙터에서 바꾼 색 · 글 크기를 다음 그리기에 쓴다.
        if (_background != null)
        {
            _background.SetPixel(0, 0, _textBackground);
            _background.Apply();
        }

        _textStyle = null;
    }

    private void OnDestroy()
    {
        if (_pixel != null)
            Destroy(_pixel);

        if (_background != null)
            Destroy(_background);
    }
}
