using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class FocusBlurCurtainGraphic : Graphic
{
    [Header("Progress")]
    [SerializeField, Range(0f, 1f)] private float _progress01;

    [Header("Curtain Shape")]
    [SerializeField] private float _openGapHeight = 520f;
    [SerializeField] private float _finalGapHeight = 0f;
    [SerializeField] private float _slantPixels = 90f;

    [Header("Soft Edge")]
    [SerializeField] private float _edgeFeatherHeight = 140f;
    [SerializeField, Range(0f, 1f)] private float _edgeFeatherAlpha = 0.55f;

    [Header("Center Blur Fake")]
    [SerializeField] private float _centerBlurHeight = 320f;
    [SerializeField, Range(0f, 1f)] private float _centerStartAlpha = 0.12f;
    [SerializeField, Range(0f, 1f)] private float _centerEndAlpha = 0.82f;
    [SerializeField] private int _centerBlurSlices = 18;

    [Header("Input")]
    [SerializeField] private bool _raycastBlocking;

    public float Progress01
    {
        get => _progress01;
        set
        {
            _progress01 = Mathf.Clamp01(value);
            SetVerticesDirty();
            ApplyRaycastState();
        }
    }

    public bool RaycastBlocking
    {
        get => _raycastBlocking;
        set
        {
            _raycastBlocking = value;
            ApplyRaycastState();
        }
    }

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
        ApplyRaycastState();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        _progress01 = Mathf.Clamp01(_progress01);
        _openGapHeight = Mathf.Max(0f, _openGapHeight);
        _finalGapHeight = Mathf.Max(0f, _finalGapHeight);
        _slantPixels = Mathf.Max(0f, _slantPixels);
        _edgeFeatherHeight = Mathf.Max(0f, _edgeFeatherHeight);
        _edgeFeatherAlpha = Mathf.Clamp01(_edgeFeatherAlpha);
        _centerBlurHeight = Mathf.Max(0f, _centerBlurHeight);
        _centerStartAlpha = Mathf.Clamp01(_centerStartAlpha);
        _centerEndAlpha = Mathf.Clamp01(_centerEndAlpha);
        _centerBlurSlices = Mathf.Max(3, _centerBlurSlices);

        raycastTarget = false;
        ApplyRaycastState();
        SetVerticesDirty();
    }
