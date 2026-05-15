using UnityEngine;
using UnityEngine.UI;

public enum LightSweepDirection
{
    LeftToRight = 0,
    RightToLeft = 1
}

[RequireComponent(typeof(CanvasRenderer))]
public sealed class LightSweepGraphic : Graphic
{
    [Header("Layer Progress")]
    [SerializeField, Range(0f, 1f)] private float _broadGlow01;
    [SerializeField, Range(0f, 1f)] private float _coreSweep01;
    [SerializeField, Range(0f, 1f)] private float _trailGlow01;
    [SerializeField, Range(0f, 1f)] private float _flash01;

    [Header("Shape")]
    [SerializeField] private float _broadGlowWidth = 960f;
    [SerializeField] private float _coreWidth = 180f;
    [SerializeField] private float _trailGlowWidth = 680f;
    [SerializeField] private float _slantPixels = 360f;
    [SerializeField] private float _extraTravel = 760f;
    [SerializeField] private LightSweepDirection _direction = LightSweepDirection.LeftToRight;

    [Header("Light")]
    [SerializeField, Range(0f, 1f)] private float _broadGlowAlpha = 0.34f;
    [SerializeField, Range(0f, 1f)] private float _coreAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float _trailGlowAlpha = 0.24f;
    [SerializeField, Range(0f, 1f)] private float _flashAlpha = 0.68f;

    [Header("Input")]
    [SerializeField] private bool _raycastBlocking;

    public float BroadGlow01
    {
        get => _broadGlow01;
        set
        {
            _broadGlow01 = Mathf.Clamp01(value);
            SetVerticesDirty();
            ApplyRaycastState();
        }
    }

    public float CoreSweep01
    {
        get => _coreSweep01;
        set
        {
            _coreSweep01 = Mathf.Clamp01(value);
            SetVerticesDirty();
            ApplyRaycastState();
        }
    }

    public float TrailGlow01
    {
        get => _trailGlow01;
        set
        {
            _trailGlow01 = Mathf.Clamp01(value);
            SetVerticesDirty();
            ApplyRaycastState();
        }
    }

    public float Flash01
    {
        get => _flash01;
        set
        {
            _flash01 = Mathf.Clamp01(value);
            SetVerticesDirty();
            ApplyRaycastState();
        }
    }

    public float BroadGlowWidth
    {
        get => _broadGlowWidth;
        set
        {
            _broadGlowWidth = Mathf.Max(1f, value);
            SetVerticesDirty();
        }
    }

    public float CoreWidth
    {
        get => _coreWidth;
        set
        {
            _coreWidth = Mathf.Max(1f, value);
            SetVerticesDirty();
        }
    }

    public float TrailGlowWidth
    {
        get => _trailGlowWidth;
        set
        {
            _trailGlowWidth = Mathf.Max(1f, value);
            SetVerticesDirty();
        }
    }

    public float SlantPixels
    {
        get => _slantPixels;
        set
        {
            _slantPixels = value;
            SetVerticesDirty();
        }
    }

    public float ExtraTravel
    {
        get => _extraTravel;
        set
        {
            _extraTravel = Mathf.Max(0f, value);
            SetVerticesDirty();
        }
    }

