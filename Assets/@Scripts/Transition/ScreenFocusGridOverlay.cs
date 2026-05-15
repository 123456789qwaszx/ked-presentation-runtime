using UnityEngine;

public sealed class ScreenFocusGridOverlay : MonoBehaviour
{
    [Header("Frame")]
    [SerializeField] private RectTransform _frameRoot;
    [SerializeField] private Camera _uiCamera;

    [Header("Visible")]
    [SerializeField] private bool _show = true;

    [Header("Grid")]
    [SerializeField] private bool _showGridLines = true;
    [SerializeField] private bool _showFocusPoints = true;
    [SerializeField] private bool _showLabels = true;

    [Header("Style")]
    [SerializeField] private Color _lineColor = new Color(1f, 0.85f, 0f, 0.65f);
    [SerializeField] private Color _pointColor = new Color(1f, 0.95f, 0f, 1f);
    [SerializeField] private Color _labelColor = new Color(1f, 0.95f, 0f, 1f);

    [SerializeField] private float _lineThickness = 2f;
    [SerializeField] private float _pointSize = 9f;
    [SerializeField] private int _labelFontSize = 12;

    private Texture2D _pixel;
    private GUIStyle _labelStyle;

    private void Awake()
    {
        EnsurePixel();
    }

    private void OnGUI()
    {
        if (!_show)
            return;

        EnsurePixel();
        EnsureGuiStyle();

        if (_frameRoot == null)
        {
            DrawFallbackScreenGrid();
            return;
        }

        DrawRectTransformGrid(_frameRoot);
    }

    public void SetVisible(bool visible)
    {
        _show = visible;
    }

    public void Toggle()
    {
        _show = !_show;
    }

    private void DrawRectTransformGrid(RectTransform frameRoot)
    {
        Rect rect = frameRoot.rect;

        float left = rect.xMin;
        float right = rect.xMax;
        float bottom = rect.yMin;
        float top = rect.yMax;

        float xLeft = -rect.width / 3f;
        float xRight = rect.width / 3f;
        float yTop = rect.height / 3f;
        float yBottom = -rect.height / 3f;

        Color oldColor = GUI.color;

        if (_showGridLines)
        {
            GUI.color = _lineColor;

            DrawLine(
                LocalToGuiPoint(frameRoot, new Vector2(xLeft, bottom)),
                LocalToGuiPoint(frameRoot, new Vector2(xLeft, top)),
                _lineThickness);

            DrawLine(
                LocalToGuiPoint(frameRoot, new Vector2(xRight, bottom)),
                LocalToGuiPoint(frameRoot, new Vector2(xRight, top)),
                _lineThickness);

            DrawLine(
                LocalToGuiPoint(frameRoot, new Vector2(left, yTop)),
                LocalToGuiPoint(frameRoot, new Vector2(right, yTop)),
                _lineThickness);

            DrawLine(
                LocalToGuiPoint(frameRoot, new Vector2(left, yBottom)),
                LocalToGuiPoint(frameRoot, new Vector2(right, yBottom)),
                _lineThickness);
        }

        if (_showFocusPoints)
        {
            DrawFocusPoint("top_left", LocalToGuiPoint(frameRoot, new Vector2(xLeft, yTop)));
            DrawFocusPoint("top", LocalToGuiPoint(frameRoot, new Vector2(0f, yTop)));
            DrawFocusPoint("top_right", LocalToGuiPoint(frameRoot, new Vector2(xRight, yTop)));

            DrawFocusPoint("left", LocalToGuiPoint(frameRoot, new Vector2(xLeft, 0f)));
            DrawFocusPoint("center", LocalToGuiPoint(frameRoot, Vector2.zero));
            DrawFocusPoint("right", LocalToGuiPoint(frameRoot, new Vector2(xRight, 0f)));

            DrawFocusPoint("bottom_left", LocalToGuiPoint(frameRoot, new Vector2(xLeft, yBottom)));
            DrawFocusPoint("bottom", LocalToGuiPoint(frameRoot, new Vector2(0f, yBottom)));
            DrawFocusPoint("bottom_right", LocalToGuiPoint(frameRoot, new Vector2(xRight, yBottom)));

            DrawFocusPoint("third_ul", LocalToGuiPoint(frameRoot, new Vector2(xLeft, yTop)), new Vector2(0f, -18f));
            DrawFocusPoint("third_ur", LocalToGuiPoint(frameRoot, new Vector2(xRight, yTop)), new Vector2(0f, -18f));
            DrawFocusPoint("third_ll", LocalToGuiPoint(frameRoot, new Vector2(xLeft, yBottom)), new Vector2(0f, 18f));
            DrawFocusPoint("third_lr", LocalToGuiPoint(frameRoot, new Vector2(xRight, yBottom)), new Vector2(0f, 18f));
        }

        GUI.color = oldColor;
    }

