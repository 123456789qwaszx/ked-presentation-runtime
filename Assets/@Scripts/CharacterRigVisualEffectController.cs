using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterRigVisualEffectController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Image portraitImage;

    [Header("Material")]
    [SerializeField] private Material sourceMaterial;

    [Header("Dim Style")]
    [SerializeField, Range(0f, 1f)] private float dimBrightness = 0.62f;
    [SerializeField, Range(0f, 1f)] private float dimSaturation = 0.70f;
    [SerializeField] private Color dimTintColor = new Color(0.45f, 0.48f, 0.55f, 1f);

    [Header("Outer Rim Style")]
    [SerializeField] private Color outerRimColor = Color.white;
    [SerializeField, Min(0f)] private float outerRimWidth = 0.006f;
    [SerializeField, Min(0.0001f)] private float outerRimSoftness = 0.02f;

    [Header("Inner Rim Style")]
    [SerializeField] private Color innerRimColor = new Color(1f, 0.96f, 0.86f, 1f);
    [SerializeField, Min(0f)] private float innerRimWidth = 0.004f;
    [SerializeField, Min(0.0001f)] private float innerRimSoftness = 0.02f;

    [Header("Blur Style")]
    [SerializeField, Min(0f)] private float blurSize = 0.004f;

    [Header("Focus Preset")]
    [SerializeField, Range(0f, 1f)] private float focusInnerRimAmount = 0.25f;
    [SerializeField, Range(0f, 1f)] private float focusOuterRimAmount = 0f;

    [Header("Defocus Preset")]
    [SerializeField, Range(0f, 1f)] private float defocusDimAmount = 0.45f;
    [SerializeField, Range(0f, 1f)] private float defocusBlurAmount = 0.25f;

    private Material _runtimeMaterial;

    private float _dimAmount;
    private float _blurAmount;
    private float _outerRimAmount;
    private float _innerRimAmount;
    private Color _outerRimColor;
    private Color _innerRimColor;

    private static readonly int DimAmountId = Shader.PropertyToID("_DimAmount");
    private static readonly int DimBrightnessId = Shader.PropertyToID("_DimBrightness");
    private static readonly int DimSaturationId = Shader.PropertyToID("_DimSaturation");
    private static readonly int DimTintColorId = Shader.PropertyToID("_DimTintColor");

    private static readonly int RimAmountId = Shader.PropertyToID("_RimAmount");
    private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
    private static readonly int RimWidthId = Shader.PropertyToID("_RimWidth");
    private static readonly int RimSoftnessId = Shader.PropertyToID("_RimSoftness");

    private static readonly int InnerRimAmountId = Shader.PropertyToID("_InnerRimAmount");
    private static readonly int InnerRimColorId = Shader.PropertyToID("_InnerRimColor");
    private static readonly int InnerRimWidthId = Shader.PropertyToID("_InnerRimWidth");
    private static readonly int InnerRimSoftnessId = Shader.PropertyToID("_InnerRimSoftness");

    private static readonly int BlurAmountId = Shader.PropertyToID("_BlurAmount");
    private static readonly int BlurSizeId = Shader.PropertyToID("_BlurSize");

    public float DimAmount => _dimAmount;
    public float BlurAmount => _blurAmount;
    public float OuterRimAmount => _outerRimAmount;
    public float InnerRimAmount => _innerRimAmount;

    // Legacy compatibility.
    public float RimAmount => _outerRimAmount;
    public Color RimColor => _outerRimColor;

    public Color OuterRimColor => _outerRimColor;
    public Color InnerRimColor => _innerRimColor;

    public float FocusInnerRimAmount => focusInnerRimAmount;
    public float FocusOuterRimAmount => focusOuterRimAmount;
    public float DefocusDimAmount => defocusDimAmount;
    public float DefocusBlurAmount => defocusBlurAmount;

    private void Awake()
    {
        EnsureTarget();
        EnsureRuntimeMaterial();

        _dimAmount = 0f;
        _blurAmount = 0f;
        _outerRimAmount = 0f;
        _innerRimAmount = 0f;
        _outerRimColor = outerRimColor;
        _innerRimColor = innerRimColor;

        ApplyMaterialValues();
    }

    private void Reset()
    {
        EnsureTarget();
    }

    private void OnDestroy()
    {
        if (_runtimeMaterial != null)
        {
            Destroy(_runtimeMaterial);
            _runtimeMaterial = null;
        }
    }

    public void ApplyImmediate(
        float dim,
        float outerRim,
        float innerRim,
        float blur,
        Color outerColor,
        Color innerColor)
    {
        EnsureRuntimeMaterial();

        if (_runtimeMaterial == null)
            return;

        _dimAmount = Mathf.Clamp01(dim);
        _outerRimAmount = Mathf.Clamp01(outerRim);
        _innerRimAmount = Mathf.Clamp01(innerRim);
        _blurAmount = Mathf.Clamp01(blur);
        _outerRimColor = outerColor;
        _innerRimColor = innerColor;

        ApplyMaterialValues();
    }

    // Legacy compatibility. Old "rim" maps to outer rim.
    public void ApplyImmediate(float dim, float rim, float blur, Color color)
    {
        ApplyImmediate(
            dim,
            rim,
            0f,
            blur,
            color,
            innerRimColor);
    }

    public void ApplyFocusImmediate(float intensity)
    {
        intensity = Mathf.Clamp01(intensity);

        ApplyImmediate(
            0f,
            focusOuterRimAmount * intensity,
            focusInnerRimAmount * intensity,
            0f,
            outerRimColor,
            innerRimColor);
    }

    public void ApplyDefocusImmediate(float intensity)
    {
        intensity = Mathf.Clamp01(intensity);

        ApplyImmediate(
            defocusDimAmount * intensity,
            0f,
            0f,
            defocusBlurAmount * intensity,
            outerRimColor,
            innerRimColor);
    }

    public void ClearImmediate()
    {
        ApplyImmediate(
            0f,
            0f,
            0f,
            0f,
            outerRimColor,
            innerRimColor);
    }

    public void RefreshStyleValues()
    {
        ApplyMaterialValues();
    }

    private void EnsureTarget()
    {
        if (portraitImage != null)
            return;

        portraitImage = GetComponent<Image>();
    }

    private void EnsureRuntimeMaterial()
    {
        EnsureTarget();

        if (_runtimeMaterial != null)
            return;

        if (portraitImage == null)
        {
            Debug.LogWarning(
                "[CharacterRigVisualEffectController] portraitImage is missing.",
                this);
            return;
        }

        if (sourceMaterial == null)
        {
            Debug.LogWarning(
                "[CharacterRigVisualEffectController] sourceMaterial is missing.",
                this);
            return;
        }

        _runtimeMaterial = Instantiate(sourceMaterial);
        _runtimeMaterial.name = $"{sourceMaterial.name}_Runtime_{gameObject.name}";

        portraitImage.material = _runtimeMaterial;
    }

    private void ApplyMaterialValues()
    {
        if (_runtimeMaterial == null)
            return;

        _runtimeMaterial.SetFloat(DimAmountId, _dimAmount);
        _runtimeMaterial.SetFloat(DimBrightnessId, Mathf.Clamp01(dimBrightness));
        _runtimeMaterial.SetFloat(DimSaturationId, Mathf.Clamp01(dimSaturation));
        _runtimeMaterial.SetColor(DimTintColorId, dimTintColor);

        _runtimeMaterial.SetFloat(RimAmountId, _outerRimAmount);
        _runtimeMaterial.SetColor(RimColorId, _outerRimColor);
        _runtimeMaterial.SetFloat(RimWidthId, Mathf.Max(0f, outerRimWidth));
        _runtimeMaterial.SetFloat(RimSoftnessId, Mathf.Max(0.0001f, outerRimSoftness));

        _runtimeMaterial.SetFloat(InnerRimAmountId, _innerRimAmount);
        _runtimeMaterial.SetColor(InnerRimColorId, _innerRimColor);
        _runtimeMaterial.SetFloat(InnerRimWidthId, Mathf.Max(0f, innerRimWidth));
        _runtimeMaterial.SetFloat(InnerRimSoftnessId, Mathf.Max(0.0001f, innerRimSoftness));

        _runtimeMaterial.SetFloat(BlurAmountId, _blurAmount);
        _runtimeMaterial.SetFloat(BlurSizeId, Mathf.Max(0f, blurSize));
    }
}