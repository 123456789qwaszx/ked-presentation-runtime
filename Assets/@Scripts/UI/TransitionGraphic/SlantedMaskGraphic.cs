using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
[RequireComponent(typeof(Mask))]
public sealed class SlantedMaskGraphic : Graphic
{
    [SerializeField] private float _slantPixels = 220f;
    [SerializeField] private Vector2 _shapeOffsetPixels;
    [SerializeField] private Vector2 _hiddenOffsetPixels = new Vector2(-2200f, 0f);
    [SerializeField] private bool _slantToRight = true;
    [SerializeField] private bool _flipVertical;
    [SerializeField] private bool _hideMaskGraphic = true;

    public float SlantPixels
    {
        get => _slantPixels;
        set
        {
            _slantPixels = Mathf.Max(0f, value);
            SetVerticesDirty();
        }
    }

    public Vector2 ShapeOffsetPixels
    {
        get => _shapeOffsetPixels;
        set
        {
            _shapeOffsetPixels = value;
            SetVerticesDirty();
        }
    }

    public Vector2 HiddenOffsetPixels
    {
        get => _hiddenOffsetPixels;
        set
        {
            _hiddenOffsetPixels = value;
        }
    }

    public bool SlantToRight
    {
        get => _slantToRight;
        set
        {
            _slantToRight = value;
            SetVerticesDirty();
        }
    }

    public bool FlipVertical
    {
        get => _flipVertical;
        set
        {
            _flipVertical = value;
            SetVerticesDirty();
        }
    }

    public bool HideMaskGraphic
    {
        get => _hideMaskGraphic;
        set
        {
            _hideMaskGraphic = value;
            ApplyMaskGraphicVisibility();
        }
    }

    protected override void Awake()
    {
        base.Awake();
        ApplyMaskGraphicVisibility();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        _slantPixels = Mathf.Max(0f, _slantPixels);

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

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect r = rectTransform.rect;

        float left = r.xMin;
        float right = r.xMax;
        float bottom = r.yMin;
        float top = r.yMax;

        Vector2 p0;
        Vector2 p1;
        Vector2 p2;
        Vector2 p3;

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

        p0 += _shapeOffsetPixels;
        p1 += _shapeOffsetPixels;
        p2 += _shapeOffsetPixels;
        p3 += _shapeOffsetPixels;

        AddQuad(vh, p0, p1, p2, p3);
    }

    private void ApplyMaskGraphicVisibility()
    {
        Mask mask = GetComponent<Mask>();

        if (mask == null)
            return;

        mask.showMaskGraphic = !_hideMaskGraphic;
    }

    private void AddQuad(VertexHelper vh, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
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