    private void DrawFallbackScreenGrid()
    {
        float w = Screen.width;
        float h = Screen.height;

        // This fallback mirrors RectTransform center-origin coordinates.
        float xLeft = w * 0.5f - w / 3f;
        float xRight = w * 0.5f + w / 3f;
        float yTop = h * 0.5f - h / 3f;
        float yBottom = h * 0.5f + h / 3f;

        float xCenter = w * 0.5f;
        float yCenter = h * 0.5f;

        Color oldColor = GUI.color;

        if (_showGridLines)
        {
            GUI.color = _lineColor;

            DrawLine(new Vector2(xLeft, 0f), new Vector2(xLeft, h), _lineThickness);
            DrawLine(new Vector2(xRight, 0f), new Vector2(xRight, h), _lineThickness);
            DrawLine(new Vector2(0f, yTop), new Vector2(w, yTop), _lineThickness);
            DrawLine(new Vector2(0f, yBottom), new Vector2(w, yBottom), _lineThickness);
        }

        if (_showFocusPoints)
        {
            DrawFocusPoint("top_left", new Vector2(xLeft, yTop));
            DrawFocusPoint("top", new Vector2(xCenter, yTop));
            DrawFocusPoint("top_right", new Vector2(xRight, yTop));

            DrawFocusPoint("left", new Vector2(xLeft, yCenter));
            DrawFocusPoint("center", new Vector2(xCenter, yCenter));
            DrawFocusPoint("right", new Vector2(xRight, yCenter));

            DrawFocusPoint("bottom_left", new Vector2(xLeft, yBottom));
            DrawFocusPoint("bottom", new Vector2(xCenter, yBottom));
            DrawFocusPoint("bottom_right", new Vector2(xRight, yBottom));

            DrawFocusPoint("third_ul", new Vector2(xLeft, yTop), new Vector2(0f, -18f));
            DrawFocusPoint("third_ur", new Vector2(xRight, yTop), new Vector2(0f, -18f));
            DrawFocusPoint("third_ll", new Vector2(xLeft, yBottom), new Vector2(0f, 18f));
            DrawFocusPoint("third_lr", new Vector2(xRight, yBottom), new Vector2(0f, 18f));
        }

        GUI.color = oldColor;
    }

    private Vector2 LocalToGuiPoint(RectTransform frameRoot, Vector2 localPoint)
    {
        Vector3 world = frameRoot.TransformPoint(new Vector3(localPoint.x, localPoint.y, 0f));
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(_uiCamera, world);

        // OnGUI uses top-left origin. Screen point uses bottom-left origin.
        return new Vector2(screen.x, Screen.height - screen.y);
    }

    private void EnsurePixel()
    {
        if (_pixel != null)
            return;

        _pixel = new Texture2D(1, 1);
        _pixel.SetPixel(0, 0, Color.white);
        _pixel.Apply();
    }

    private void EnsureGuiStyle()
    {
        if (_labelStyle != null && _labelStyle.fontSize == _labelFontSize)
            return;

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = _labelFontSize,
            fontStyle = FontStyle.Bold
        };

        _labelStyle.normal.textColor = _labelColor;
    }

    private void DrawFocusPoint(string label, Vector2 screenPoint)
    {
        DrawFocusPoint(label, screenPoint, Vector2.zero);
    }

    private void DrawFocusPoint(string label, Vector2 screenPoint, Vector2 labelOffset)
    {
        GUI.color = _pointColor;

        float half = _pointSize * 0.5f;
        GUI.DrawTexture(
            new Rect(
                screenPoint.x - half,
                screenPoint.y - half,
                _pointSize,
                _pointSize),
            _pixel);

        if (!_showLabels)
            return;

        GUI.color = _labelColor;

        Vector2 labelPosition = screenPoint + new Vector2(0f, 16f) + labelOffset;

        GUI.Label(
            new Rect(
                labelPosition.x - 60f,
                labelPosition.y - 10f,
                120f,
                20f),
            label,
            _labelStyle);
    }

    private void DrawLine(Vector2 from, Vector2 to, float thickness)
    {
        Vector2 direction = to - from;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float length = direction.magnitude;

        Matrix4x4 oldMatrix = GUI.matrix;

        GUIUtility.RotateAroundPivot(angle, from);

        GUI.DrawTexture(
            new Rect(
                from.x,
                from.y - thickness * 0.5f,
                length,
                thickness),
            _pixel);

        GUI.matrix = oldMatrix;
    }

    private void OnDestroy()
    {
        if (_pixel != null)
            Destroy(_pixel);
    }
}