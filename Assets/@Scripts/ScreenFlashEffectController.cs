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

    private Material _runtimeMaterial;

    private float _flashAmount;
    private Color _flashColor;

    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

    public float FlashAmount => _flashAmount;
    public Color FlashColor => _flashColor;

    private void Reset()
    {
        EnsureTarget();
    }

    private void Awake()
    {
        EnsureTarget();
        EnsureRuntimeMaterial();

        _flashAmount = 0f;
        _flashColor = defaultFlashColor;

        ApplyMaterialValues();

        if (targetImage != null)
            targetImage.raycastTarget = false;
    }

    private void OnDestroy()
    {
        KillTween();

        if (_runtimeMaterial != null)
        {
            Destroy(_runtimeMaterial);
            _runtimeMaterial = null;
        }
    }

    public void ApplyImmediate(float amount, Color color)
    {
        EnsureRuntimeMaterial();

        if (_runtimeMaterial == null)
            return;

        _flashAmount = Mathf.Clamp01(amount);
        _flashColor = color;

        ApplyMaterialValues();
    }

    public void ClearImmediate()
    {
        ApplyImmediate(0f, _flashColor);
    }

    public void KillTween(bool complete)
    {
        transform.DOKill(complete);
    }

    private void KillTween()
    {
        transform.DOKill(false);
    }

    private void EnsureTarget()
    {
        if (targetImage != null)
            return;

        targetImage = GetComponent<Image>();
    }

    private void EnsureRuntimeMaterial()
    {
        EnsureTarget();

        if (_runtimeMaterial != null)
            return;

        if (targetImage == null)
        {
            Debug.LogWarning(
                "[ScreenFlashEffectController] targetImage is missing.",
                this);
            return;
        }

        Material baseMaterial = sourceMaterial != null
            ? sourceMaterial
            : targetImage.material;

        if (baseMaterial == null)
        {
            Debug.LogWarning(
                "[ScreenFlashEffectController] source material is missing. " +
                "Assign M_UIScreenFlash to the Image material or sourceMaterial.",
                this);
            return;
        }

        _runtimeMaterial = Instantiate(baseMaterial);
        _runtimeMaterial.name = $"{baseMaterial.name}_Runtime_{gameObject.name}";

        targetImage.material = _runtimeMaterial;
    }

    private void ApplyMaterialValues()
    {
        if (_runtimeMaterial == null)
            return;

        _runtimeMaterial.SetFloat(FlashAmountId, _flashAmount);
        _runtimeMaterial.SetColor(FlashColorId, _flashColor);
    }
}