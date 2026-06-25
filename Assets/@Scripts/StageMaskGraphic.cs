using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
[RequireComponent(typeof(Mask))]
public sealed class StageMaskGraphic : Graphic
{
    [Header("Common")]
    [SerializeField] private StageMaskKind _kind = StageMaskKind.FullRect;
    [SerializeField] private Vector2 _shapeOffsetPixels;
    [SerializeField] private Vector2 _hiddenOffsetPixels = new Vector2(-2200f, 0f);
    [SerializeField] private bool _hideMaskGraphic = true;

    [Header("Slanted")]
    [SerializeField] private float _slantPixels = 220f;
    [SerializeField] private bool _slantToRight = true;
    [SerializeField] private bool _flipVertical;

    [Header("Horizontal Strip")]
    [SerializeField] private float _stripHeightPixels = 320f;
    [SerializeField] private float _horizontalBleedPixels = 64f;

    public StageMaskKind Kind
    {
        get => _kind;
        set
        {
            if (_kind == value)
                return;

            _kind = value;
            SetVerticesDirty();
        }
    }

    public Vector2 ShapeOffsetPixels
    {
        get => _shapeOffsetPixels;
        set
        {
            if (_shapeOffsetPixels == value)
                return;

            _shapeOffsetPixels = value;
            SetVerticesDirty();
        }
    }

    public Vector2 HiddenOffsetPixels
    {
        get => _hiddenOffsetPixels;
        set => _hiddenOffsetPixels = value;
    }

    public bool HideMaskGraphic
    {
        get => _hideMaskGraphic;
        set
        {
            if (_hideMaskGraphic == value)
                return;

            _hideMaskGraphic = value;
            ApplyMaskGraphicVisibility();
        }
    }

    public float SlantPixels
    {
        get => _slantPixels;
        set
        {
            value = Mathf.Max(0f, value);

            if (Mathf.Approximately(_slantPixels, value))
                return;

            _slantPixels = value;
            SetVerticesDirty();
        }
    }

    public bool SlantToRight
    {
        get => _slantToRight;
        set
        {
            if (_slantToRight == value)
                return;

            _slantToRight = value;
            SetVerticesDirty();
        }
    }

    public bool FlipVertical
    {
        get => _flipVertical;
        set
        {
            if (_flipVertical == value)
                return;

            _flipVertical = value;
            SetVerticesDirty();
        }
    }

    public float StripHeightPixels
    {
        get => _stripHeightPixels;
        set
        {
            value = Mathf.Max(0f, value);

            if (Mathf.Approximately(_stripHeightPixels, value))
                return;

            _stripHeightPixels = value;
            SetVerticesDirty();
        }
    }

    public float HorizontalBleedPixels
    {
        get => _horizontalBleedPixels;
        set
        {
            value = Mathf.Max(0f, value);

            if (Mathf.Approximately(_horizontalBleedPixels, value))
                return;

            _horizontalBleedPixels = value;
            SetVerticesDirty();
        }
    }

    protected override void Awake()
    {
        base.Awake();

        raycastTarget = false;
        ApplyMaskGraphicVisibility();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        ApplyMaskGraphicVisibility();
        SetVerticesDirty();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        _slantPixels = Mathf.Max(0f, _slantPixels);
        _stripHeightPixels = Mathf.Max(0f, _stripHeightPixels);
        _horizontalBleedPixels = Mathf.Max(0f, _horizontalBleedPixels);

        raycastTarget = false;

        ApplyMaskGraphicVisibility();
        SetVerticesDirty();
    }
#endif

    public void ResetToHiddenOffset()
    {
        ShapeOffsetPixels = _hiddenOffsetPixels;
    }

    public void SetShapeOffsetImmediate(Vector2 offset)
    {
        ShapeOffsetPixels = offset;
    }

    public void SetSlanted(
        float slantPixels,
        bool slantToRight,
        bool flipVertical)
    {
        _kind = StageMaskKind.Slanted;
        _slantPixels = Mathf.Max(0f, slantPixels);
        _slantToRight = slantToRight;
        _flipVertical = flipVertical;

        SetVerticesDirty();
    }

    public void SetHorizontalStrip(
        float heightPixels,
        float horizontalBleedPixels)
    {
        _kind = StageMaskKind.HorizontalStrip;
        _stripHeightPixels = Mathf.Max(0f, heightPixels);
        _horizontalBleedPixels = Mathf.Max(0f, horizontalBleedPixels);

        SetVerticesDirty();
    }

    public bool TryGetQuadPoints(
        out Vector2 p0,
        out Vector2 p1,
        out Vector2 p2,
        out Vector2 p3)
    {
        Rect rect = rectTransform.rect;

        switch (_kind)
        {
            case StageMaskKind.FullRect:
                GetFullRectQuad(rect, out p0, out p1, out p2, out p3);
                break;

            case StageMaskKind.Slanted:
                GetSlantedQuad(rect, out p0, out p1, out p2, out p3);
                break;

            case StageMaskKind.HorizontalStrip:
                GetHorizontalStripQuad(rect, out p0, out p1, out p2, out p3);
                break;

            default:
                p0 = default;
                p1 = default;
                p2 = default;
                p3 = default;
                return false;
        }

        ApplyOffset(ref p0, ref p1, ref p2, ref p3);
        return true;
    }

