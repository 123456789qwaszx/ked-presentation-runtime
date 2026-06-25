using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StageMaskSlot : MonoBehaviour
{
    [SerializeField] private StageMaskGraphic _graphic;
    [SerializeField] private Mask _mask;

    public StageMaskGraphic Graphic => _graphic;
    public Mask Mask => _mask;

    public bool HasMask => _graphic != null && _mask != null;

    private void Awake()
    {
        CacheRefs();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheRefs();
    }
#endif

    public void SetKind(StageMaskKind kind)
    {
        if (_graphic == null)
            return;

        _graphic.Kind = kind;
    }

    public void SetFullVisible()
    {
        if (_mask != null)
            _mask.enabled = false;

        if (_graphic != null)
            _graphic.enabled = false;
    }

    public void SetMaskedFullRectVisible()
    {
        if (!HasMask)
            return;

        _graphic.enabled = true;
        _mask.enabled = true;

        _graphic.Kind = StageMaskKind.FullRect;
        _graphic.SetShapeOffsetImmediate(Vector2.zero);
    }

    public void SetMaskedVisible()
    {
        if (!HasMask)
            return;

        _graphic.enabled = true;
        _mask.enabled = true;

        _graphic.SetShapeOffsetImmediate(Vector2.zero);
    }

    public void SetMaskedHidden()
    {
        if (!HasMask)
            return;

        _graphic.enabled = true;
        _mask.enabled = true;

        _graphic.ResetToHiddenOffset();
    }

    public void SetMaskedVisible(StageMaskKind kind)
    {
        if (!HasMask)
            return;

        _graphic.Kind = kind;
        SetMaskedVisible();
    }

    public void SetMaskedHidden(StageMaskKind kind)
    {
        if (!HasMask)
            return;

        _graphic.Kind = kind;
        SetMaskedHidden();
    }

    public void SetMaskOffsetImmediate(Vector2 offset)
    {
        if (!HasMask)
            return;

        _graphic.enabled = true;
        _mask.enabled = true;

        _graphic.SetShapeOffsetImmediate(offset);
    }

    public void SetSlanted(
        float slantPixels,
        bool slantToRight,
        bool flipVertical)
    {
        if (_graphic == null)
            return;

        _graphic.SetSlanted(
            slantPixels,
            slantToRight,
            flipVertical);
    }

    public void SetHorizontalStrip(
        float heightPixels,
        float horizontalBleedPixels)
    {
        if (_graphic == null)
            return;

        _graphic.SetHorizontalStrip(
            heightPixels,
            horizontalBleedPixels);
    }

    private void CacheRefs()
    {
        if (_graphic == null)
            _graphic = GetComponent<StageMaskGraphic>();

        if (_mask == null)
            _mask = GetComponent<Mask>();
    }
}