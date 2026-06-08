using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class ScreenFlashEffectController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Image targetImage;

    [Header("Material")]
    [SerializeField] private Material sourceMaterial;

    [Header("Default")]
    [SerializeField] private Color defaultFlashColor = Color.white;

    private UiEffectMaterialBinding _material;

    private float _flashAmount;
    private Color _flashColor;

    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

    public float FlashAmount => _flashAmount;
    public Color FlashColor => _flashColor;

    private void Reset() => EnsureTarget();

    private void Awake()
    {
        EnsureTarget();
        EnsureMaterial();

        _flashAmount = 0f;
        _flashColor = defaultFlashColor;

        ApplyMaterialValues();

        if (targetImage != null)
            targetImage.raycastTarget = false;
    }

    private void OnDestroy()
    {
        KillTween(false);
        _material?.Dispose();
        _material = null;
    }

    public void ApplyImmediate(float amount, Color color)
    {
        EnsureMaterial();

        if (_material == null || !_material.IsValid)
            return;

        _flashAmount = Mathf.Clamp01(amount);
        _flashColor = color;

        ApplyMaterialValues();
    }

    public void ClearImmediate() => ApplyImmediate(0f, _flashColor);

    public void KillTween(bool complete) => transform.DOKill(complete);

    private void EnsureTarget()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    private void EnsureMaterial()
    {
        EnsureTarget();

        if (_material != null)
            return;

        if (targetImage == null)
        {
            Debug.LogWarning("[ScreenFlashEffectController] targetImage is missing.", this);
            return;
        }

        _material = new UiEffectMaterialBinding(targetImage, sourceMaterial, gameObject.name);
    }

    private void ApplyMaterialValues()
    {
        if (_material == null)
            return;

        _material.SetFloat(FlashAmountId, _flashAmount);
        _material.SetColor(FlashColorId, _flashColor);
    }
}