    public bool TryGetEdge(
        StageMaskEdge edge,
        out Vector2 a,
        out Vector2 b)
    {
        a = default;
        b = default;

        if (!TryGetQuadPoints(out Vector2 p0, out Vector2 p1, out Vector2 p2, out Vector2 p3))
            return false;

        switch (_kind)
        {
            case StageMaskKind.FullRect:
                if (edge == StageMaskEdge.Leading)
                {
                    a = p0;
                    b = p1;
                }
                else
                {
                    a = p2;
                    b = p3;
                }

                return true;

            case StageMaskKind.Slanted:
                GetSlantedEdge(edge, p0, p1, p2, p3, out a, out b);
                return true;

            case StageMaskKind.HorizontalStrip:
                GetHorizontalStripEdge(edge, p0, p1, p2, p3, out a, out b);
                return true;

            default:
                return false;
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (!TryGetQuadPoints(out Vector2 p0, out Vector2 p1, out Vector2 p2, out Vector2 p3))
            return;

        AddQuad(vh, p0, p1, p2, p3);
    }

    private static void GetFullRectQuad(
        Rect rect,
        out Vector2 p0,
        out Vector2 p1,
        out Vector2 p2,
        out Vector2 p3)
    {
        p0 = new Vector2(rect.xMin, rect.yMin);
        p1 = new Vector2(rect.xMin, rect.yMax);
        p2 = new Vector2(rect.xMax, rect.yMax);
        p3 = new Vector2(rect.xMax, rect.yMin);
    }

    private void GetSlantedQuad(
        Rect rect,
        out Vector2 p0,
        out Vector2 p1,
        out Vector2 p2,
        out Vector2 p3)
    {
        float left = rect.xMin;
        float right = rect.xMax;
        float bottom = rect.yMin;
        float top = rect.yMax;

        if (_slantToRight)
        {
            if (!_flipVertical)
            {
                p0 = new Vector2(left, bottom);
                p1 = new Vector2(left + _slantPixels, top);
                p2 = new Vector2(right, top);
                p3 = new Vector2(right, bottom);
            }
            else
            {
                p0 = new Vector2(left + _slantPixels, bottom);
                p1 = new Vector2(left, top);
                p2 = new Vector2(right, top);
                p3 = new Vector2(right, bottom);
            }
        }
        else
        {
            if (!_flipVertical)
            {
                p0 = new Vector2(left, bottom);
                p1 = new Vector2(left, top);
                p2 = new Vector2(right - _slantPixels, top);
                p3 = new Vector2(right, bottom);
            }
            else
            {
                p0 = new Vector2(left, bottom);
                p1 = new Vector2(left, top);
                p2 = new Vector2(right, top);
                p3 = new Vector2(right - _slantPixels, bottom);
            }
        }
    }

    private void GetHorizontalStripQuad(
        Rect rect,
        out Vector2 p0,
        out Vector2 p1,
        out Vector2 p2,
        out Vector2 p3)
    {
        float halfHeight = _stripHeightPixels * 0.5f;

        float left = rect.xMin - _horizontalBleedPixels;
        float right = rect.xMax + _horizontalBleedPixels;

        float centerY = rect.center.y;
        float bottom = centerY - halfHeight;
        float top = centerY + halfHeight;

        p0 = new Vector2(left, bottom);
        p1 = new Vector2(left, top);
        p2 = new Vector2(right, top);
        p3 = new Vector2(right, bottom);
    }

    private void GetSlantedEdge(
        StageMaskEdge edge,
        Vector2 p0,
        Vector2 p1,
        Vector2 p2,
        Vector2 p3,
        out Vector2 a,
        out Vector2 b)
    {
        if (edge == StageMaskEdge.Leading)
        {
            if (_slantToRight)
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
        else
        {
            if (_slantToRight)
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
    }

    private static void GetHorizontalStripEdge(
        StageMaskEdge edge,
        Vector2 p0,
        Vector2 p1,
        Vector2 p2,
        Vector2 p3,
        out Vector2 a,
        out Vector2 b)
    {
        if (edge == StageMaskEdge.Leading)
        {
            a = p1;
            b = p2;
        }
        else
        {
            a = p0;
            b = p3;
        }
    }

    private void ApplyOffset(
        ref Vector2 p0,
        ref Vector2 p1,
        ref Vector2 p2,
        ref Vector2 p3)
    {
        p0 += _shapeOffsetPixels;
        p1 += _shapeOffsetPixels;
        p2 += _shapeOffsetPixels;
        p3 += _shapeOffsetPixels;
    }

    private void ApplyMaskGraphicVisibility()
    {
        Mask mask = GetComponent<Mask>();

        if (mask == null)
            return;

        mask.showMaskGraphic = !_hideMaskGraphic;
    }

    private void AddQuad(
        VertexHelper vh,
        Vector2 p0,
        Vector2 p1,
        Vector2 p2,
        Vector2 p3)
    {
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
}