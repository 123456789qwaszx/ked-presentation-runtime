using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class StageMaskEdgeGraphic : Graphic
{
    [SerializeField] private StageMaskGraphic _source;
    [SerializeField] private StageMaskEdgeMode _edgeMode = StageMaskEdgeMode.Leading;
    [SerializeField] private float _thickness = 6f;

    private StageMaskKind _lastKind;
    private Vector2 _lastOffset;
    private float _lastSlantPixels;
    private bool _lastSlantToRight;
    private bool _lastFlipVertical;
    private float _lastStripHeightPixels;
    private float _lastHorizontalBleedPixels;

    public StageMaskGraphic Source
    {
        get => _source;
        set
        {
            _source = value;
            CacheSourceState();
            SetVerticesDirty();
        }
    }

    public StageMaskEdgeMode EdgeMode
    {
        get => _edgeMode;
        set
        {
            _edgeMode = value;
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

    protected override void Awake()
    {
        base.Awake();

        raycastTarget = false;
        CacheSourceState();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        _thickness = Mathf.Max(0f, _thickness);
        raycastTarget = false;

        CacheSourceState();
        SetVerticesDirty();
    }
#endif

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

        if (_edgeMode == StageMaskEdgeMode.None)
            return;

        if (_edgeMode == StageMaskEdgeMode.Leading ||
            _edgeMode == StageMaskEdgeMode.Both)
        {
            AddEdge(vh, StageMaskEdge.Leading);
        }

        if (_edgeMode == StageMaskEdgeMode.Trailing ||
            _edgeMode == StageMaskEdgeMode.Both)
        {
            AddEdge(vh, StageMaskEdge.Trailing);
        }
    }

    private void AddEdge(VertexHelper vh, StageMaskEdge edge)
    {
        if (!_source.TryGetEdge(edge, out Vector2 a, out Vector2 b))
            return;

        AddLineQuad(vh, a, b, _thickness);
    }

    private void AddLineQuad(
        VertexHelper vh,
        Vector2 a,
        Vector2 b,
        float thickness)
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

        int startIndex = vh.currentVertCount;

        UIVertex v = UIVertex.simpleVert;
        v.color = color;

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
        if (_source == null)
            return false;

        return _lastKind != _source.Kind ||
               _lastOffset != _source.ShapeOffsetPixels ||
               !Mathf.Approximately(_lastSlantPixels, _source.SlantPixels) ||
               _lastSlantToRight != _source.SlantToRight ||
               _lastFlipVertical != _source.FlipVertical ||
               !Mathf.Approximately(_lastStripHeightPixels, _source.StripHeightPixels) ||
               !Mathf.Approximately(_lastHorizontalBleedPixels, _source.HorizontalBleedPixels);
    }

    private void CacheSourceState()
    {
        if (_source == null)
            return;

        _lastKind = _source.Kind;
        _lastOffset = _source.ShapeOffsetPixels;
        _lastSlantPixels = _source.SlantPixels;
        _lastSlantToRight = _source.SlantToRight;
        _lastFlipVertical = _source.FlipVertical;
        _lastStripHeightPixels = _source.StripHeightPixels;
        _lastHorizontalBleedPixels = _source.HorizontalBleedPixels;
    }
}