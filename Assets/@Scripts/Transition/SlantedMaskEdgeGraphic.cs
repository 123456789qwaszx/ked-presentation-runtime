using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class SlantedMaskEdgeGraphic : Graphic
{
    [SerializeField] private SlantedMaskGraphic _source;
    [SerializeField] private float _thickness = 6f;
    [SerializeField] private bool _drawLeadingEdge = true;

    private Vector2 _lastOffset;
    private float _lastSlant;
    private bool _lastSlantToRight;
    private bool _lastFlipVertical;

    public SlantedMaskGraphic Source
    {
        get => _source;
        set
        {
            _source = value;
            CacheSourceState();
            SetVerticesDirty();
        }
    }

    public float Thickness
    {
        get => _thickness;
        set
        {
            _thickness = Mathf.Max(0f, value);
            SetVerticesDirty();
        }
    }

    public bool DrawLeadingEdge
    {
        get => _drawLeadingEdge;
        set
        {
            _drawLeadingEdge = value;
            SetVerticesDirty();
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        _thickness = Mathf.Max(0f, _thickness);
        CacheSourceState();
        SetVerticesDirty();
    }
#endif

    protected override void Awake()
    {
        base.Awake();
        CacheSourceState();
    }

    private void LateUpdate()
    {
        if (_source == null)
            return;

        if (!HasSourceStateChanged())
            return;

        CacheSourceState();
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (_source == null)
            return;

        Rect r = rectTransform.rect;

        float left = r.xMin;
        float right = r.xMax;
        float bottom = r.yMin;
        float top = r.yMax;

        Vector2 p0;
        Vector2 p1;
        Vector2 p2;
        Vector2 p3;

        float slant = _source.SlantPixels;
        Vector2 offset = _source.ShapeOffsetPixels;

        if (_source.SlantToRight)
        {
            if (!_source.FlipVertical)
            {
                p0 = new Vector2(left, bottom);
                p1 = new Vector2(left + slant, top);
                p2 = new Vector2(right, top);
                p3 = new Vector2(right, bottom);
            }
            else
            {
                p0 = new Vector2(left + slant, bottom);
                p1 = new Vector2(left, top);
                p2 = new Vector2(right, top);
                p3 = new Vector2(right, bottom);
            }
        }
        else
        {
            if (!_source.FlipVertical)
            {
                p0 = new Vector2(left, bottom);
                p1 = new Vector2(left, top);
                p2 = new Vector2(right - slant, top);
                p3 = new Vector2(right, bottom);
            }
            else
            {
                p0 = new Vector2(left, bottom);
                p1 = new Vector2(left, top);
                p2 = new Vector2(right, top);
                p3 = new Vector2(right - slant, bottom);
            }
        }

        p0 += offset;
        p1 += offset;
        p2 += offset;
        p3 += offset;

        Vector2 a;
        Vector2 b;

        if (_drawLeadingEdge)
        {
            GetLeadingEdge(p0, p1, p2, p3, out a, out b);
        }
        else
        {
            GetTrailingEdge(p0, p1, p2, p3, out a, out b);
        }

        AddLineQuad(vh, a, b, _thickness);
    }

    private void GetLeadingEdge(
        Vector2 p0,
        Vector2 p1,
        Vector2 p2,
        Vector2 p3,
        out Vector2 a,
        out Vector2 b)
    {
        if (_source.SlantToRight)
        {
            a = p0;
            b = p1;
        }
        else
        {
            a = p2;
            b = p3;
        }
    }

    private void GetTrailingEdge(
        Vector2 p0,
        Vector2 p1,
        Vector2 p2,
        Vector2 p3,
        out Vector2 a,
        out Vector2 b)
    {
        if (_source.SlantToRight)
        {
            a = p2;
            b = p3;
        }
        else
        {
            a = p0;
            b = p1;
        }
    }

    private void AddLineQuad(VertexHelper vh, Vector2 a, Vector2 b, float thickness)
    {
        if (thickness <= 0f)
            return;

        Vector2 dir = b - a;

        if (dir.sqrMagnitude <= 0.0001f)
            return;

        dir.Normalize();

        Vector2 normal = new Vector2(-dir.y, dir.x);
        Vector2 half = normal * (thickness * 0.5f);

        Vector2 p0 = a - half;
        Vector2 p1 = a + half;
        Vector2 p2 = b + half;
        Vector2 p3 = b - half;

        UIVertex v = UIVertex.simpleVert;
        v.color = color;

        int startIndex = vh.currentVertCount;

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

    private bool HasSourceStateChanged()
    {
        return _lastOffset != _source.ShapeOffsetPixels ||
               !Mathf.Approximately(_lastSlant, _source.SlantPixels) ||
               _lastSlantToRight != _source.SlantToRight ||
               _lastFlipVertical != _source.FlipVertical;
    }

    private void CacheSourceState()
    {
        if (_source == null)
            return;

        _lastOffset = _source.ShapeOffsetPixels;
        _lastSlant = _source.SlantPixels;
        _lastSlantToRight = _source.SlantToRight;
        _lastFlipVertical = _source.FlipVertical;
    }
}