    public LightSweepDirection Direction
    {
        get => _direction;
        set
        {
            _direction = value;
            SetVerticesDirty();
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

        _broadGlow01 = Mathf.Clamp01(_broadGlow01);
        _coreSweep01 = Mathf.Clamp01(_coreSweep01);
        _trailGlow01 = Mathf.Clamp01(_trailGlow01);
        _flash01 = Mathf.Clamp01(_flash01);

        _broadGlowWidth = Mathf.Max(1f, _broadGlowWidth);
        _coreWidth = Mathf.Max(1f, _coreWidth);
        _trailGlowWidth = Mathf.Max(1f, _trailGlowWidth);
        _extraTravel = Mathf.Max(0f, _extraTravel);

        _broadGlowAlpha = Mathf.Clamp01(_broadGlowAlpha);
        _coreAlpha = Mathf.Clamp01(_coreAlpha);
        _trailGlowAlpha = Mathf.Clamp01(_trailGlowAlpha);
        _flashAlpha = Mathf.Clamp01(_flashAlpha);

        raycastTarget = false;
        ApplyRaycastState();
        SetVerticesDirty();
    }
#endif

    public void Configure(
        float broadGlowWidth,
        float coreWidth,
        float trailGlowWidth,
        float slantPixels,
        float extraTravel,
        LightSweepDirection direction,
        float broadGlowAlpha,
        float coreAlpha,
        float trailGlowAlpha,
        float flashAlpha)
    {
        _broadGlowWidth = Mathf.Max(1f, broadGlowWidth);
        _coreWidth = Mathf.Max(1f, coreWidth);
        _trailGlowWidth = Mathf.Max(1f, trailGlowWidth);
        _slantPixels = slantPixels;
        _extraTravel = Mathf.Max(0f, extraTravel);
        _direction = direction;

        _broadGlowAlpha = Mathf.Clamp01(broadGlowAlpha);
        _coreAlpha = Mathf.Clamp01(coreAlpha);
        _trailGlowAlpha = Mathf.Clamp01(trailGlowAlpha);
        _flashAlpha = Mathf.Clamp01(flashAlpha);

        SetVerticesDirty();
        ApplyRaycastState();
    }

    public void SetLayerProgress(
        float broadGlow01,
        float coreSweep01,
        float trailGlow01,
        float flash01)
    {
        _broadGlow01 = Mathf.Clamp01(broadGlow01);
        _coreSweep01 = Mathf.Clamp01(coreSweep01);
        _trailGlow01 = Mathf.Clamp01(trailGlow01);
        _flash01 = Mathf.Clamp01(flash01);

        SetVerticesDirty();
        ApplyRaycastState();
    }

    public void ClearImmediate()
    {
        SetLayerProgress(0f, 0f, 0f, 0f);
        gameObject.SetActive(false);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (!HasVisibleLayer())
            return;

        Rect r = rectTransform.rect;

        if (r.width <= 0f || r.height <= 0f)
            return;

        float left = r.xMin;
        float right = r.xMax;
        float bottom = r.yMin;
        float top = r.yMax;

        AddFullScreenFlash(vh, left, right, bottom, top);
        AddBroadGlow(vh, left, right, bottom, top);
        AddCoreSweep(vh, left, right, bottom, top);
        AddTrailGlow(vh, left, right, bottom, top);
    }

    private bool HasVisibleLayer()
    {
        return _broadGlow01 > 0f ||
               _coreSweep01 > 0f ||
               _trailGlow01 > 0f ||
               _flash01 > 0f;
    }

    private void AddFullScreenFlash(
        VertexHelper vh,
        float left,
        float right,
        float bottom,
        float top)
    {
        if (_flash01 <= 0.001f || _flashAlpha <= 0.001f)
            return;

        Color c = color;
        c.a *= _flashAlpha * _flash01;

        AddQuad(
            vh,
            new Vector2(left, bottom),
            new Vector2(left, top),
            new Vector2(right, top),
            new Vector2(right, bottom),
            c);
    }

    private void AddBroadGlow(
        VertexHelper vh,
        float left,
        float right,
        float bottom,
        float top)
    {
        if (_broadGlow01 <= 0.001f || _broadGlowAlpha <= 0.001f)
            return;

        float centerX = GetSweepCenterX(
            left,
            right,
            _broadGlowWidth,
            _broadGlow01);

        AddSoftBand(
            vh,
            bottom,
            top,
            centerX,
            _broadGlowWidth,
            _broadGlowWidth * 0.18f,
            _broadGlowAlpha,
            _broadGlow01,
            0.55f);
    }

    private void AddCoreSweep(
        VertexHelper vh,
        float left,
        float right,
        float bottom,
        float top)
    {
        if (_coreSweep01 <= 0.001f || _coreAlpha <= 0.001f)
            return;

        float coreBandWidth = Mathf.Max(_coreWidth * 3.2f, _coreWidth + 1f);

        float centerX = GetSweepCenterX(
            left,
            right,
            coreBandWidth,
            _coreSweep01);

        AddSoftBand(
            vh,
            bottom,
            top,
            centerX,
            coreBandWidth,
            _coreWidth,
            _coreAlpha,
            _coreSweep01,
            1f);
    }

    private void AddTrailGlow(
        VertexHelper vh,
        float left,
        float right,
        float bottom,
        float top)
    {
        if (_trailGlow01 <= 0.001f || _trailGlowAlpha <= 0.001f)
            return;

        float centerX = GetSweepCenterX(
            left,
            right,
            _trailGlowWidth,
            _trailGlow01);

        float alphaLife = 1f - Mathf.SmoothStep(0f, 1f, _trailGlow01);

        AddSoftBand(
            vh,
            bottom,
            top,
            centerX,
            _trailGlowWidth,
            _trailGlowWidth * 0.12f,
            _trailGlowAlpha,
            alphaLife,
            0.45f);
    }

    private float GetSweepCenterX(
        float left,
        float right,
        float width,
        float progress01)
    {
        float travelLeft = left - _extraTravel - width;
        float travelRight = right + _extraTravel + width;

        if (_direction == LightSweepDirection.RightToLeft)
            return Mathf.Lerp(travelRight, travelLeft, progress01);

        return Mathf.Lerp(travelLeft, travelRight, progress01);
    }

    private void AddSoftBand(
        VertexHelper vh,
        float bottom,
        float top,
        float centerX,
        float bandWidth,
        float coreWidth,
        float alpha,
        float lifeAlpha,
        float softness)
    {
        float safeBandWidth = Mathf.Max(1f, bandWidth);
        float safeCoreWidth = Mathf.Clamp(coreWidth, 1f, safeBandWidth);

        float halfBand = safeBandWidth * 0.5f;
        float halfCore = safeCoreWidth * 0.5f;

        float innerAlpha = Mathf.Clamp01(alpha * lifeAlpha);
        float sideAlpha = innerAlpha * Mathf.Clamp01(softness);

        if (innerAlpha <= 0.001f && sideAlpha <= 0.001f)
            return;

        float x0 = centerX - halfBand;
        float x1 = centerX - halfCore;
        float x2 = centerX + halfCore;
        float x3 = centerX + halfBand;

        float slant = _direction == LightSweepDirection.RightToLeft
            ? -_slantPixels
            : _slantPixels;

        Color clear = color;
        clear.a = 0f;

        Color side = color;
        side.a *= sideAlpha;

        Color core = color;
        core.a *= innerAlpha;

        AddGradientQuad(
            vh,
            new Vector2(x0, bottom),
            new Vector2(x0 + slant, top),
            new Vector2(x1 + slant, top),
            new Vector2(x1, bottom),
            clear,
            clear,
            side,
            side);

        AddGradientQuad(
            vh,
            new Vector2(x1, bottom),
            new Vector2(x1 + slant, top),
            new Vector2(x2 + slant, top),
            new Vector2(x2, bottom),
            side,
            side,
            core,
            core);

        AddGradientQuad(
            vh,
            new Vector2(x2, bottom),
            new Vector2(x2 + slant, top),
            new Vector2(x3 + slant, top),
            new Vector2(x3, bottom),
            core,
            core,
            clear,
            clear);
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
        raycastTarget = _raycastBlocking && HasVisibleLayer();
    }
}