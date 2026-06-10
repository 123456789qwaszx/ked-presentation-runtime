using UnityEngine;
using UnityEngine.Serialization;

public sealed class ScreenFocusGridOverlay : MonoBehaviour
{
    [Header("Frame")]
    [SerializeField] private RectTransform StageShot_Root;
    private Camera _uiCamera;

    [Header("Visible")]
    [SerializeField] private bool _show = true;

    [Header("Grid")]
    [SerializeField] private bool _showGridLines = true;
    [SerializeField] private bool _showFocusPoints = true;
    [SerializeField] private bool _showLabels = true;

    [Header("Style")]
    [SerializeField] private Color _lineColor = new Color(1f, 0.85f, 0f, 0.65f);
    [SerializeField] private Color _innerLineColor = new Color(1f, 0.95f, 0f, 0.35f);
    [SerializeField] private Color _pointColor = new Color(1f, 0.95f, 0f, 1f);
    [SerializeField] private Color _innerPointColor = new Color(0.65f, 1f, 1f, 1f);
    [SerializeField] private Color _labelColor = new Color(1f, 0.95f, 0f, 1f);

    [SerializeField] private float _lineThickness = 2f;
    [SerializeField] private float _innerLineThickness = 1f;
    [SerializeField] private float _pointSize = 9f;
    [SerializeField] private float _innerPointSize = 7f;
    [SerializeField] private int _labelFontSize = 12;

    private Texture2D _pixel;
    private GUIStyle _labelStyle;

    // ScreenFocusPointResolver와 반드시 같은 값으로 유지.
    // Outer: 9-point focus zone
    // Inner: thirds 계열 보조 focus zone
    private const float OuterXRatio = 0.24f;
    private const float OuterYRatio = 0.16f;

    private const float InnerXRatio = 0.14f;
    private const float InnerYRatio = 0.09f;

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

        if (StageShot_Root == null)
        {
            DrawFallbackScreenGrid();
            return;
        }

        DrawRectTransformGrid(StageShot_Root);
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

        float outerXLeft = -rect.width * OuterXRatio;
        float outerXRight = rect.width * OuterXRatio;
        float outerYTop = rect.height * OuterYRatio;
        float outerYBottom = -rect.height * OuterYRatio;

        float innerXLeft = -rect.width * InnerXRatio;
        float innerXRight = rect.width * InnerXRatio;
        float innerYTop = rect.height * InnerYRatio;
        float innerYBottom = -rect.height * InnerYRatio;

        Color oldColor = GUI.color;

        if (_showGridLines)
        {
            DrawGridLines(
                frameRoot,
                left,
                right,
                bottom,
                top,
                outerXLeft,
                outerXRight,
                outerYTop,
                outerYBottom,
                _lineColor,
                _lineThickness);

            DrawGridLines(
                frameRoot,
                left,
                right,
                bottom,
                top,
                innerXLeft,
                innerXRight,
                innerYTop,
                innerYBottom,
                _innerLineColor,
                _innerLineThickness);
        }

        if (_showFocusPoints)
        {
            DrawFocusPoint("top_left", LocalToGuiPoint(frameRoot, new Vector2(outerXLeft, outerYTop)));
            DrawFocusPoint("top", LocalToGuiPoint(frameRoot, new Vector2(0f, outerYTop)));
            DrawFocusPoint("top_right", LocalToGuiPoint(frameRoot, new Vector2(outerXRight, outerYTop)));

            DrawFocusPoint("left", LocalToGuiPoint(frameRoot, new Vector2(outerXLeft, 0f)));
            DrawFocusPoint("center", LocalToGuiPoint(frameRoot, Vector2.zero));
            DrawFocusPoint("right", LocalToGuiPoint(frameRoot, new Vector2(outerXRight, 0f)));

            DrawFocusPoint("bottom_left", LocalToGuiPoint(frameRoot, new Vector2(outerXLeft, outerYBottom)));
            DrawFocusPoint("bottom", LocalToGuiPoint(frameRoot, new Vector2(0f, outerYBottom)));
            DrawFocusPoint("bottom_right", LocalToGuiPoint(frameRoot, new Vector2(outerXRight, outerYBottom)));

            DrawFocusPoint(
                "third_ul",
                LocalToGuiPoint(frameRoot, new Vector2(innerXLeft, innerYTop)),
                new Vector2(0f, -18f),
                _innerPointColor,
                _innerPointSize);

            DrawFocusPoint(
                "third_ur",
                LocalToGuiPoint(frameRoot, new Vector2(innerXRight, innerYTop)),
                new Vector2(0f, -18f),
                _innerPointColor,
                _innerPointSize);

            DrawFocusPoint(
                "third_ll",
                LocalToGuiPoint(frameRoot, new Vector2(innerXLeft, innerYBottom)),
                new Vector2(0f, 18f),
                _innerPointColor,
                _innerPointSize);

            DrawFocusPoint(
                "third_lr",
                LocalToGuiPoint(frameRoot, new Vector2(innerXRight, innerYBottom)),
                new Vector2(0f, 18f),
                _innerPointColor,
                _innerPointSize);
        }

