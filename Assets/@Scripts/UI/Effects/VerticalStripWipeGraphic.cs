using UnityEngine;
using UnityEngine.UI;

public enum VerticalStripWipeOrder
{
    LeftToRight = 0,
    RightToLeft = 1
}

[RequireComponent(typeof(CanvasRenderer))]
public sealed class VerticalStripWipeGraphic : Graphic
{
    [SerializeField] private int _stripCount = 20;
    [SerializeField] private float _stripDelay = 0.02f;
    [SerializeField] private float _stripFillDuration = 0.08f;
    [SerializeField] private VerticalStripWipeOrder _order = VerticalStripWipeOrder.LeftToRight;
    [SerializeField, Range(0f, 1f)] private float _progress01;

    
    protected override void Awake()
    {
        base.Awake();

        raycastTarget = false;
    }
    
    public int StripCount
    {
        get => _stripCount;
        set
        {
            _stripCount = Mathf.Max(1, value);
            SetVerticesDirty();
        }
    }

    public float Progress01
    {
        get => _progress01;
        set
        {
            _progress01 = Mathf.Clamp01(value);
            SetVerticesDirty();
        }
    }

    public float TotalDuration
    {
        get
        {
            int count = Mathf.Max(1, _stripCount);
            float delay = Mathf.Max(0f, _stripDelay);
            float fill = Mathf.Max(0.001f, _stripFillDuration);

            return fill + delay * (count - 1);
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        _stripCount = Mathf.Max(1, _stripCount);
        _stripDelay = Mathf.Max(0f, _stripDelay);
        _stripFillDuration = Mathf.Max(0.001f, _stripFillDuration);
        _progress01 = Mathf.Clamp01(_progress01);

        SetVerticesDirty();
    }
#endif

    public void Configure(
        int stripCount,
        float stripDelay,
        float stripFillDuration,
        VerticalStripWipeOrder order)
    {
        _stripCount = Mathf.Max(1, stripCount);
        _stripDelay = Mathf.Max(0f, stripDelay);
        _stripFillDuration = Mathf.Max(0.001f, stripFillDuration);
        _order = order;

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

        if (_stripCount <= 0)
            return;

        if (_progress01 <= 0f)
            return;

        Rect r = rectTransform.rect;

        float left = r.xMin;
        float right = r.xMax;
        float bottom = r.yMin;
        float top = r.yMax;
        float width = r.width;

        if (width <= 0f || r.height <= 0f)
            return;

        int count = Mathf.Max(1, _stripCount);
        float stripWidth = width / count;
        float totalTime = TotalDuration;
        float currentTime = totalTime * _progress01;

        for (int i = 0; i < count; i++)
        {
            int visualIndex = GetVisualIndex(i, count);
            float startTime = visualIndex * _stripDelay;
            float localTime = currentTime - startTime;
            float fill01 = Mathf.Clamp01(localTime / _stripFillDuration);

            if (fill01 <= 0f)
                continue;

            float x0 = left + i * stripWidth;
            float x1 = i == count - 1
                ? right
                : x0 + stripWidth;

            AddStrip(vh, x0, x1, bottom, top, fill01);
        }
    }

    private int GetVisualIndex(int stripIndex, int stripCount)
    {
        if (_order == VerticalStripWipeOrder.RightToLeft)
            return stripCount - 1 - stripIndex;

        return stripIndex;
    }

    private void AddStrip(
        VertexHelper vh,
        float x0,
        float x1,
        float bottom,
        float top,
        float fill01)
    {
        fill01 = Mathf.Clamp01(fill01);

        float filledX0 = x0;
        float filledX1 = Mathf.Lerp(x0, x1, fill01);

        AddQuad(
            vh,
            new Vector2(filledX0, bottom),
            new Vector2(filledX0, top),
            new Vector2(filledX1, top),
            new Vector2(filledX1, bottom));
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