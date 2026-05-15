using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class SlantedShutterGraphic : Graphic
{
    [Header("Progress")]
    [SerializeField, Range(0f, 1f)] private float _progress01;

    [Header("Shape")]
    [SerializeField] private float _slantPixels = 140f;

    [Tooltip("닫히기 전 중앙에 남는 틈의 높이입니다.")]
    [SerializeField] private float _openGapHeight = 420f;

    [Tooltip("닫힐 때 마지막까지 남는 중앙 틈의 높이입니다.")]
    [SerializeField] private float _finalGapHeight = 0f;

    [Header("Center Exposure")]
    [Tooltip("중앙 흐림 밴드의 최대 높이입니다.")]
    [SerializeField] private float _centerBandHeight = 260f;

    [Tooltip("중앙 흐림 밴드가 처음 나타날 때의 알파입니다.")]
    [SerializeField, Range(0f, 1f)] private float _centerStartAlpha = 0.25f;

    [Tooltip("중앙 흐림 밴드가 최종적으로 닫힐 때의 알파입니다.")]
    [SerializeField, Range(0f, 1f)] private float _centerEndAlpha = 1f;

    [Header("Options")]
    [SerializeField] private bool _raycastBlocking;

    public float Progress01
    {
        get => _progress01;
        set
        {
            _progress01 = Mathf.Clamp01(value);
            SetVerticesDirty();
        }
    }

    public float SlantPixels
    {
        get => _slantPixels;
        set
        {
            _slantPixels = Mathf.Max(0f, value);
            SetVerticesDirty();
        }
    }

    public float OpenGapHeight
    {
        get => _openGapHeight;
        set
        {
            _openGapHeight = Mathf.Max(0f, value);
            SetVerticesDirty();
        }
    }

    public float FinalGapHeight
    {
        get => _finalGapHeight;
        set
        {
            _finalGapHeight = Mathf.Max(0f, value);
            SetVerticesDirty();
        }
    }

    public float CenterBandHeight
    {
        get => _centerBandHeight;
        set
        {
            _centerBandHeight = Mathf.Max(0f, value);
            SetVerticesDirty();
        }
    }

    public float CenterStartAlpha
    {
        get => _centerStartAlpha;
        set
        {
            _centerStartAlpha = Mathf.Clamp01(value);
            SetVerticesDirty();
        }
    }

    public float CenterEndAlpha
    {
        get => _centerEndAlpha;
        set
        {
            _centerEndAlpha = Mathf.Clamp01(value);
            SetVerticesDirty();
        }
    }

    public bool RaycastBlocking
    {
        get => _raycastBlocking;
        set
        {
            _raycastBlocking = value;
            raycastTarget = value;
        }
    }

    protected override void Awake()
    {
        base.Awake();

        raycastTarget = _raycastBlocking;
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        _progress01 = Mathf.Clamp01(_progress01);
        _slantPixels = Mathf.Max(0f, _slantPixels);
        _openGapHeight = Mathf.Max(0f, _openGapHeight);
        _finalGapHeight = Mathf.Max(0f, _finalGapHeight);
        _centerBandHeight = Mathf.Max(0f, _centerBandHeight);
        _centerStartAlpha = Mathf.Clamp01(_centerStartAlpha);
        _centerEndAlpha = Mathf.Clamp01(_centerEndAlpha);

        raycastTarget = _raycastBlocking;

        SetVerticesDirty();
    }
#endif

    public void Configure(
        float slantPixels,
        float openGapHeight,
        float finalGapHeight,
        float centerBandHeight,
        float centerStartAlpha,
        float centerEndAlpha)
    {
        _slantPixels = Mathf.Max(0f, slantPixels);
        _openGapHeight = Mathf.Max(0f, openGapHeight);
        _finalGapHeight = Mathf.Max(0f, finalGapHeight);
        _centerBandHeight = Mathf.Max(0f, centerBandHeight);
        _centerStartAlpha = Mathf.Clamp01(centerStartAlpha);
        _centerEndAlpha = Mathf.Clamp01(centerEndAlpha);

        SetVerticesDirty();
    }

    public void ClearImmediate()
    {
        Progress01 = 0f;
    }

    public void CoverImmediate()
    {
        Progress01 = 1f;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (_progress01 <= 0f)
            return;

        Rect r = rectTransform.rect;

        float left = r.xMin;
        float right = r.xMax;
        float bottom = r.yMin;
        float top = r.yMax;
        float centerY = r.center.y;

        float width = r.width;
        float height = r.height;

        if (width <= 0f || height <= 0f)
            return;

        float eased = EaseOutCameraSnap(_progress01);

        float currentGap = Mathf.Lerp(_openGapHeight, _finalGapHeight, eased);
        currentGap = Mathf.Max(0f, currentGap);

        float halfGap = currentGap * 0.5f;

        float upperInnerY = centerY + halfGap;
        float lowerInnerY = centerY - halfGap;

        Color shutterColor = color;
        shutterColor.a *= 1f;

        AddUpperShutter(vh, left, right, top, upperInnerY, _slantPixels, shutterColor);
        AddLowerShutter(vh, left, right, bottom, lowerInnerY, _slantPixels, shutterColor);

        AddCenterExposureBand(vh, left, right, centerY, currentGap, eased);
    }

    private void AddUpperShutter(
        VertexHelper vh,
        float left,
        float right,
        float top,
        float innerY,
        float slant,
        Color32 c)
    {
        Vector2 p0 = new Vector2(left, top);
        Vector2 p1 = new Vector2(right, top);
        Vector2 p2 = new Vector2(right, innerY - slant);
        Vector2 p3 = new Vector2(left, innerY + slant);

        AddQuad(vh, p0, p1, p2, p3, c);
    }

    private void AddLowerShutter(
        VertexHelper vh,
        float left,
        float right,
        float bottom,
        float innerY,
        float slant,
        Color32 c)
    {
        Vector2 p0 = new Vector2(left, innerY + slant);
        Vector2 p1 = new Vector2(right, innerY - slant);
        Vector2 p2 = new Vector2(right, bottom);
        Vector2 p3 = new Vector2(left, bottom);

        AddQuad(vh, p0, p1, p2, p3, c);
    }

    private void AddCenterExposureBand(
        VertexHelper vh,
        float left,
        float right,
        float centerY,
        float currentGap,
        float eased)
    {
        if (_centerBandHeight <= 0f)
            return;

        float visibleGap01 = _openGapHeight <= 0f
            ? 0f
            : Mathf.Clamp01(currentGap / _openGapHeight);

        if (visibleGap01 <= 0f)
            return;

        float bandHeight = Mathf.Min(_centerBandHeight, currentGap);
        float halfBand = bandHeight * 0.5f;

        float alpha = Mathf.Lerp(_centerStartAlpha, _centerEndAlpha, eased);

        Color centerColor = color;
        centerColor.a *= alpha;

        Color transparent = centerColor;
        transparent.a = 0f;

        float y0 = centerY - halfBand;
        float y1 = centerY - halfBand * 0.25f;
        float y2 = centerY + halfBand * 0.25f;
        float y3 = centerY + halfBand;

        AddGradientQuad(
            vh,
            new Vector2(left, y0),
            new Vector2(right, y0),
            new Vector2(right, y1),
            new Vector2(left, y1),
            transparent,
            transparent,
            centerColor,
            centerColor);

        AddQuad(
            vh,
            new Vector2(left, y1),
            new Vector2(right, y1),
            new Vector2(right, y2),
            new Vector2(left, y2),
            centerColor);

        AddGradientQuad(
            vh,
            new Vector2(left, y2),
            new Vector2(right, y2),
            new Vector2(right, y3),
            new Vector2(left, y3),
            centerColor,
            centerColor,
            transparent,
            transparent);
    }

    private void AddQuad(
        VertexHelper vh,
        Vector2 p0,
        Vector2 p1,
        Vector2 p2,
        Vector2 p3,
        Color32 c)
    {
        int startIndex = vh.currentVertCount;

        UIVertex v = UIVertex.simpleVert;
        v.color = c;

        v.position = p0;
        vh.AddVert(v);

        v.position = p1;
        vh.AddVert(v);

        v.position = p2;
        vh.AddVert(v);

        v.position = p3;
        vh.AddVert(v);

        vh.AddTriangle(startIndex + 0, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex + 0, startIndex + 2, startIndex + 3);
    }

    private void AddGradientQuad(
        VertexHelper vh,
        Vector2 p0,
        Vector2 p1,
        Vector2 p2,
        Vector2 p3,
        Color32 c0,
        Color32 c1,
        Color32 c2,
        Color32 c3)
    {
        int startIndex = vh.currentVertCount;

        UIVertex v = UIVertex.simpleVert;

        v.position = p0;
        v.color = c0;
        vh.AddVert(v);

        v.position = p1;
        v.color = c1;
        vh.AddVert(v);

        v.position = p2;
        v.color = c2;
        vh.AddVert(v);

        v.position = p3;
        v.color = c3;
        vh.AddVert(v);

        vh.AddTriangle(startIndex + 0, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex + 0, startIndex + 2, startIndex + 3);
    }

    private static float EaseOutCameraSnap(float t)
    {
        t = Mathf.Clamp01(t);

        // 초반은 빠르게 닫히고, 후반은 살짝 붙는 느낌.
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }
}