#endif

    public void Configure(
        float openGapHeight,
        float finalGapHeight,
        float slantPixels,
        float edgeFeatherHeight,
        float edgeFeatherAlpha,
        float centerBlurHeight,
        float centerStartAlpha,
        float centerEndAlpha,
        int centerBlurSlices)
    {
        _openGapHeight = Mathf.Max(0f, openGapHeight);
        _finalGapHeight = Mathf.Max(0f, finalGapHeight);
        _slantPixels = Mathf.Max(0f, slantPixels);
        _edgeFeatherHeight = Mathf.Max(0f, edgeFeatherHeight);
        _edgeFeatherAlpha = Mathf.Clamp01(edgeFeatherAlpha);
        _centerBlurHeight = Mathf.Max(0f, centerBlurHeight);
        _centerStartAlpha = Mathf.Clamp01(centerStartAlpha);
        _centerEndAlpha = Mathf.Clamp01(centerEndAlpha);
        _centerBlurSlices = Mathf.Max(3, centerBlurSlices);

        SetVerticesDirty();
    }

    public void ClearImmediate()
    {
        Progress01 = 0f;
        gameObject.SetActive(false);
    }

    public void CoverImmediate()
    {
        gameObject.SetActive(true);
        Progress01 = 1f;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (_progress01 <= 0f)
            return;

        Rect r = rectTransform.rect;

        if (r.width <= 0f || r.height <= 0f)
            return;

        float left = r.xMin;
        float right = r.xMax;
        float top = r.yMax;
        float bottom = r.yMin;
        float centerY = r.center.y;

        float close01 = EaseOutCubic(_progress01);
        float blur01 = EaseInOutSine(_progress01);

        float currentGap = Mathf.Lerp(_openGapHeight, _finalGapHeight, close01);
        currentGap = Mathf.Max(0f, currentGap);

        float halfGap = currentGap * 0.5f;

        float upperInnerY = centerY + halfGap;
        float lowerInnerY = centerY - halfGap;

        Color solid = color;
        solid.a *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_progress01 * 1.25f));

        AddUpperCurtain(vh, left, right, top, upperInnerY, _slantPixels, solid);
        AddLowerCurtain(vh, left, right, bottom, lowerInnerY, _slantPixels, solid);

        AddInnerEdgeFeather(
            vh,
            left,
            right,
            centerY,
            upperInnerY,
            lowerInnerY,
            _slantPixels,
            _edgeFeatherHeight,
            _edgeFeatherAlpha,
            solid);

        AddCenterBlurBand(
            vh,
            left,
            right,
            centerY,
            currentGap,
            blur01,
            solid);
    }

    private void AddUpperCurtain(
        VertexHelper vh,
        float left,
        float right,
        float top,
        float innerY,
        float slant,
        Color colorValue)
    {
        Vector2 p0 = new Vector2(left, top);
        Vector2 p1 = new Vector2(right, top);
        Vector2 p2 = new Vector2(right, innerY - slant);
        Vector2 p3 = new Vector2(left, innerY + slant);

        AddQuad(vh, p0, p1, p2, p3, colorValue);
    }

    private void AddLowerCurtain(
        VertexHelper vh,
        float left,
        float right,
        float bottom,
        float innerY,
        float slant,
        Color colorValue)
    {
        Vector2 p0 = new Vector2(left, innerY + slant);
        Vector2 p1 = new Vector2(right, innerY - slant);
        Vector2 p2 = new Vector2(right, bottom);
        Vector2 p3 = new Vector2(left, bottom);

        AddQuad(vh, p0, p1, p2, p3, colorValue);
    }

    private void AddInnerEdgeFeather(
        VertexHelper vh,
        float left,
        float right,
        float centerY,
        float upperInnerY,
        float lowerInnerY,
        float slant,
        float featherHeight,
        float featherAlpha,
        Color baseColor)
    {
        if (featherHeight <= 0f || featherAlpha <= 0f)
            return;

        Color dark = baseColor;
        dark.a *= featherAlpha;

        Color clear = baseColor;
        clear.a = 0f;

        float upperFeatherEndY = Mathf.Max(centerY, upperInnerY - featherHeight);
        float lowerFeatherEndY = Mathf.Min(centerY, lowerInnerY + featherHeight);

        AddGradientQuad(
            vh,
            new Vector2(left, upperInnerY + slant),
            new Vector2(right, upperInnerY - slant),
            new Vector2(right, upperFeatherEndY - slant * 0.25f),
            new Vector2(left, upperFeatherEndY + slant * 0.25f),
            dark,
            dark,
            clear,
            clear);

        AddGradientQuad(
            vh,
            new Vector2(left, lowerFeatherEndY + slant * 0.25f),
            new Vector2(right, lowerFeatherEndY - slant * 0.25f),
            new Vector2(right, lowerInnerY - slant),
            new Vector2(left, lowerInnerY + slant),
            clear,
            clear,
            dark,
            dark);
    }

    private void AddCenterBlurBand(
        VertexHelper vh,
        float left,
        float right,
        float centerY,
        float currentGap,
        float blur01,
        Color baseColor)
    {
        if (_centerBlurHeight <= 0f)
            return;

        float bandHeight = _centerBlurHeight;

        if (currentGap > 1f)
            bandHeight = Mathf.Min(_centerBlurHeight, currentGap + _edgeFeatherHeight);

        float half = bandHeight * 0.5f;

        if (half <= 0f)
            return;

        int slices = Mathf.Max(3, _centerBlurSlices);
        float sliceHeight = bandHeight / slices;

        float alpha = Mathf.Lerp(_centerStartAlpha, _centerEndAlpha, blur01);

        // 초반에는 중앙이 살짝 흐릿하게만 보이고,
        // 후반으로 갈수록 중앙 암부가 짙어진다.
        alpha *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_progress01 * 1.35f));

        for (int i = 0; i < slices; i++)
        {
            float y0 = centerY - half + sliceHeight * i;
            float y1 = y0 + sliceHeight;

            float mid = (y0 + y1) * 0.5f;
            float dist01 = Mathf.Abs(mid - centerY) / half;

            float weight = 1f - dist01;
            weight = Mathf.Clamp01(weight);
            weight = Mathf.Pow(weight, 0.65f);

            Color c = baseColor;
            c.a *= alpha * weight;

            if (c.a <= 0.001f)
                continue;

            AddQuad(
                vh,
                new Vector2(left, y0),
                new Vector2(right, y0),
                new Vector2(right, y1),
                new Vector2(left, y1),
                c);
        }
    }

    private void AddQuad(
        VertexHelper vh,
        Vector2 p0,
        Vector2 p1,
        Vector2 p2,
        Vector2 p3,
        Color colorValue)
    {
        int startIndex = vh.currentVertCount;

        UIVertex v = UIVertex.simpleVert;
        v.color = colorValue;

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
        Color c0,
        Color c1,
        Color c2,
        Color c3)
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

    private void ApplyRaycastState()
    {
        raycastTarget = _raycastBlocking && _progress01 > 0.98f;
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }

    private static float EaseInOutSine(float t)
    {
        t = Mathf.Clamp01(t);
        return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
    }
}