        GUI.color = oldColor;
    }

    private void DrawFallbackScreenGrid()
    {
        float w = Screen.width;
        float h = Screen.height;

        float xCenter = w * 0.5f;
        float yCenter = h * 0.5f;

        float outerXLeft = xCenter - w * OuterXRatio;
        float outerXRight = xCenter + w * OuterXRatio;
        float outerYTop = yCenter - h * OuterYRatio;
        float outerYBottom = yCenter + h * OuterYRatio;

        float innerXLeft = xCenter - w * InnerXRatio;
        float innerXRight = xCenter + w * InnerXRatio;
        float innerYTop = yCenter - h * InnerYRatio;
        float innerYBottom = yCenter + h * InnerYRatio;

        Color oldColor = GUI.color;

        if (_showGridLines)
        {
            DrawScreenGridLines(
                w,
                h,
                outerXLeft,
                outerXRight,
                outerYTop,
                outerYBottom,
                _lineColor,
                _lineThickness);

            DrawScreenGridLines(
                w,
                h,
                innerXLeft,
                innerXRight,
                innerYTop,
                innerYBottom,
                _innerLineColor,
                _innerLineThickness);
        }

        if (_showFocusPoints)
        {
            DrawFocusPoint("top_left", new Vector2(outerXLeft, outerYTop));
            DrawFocusPoint("top", new Vector2(xCenter, outerYTop));
            DrawFocusPoint("top_right", new Vector2(outerXRight, outerYTop));

            DrawFocusPoint("left", new Vector2(outerXLeft, yCenter));
            DrawFocusPoint("center", new Vector2(xCenter, yCenter));
            DrawFocusPoint("right", new Vector2(outerXRight, yCenter));

            DrawFocusPoint("bottom_left", new Vector2(outerXLeft, outerYBottom));
            DrawFocusPoint("bottom", new Vector2(xCenter, outerYBottom));
            DrawFocusPoint("bottom_right", new Vector2(outerXRight, outerYBottom));

            DrawFocusPoint(
                "third_ul",
                new Vector2(innerXLeft, innerYTop),
                new Vector2(0f, -18f),
                _innerPointColor,
                _innerPointSize);

            DrawFocusPoint(
                "third_ur",
                new Vector2(innerXRight, innerYTop),
                new Vector2(0f, -18f),
                _innerPointColor,
                _innerPointSize);

            DrawFocusPoint(
                "third_ll",
                new Vector2(innerXLeft, innerYBottom),
                new Vector2(0f, 18f),
                _innerPointColor,
                _innerPointSize);

            DrawFocusPoint(
                "third_lr",
                new Vector2(innerXRight, innerYBottom),
                new Vector2(0f, 18f),
                _innerPointColor,
                _innerPointSize);
        }

        GUI.color = oldColor;
    }

    private void DrawGridLines(
        RectTransform frameRoot,
        float left,
        float right,
        float bottom,
        float top,
        float xLeft,
        float xRight,
        float yTop,
        float yBottom,
        Color color,
        float thickness)
    {
        GUI.color = color;

        DrawLine(
            LocalToGuiPoint(frameRoot, new Vector2(xLeft, bottom)),
            LocalToGuiPoint(frameRoot, new Vector2(xLeft, top)),
            thickness);

        DrawLine(
            LocalToGuiPoint(frameRoot, new Vector2(xRight, bottom)),
            LocalToGuiPoint(frameRoot, new Vector2(xRight, top)),
            thickness);

        DrawLine(
            LocalToGuiPoint(frameRoot, new Vector2(left, yTop)),
            LocalToGuiPoint(frameRoot, new Vector2(right, yTop)),
            thickness);

        DrawLine(
            LocalToGuiPoint(frameRoot, new Vector2(left, yBottom)),
            LocalToGuiPoint(frameRoot, new Vector2(right, yBottom)),
            thickness);
    }

    private void DrawScreenGridLines(
        float width,
        float height,
        float xLeft,
        float xRight,
        float yTop,
        float yBottom,
        Color color,
        float thickness)
    {
        GUI.color = color;

        DrawLine(new Vector2(xLeft, 0f), new Vector2(xLeft, height), thickness);
        DrawLine(new Vector2(xRight, 0f), new Vector2(xRight, height), thickness);
        DrawLine(new Vector2(0f, yTop), new Vector2(width, yTop), thickness);
        DrawLine(new Vector2(0f, yBottom), new Vector2(width, yBottom), thickness);
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
        {
            _labelStyle.normal.textColor = _labelColor;
            return;
        }

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
        DrawFocusPoint(label, screenPoint, Vector2.zero, _pointColor, _pointSize);
    }

    private void DrawFocusPoint(string label, Vector2 screenPoint, Vector2 labelOffset)
    {
        DrawFocusPoint(label, screenPoint, labelOffset, _pointColor, _pointSize);
    }

    private void DrawFocusPoint(
        string label,
        Vector2 screenPoint,
        Vector2 labelOffset,
        Color pointColor,
        float pointSize)
    {
        GUI.color = pointColor;

        float size = Mathf.Max(1f, pointSize);
        float half = size * 0.5f;

        GUI.DrawTexture(
            new Rect(
                screenPoint.x - half,
                screenPoint.y - half,
                size,
                size),
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