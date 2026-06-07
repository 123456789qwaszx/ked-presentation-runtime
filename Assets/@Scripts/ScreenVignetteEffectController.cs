using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class ScreenVignetteEffectController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Image targetImage;

    [Header("Material")]
    [SerializeField] private Material sourceMaterial;

    [Header("Default")]
    [SerializeField] private Color defaultColor = Color.black;
    [SerializeField, Range(0f, 1f)] private float defaultAmount = 0f;
    [SerializeField, Range(0f, 1f)] private float defaultRadius = 0.45f;
    [SerializeField, Range(0.001f, 1f)] private float defaultSoftness = 0.35f;
    [SerializeField, Min(0f)] private float defaultAspect = 1.777f;

    private Material _runtimeMaterial;

    private float _amount;
    private Color _color;
    private float _radius;
    private float _softness;
    private float _aspect;

    private static readonly int VignetteAmountId = Shader.PropertyToID("_VignetteAmount");
    private static readonly int VignetteColorId = Shader.PropertyToID("_VignetteColor");
    private static readonly int VignetteRadiusId = Shader.PropertyToID("_VignetteRadius");
    private static readonly int VignetteSoftnessId = Shader.PropertyToID("_VignetteSoftness");
    private static readonly int AspectId = Shader.PropertyToID("_Aspect");

    public float Amount => _amount;
    public Color Color => _color;
    public float Radius => _radius;
    public float Softness => _softness;
    public float Aspect => _aspect;

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
        _radius = Mathf.Clamp01(defaultRadius);
        _softness = Mathf.Max(0.001f, defaultSoftness);
        _aspect = Mathf.Max(0f, defaultAspect);

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
        float radius,
        float softness,
        float aspect)
    {
        EnsureRuntimeMaterial();

        if (_runtimeMaterial == null)
            return;

        _amount = Mathf.Clamp01(amount);
        _color = color;
        _radius = Mathf.Clamp01(radius);
        _softness = Mathf.Max(0.001f, softness);
        _aspect = Mathf.Max(0f, aspect);

        ApplyMaterialValues();
    }

    public void ClearImmediate()
    {
        ApplyImmediate(
            0f,
            _color,
            _radius,
            _softness,
            _aspect);
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
                "[ScreenVignetteEffectController] targetImage is missing.",
                this);
            return;
        }

        Material baseMaterial = sourceMaterial != null
            ? sourceMaterial
            : targetImage.material;

        if (baseMaterial == null)
        {
            Debug.LogWarning(
                "[ScreenVignetteEffectController] source material is missing. " +
                "Assign M_UIScreenVignette to the Image material or sourceMaterial.",
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

        _runtimeMaterial.SetFloat(VignetteAmountId, _amount);
        _runtimeMaterial.SetColor(VignetteColorId, _color);
        _runtimeMaterial.SetFloat(VignetteRadiusId, _radius);
        _runtimeMaterial.SetFloat(VignetteSoftnessId, _softness);
        _runtimeMaterial.SetFloat(AspectId, _aspect);
    }
}