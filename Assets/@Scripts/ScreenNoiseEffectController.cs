using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class ScreenNoiseEffectController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Image targetImage;

    [Header("Material")]
    [SerializeField] private Material sourceMaterial;

    [Header("Default")]
    [SerializeField, Range(0f, 1f)] private float defaultAmount = 0f;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField, Min(0f)] private float defaultScale = 0.8f;
    [SerializeField] private float defaultSpeedX = 0.015f;
    [SerializeField] private float defaultSpeedY = 0.012f;
    [SerializeField, Min(0f)] private float defaultContrast = 1f;

    private Material _runtimeMaterial;

    private float _amount;
    private Color _color;
    private float _scale;
    private float _speedX;
    private float _speedY;
    private float _contrast;

    private static readonly int NoiseAmountId = Shader.PropertyToID("_NoiseAmount");
    private static readonly int NoiseColorId = Shader.PropertyToID("_NoiseColor");
    private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
    private static readonly int NoiseSpeedXId = Shader.PropertyToID("_NoiseSpeedX");
    private static readonly int NoiseSpeedYId = Shader.PropertyToID("_NoiseSpeedY");
    private static readonly int NoiseContrastId = Shader.PropertyToID("_NoiseContrast");

    public float Amount => _amount;
    public Color Color => _color;
    public float Scale => _scale;
    public float SpeedX => _speedX;
    public float SpeedY => _speedY;
    public float Contrast => _contrast;

    private void Reset()
    {
        EnsureTarget();
    }

    private void Awake()
    {
        EnsureTarget();
        EnsureRuntimeMaterial();

        _amount = Mathf.Clamp01(defaultAmount);
        _color = defaultColor;
        _scale = Mathf.Max(0f, defaultScale);
        _speedX = defaultSpeedX;
        _speedY = defaultSpeedY;
        _contrast = Mathf.Max(0f, defaultContrast);

        ApplyMaterialValues();

        if (targetImage != null)
            targetImage.raycastTarget = false;
    }

    private void OnDestroy()
    {
        KillTween(false);

        if (_runtimeMaterial != null)
        {
            Destroy(_runtimeMaterial);
            _runtimeMaterial = null;
        }
    }

    public void ApplyImmediate(
        float amount,
        Color color,
        float scale,
        float speedX,
        float speedY,
        float contrast)
    {
        EnsureRuntimeMaterial();

        if (_runtimeMaterial == null)
            return;

        _amount = Mathf.Clamp01(amount);
        _color = color;
        _scale = Mathf.Max(0f, scale);
        _speedX = speedX;
        _speedY = speedY;
        _contrast = Mathf.Max(0f, contrast);

        ApplyMaterialValues();
    }

    public void ClearImmediate()
    {
        ApplyImmediate(
            0f,
            _color,
            _scale,
            _speedX,
            _speedY,
            _contrast);
    }

    public void KillTween(bool complete)
    {
        transform.DOKill(complete);
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
                "[ScreenNoiseEffectController] targetImage is missing.",
                this);
            return;
        }

        Material baseMaterial = sourceMaterial != null
            ? sourceMaterial
            : targetImage.material;

        if (baseMaterial == null)
        {
            Debug.LogWarning(
                "[ScreenNoiseEffectController] source material is missing. " +
                "Assign M_UIScreenNoise to the Image material or sourceMaterial.",
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

        _runtimeMaterial.SetFloat(NoiseAmountId, _amount);
        _runtimeMaterial.SetColor(NoiseColorId, _color);
        _runtimeMaterial.SetFloat(NoiseScaleId, _scale);
        _runtimeMaterial.SetFloat(NoiseSpeedXId, _speedX);
        _runtimeMaterial.SetFloat(NoiseSpeedYId, _speedY);
        _runtimeMaterial.SetFloat(NoiseContrastId, _contrast);
    }
}