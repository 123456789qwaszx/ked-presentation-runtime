using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterRigVisualEffectController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Image portraitImage;

    [Header("Material")]
    [SerializeField] private Material sourceMaterial;

    [Header("Default Values")]
    [SerializeField] private Color rimColor = Color.white;

    [Header("Focus Preset")]
    [SerializeField] private float focusRimAmount = 0.35f;

    [Header("Defocus Preset")]
    [SerializeField] private float defocusDimAmount = 0.28f;
    [SerializeField] private float defocusBlurAmount = 0.25f;

    private Material _runtimeMaterial;

    private float _dimAmount;
    private float _rimAmount;
    private float _blurAmount;
    private Color _rimColor;

    private static readonly int DimAmountId = Shader.PropertyToID("_DimAmount");
    private static readonly int RimAmountId = Shader.PropertyToID("_RimAmount");
    private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
    private static readonly int BlurAmountId = Shader.PropertyToID("_BlurAmount");
    private static readonly int TexelSizeId = Shader.PropertyToID("_TexelSize");

    public float DimAmount => _dimAmount;
    public float RimAmount => _rimAmount;
    public float BlurAmount => _blurAmount;
    public Color RimColor => _rimColor;

    public float FocusRimAmount => focusRimAmount;
    public float DefocusDimAmount => defocusDimAmount;
    public float DefocusBlurAmount => defocusBlurAmount;

    private void Awake()
    {
        EnsureTarget();
        EnsureRuntimeMaterial();

        _dimAmount = 0f;
        _rimAmount = 0f;
        _blurAmount = 0f;
        _rimColor = rimColor;

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

    public void ApplyImmediate(float dim, float rim, float blur, Color color)
    {
        EnsureRuntimeMaterial();

        if (_runtimeMaterial == null)
            return;

        _dimAmount = dim;
        _rimAmount = rim;
        _blurAmount = blur;
        _rimColor = color;

        ApplyMaterialValues();
    }

    public void ApplyFocusImmediate(float intensity)
    {
        ApplyImmediate(
            0f,
            focusRimAmount * intensity,
            0f,
            rimColor);
    }

    public void ApplyDefocusImmediate(float intensity)
    {
        ApplyImmediate(
            defocusDimAmount * intensity,
            0f,
            defocusBlurAmount * intensity,
            rimColor);
    }

    public void ClearImmediate()
    {
        ApplyImmediate(0f, 0f, 0f, rimColor);
    }

    public void RefreshTexelSize()
    {
        ApplyTexelSize();
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
        _runtimeMaterial.SetFloat(RimAmountId, _rimAmount);
        _runtimeMaterial.SetFloat(BlurAmountId, _blurAmount);
        _runtimeMaterial.SetColor(RimColorId, _rimColor);

        ApplyTexelSize();
    }

    private void ApplyTexelSize()
    {
        if (_runtimeMaterial == null || portraitImage == null)
            return;

        Sprite sprite = portraitImage.sprite;
        if (sprite == null || sprite.texture == null)
            return;

        Texture texture = sprite.texture;

        _runtimeMaterial.SetVector(
            TexelSizeId,
            new Vector4(
                1f / texture.width,
                1f / texture.height,
                0f,
                0f));
    }
}