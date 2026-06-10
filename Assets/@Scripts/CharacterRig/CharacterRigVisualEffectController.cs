using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Runtime material owner for character portrait Images.
// POCO (not MonoBehaviour): created by SetupCharRigCommand, held in CharacterRigRefs.VisualEffect,
// disposed by CharacterRigRegistry.DestroyRig.
// 주의: runtime material은 rig마다 Instantiate되므로 Dispose가 누락되면 teardown(롤백/seek 리빌드)마다 누수됩니다.
public sealed class CharacterRigVisualEffectController : IDisposable
{
    private const float DimBrightness = 0.62f;
    private const float DimSaturation = 0.70f;
    private static readonly Color DefaultDimTintColor = new(0.50f, 0.50f, 0.78f, 1f);

    private static readonly Color DefaultOuterRimColor = Color.white;
    private const float OuterRimWidth = 0.0025f;
    private const float OuterRimSoftness = 0.8f;

    private static readonly Color DefaultInnerRimColor = Color.white;
    private const float InnerRimWidth = 0.003f;
    private const float InnerRimSoftness = 0.8f;

    private const float BlurSize = 0.001f;

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

    private Material _runtimeMaterial;

    private float _dimAmount;
    private float _blurAmount;
    private float _outerRimAmount;
    private float _innerRimAmount;

    private Color _dimTintColor;
    private Color _outerRimColor;
    private Color _innerRimColor;

    public float DimAmount => _dimAmount;
    public float BlurAmount => _blurAmount;
    public float OuterRimAmount => _outerRimAmount;
    public float InnerRimAmount => _innerRimAmount;

    public Color DimTintColor => _dimTintColor;
    public Color OuterRimColor => _outerRimColor;
    public Color InnerRimColor => _innerRimColor;

    public CharacterRigVisualEffectController(
        Image portraitImage,
        Image portraitOverlayImage,
        Material sourceMaterial)
    {
        if (portraitImage == null && portraitOverlayImage == null)
        {
            Debug.LogWarning("[CharacterRigVisualEffectController] portraitImage and portraitOverlayImage are null.");
            return;
        }

        if (sourceMaterial == null)
        {
            Object context = portraitImage != null
                ? portraitImage
                : portraitOverlayImage;

            Debug.LogWarning(
                "[CharacterRigVisualEffectController] sourceMaterial is null. " +
                "Check the Resources path in SetupCharRigCommand.",
                context);
            return;
        }

        Image runtimeNameSource = portraitImage != null
            ? portraitImage
            : portraitOverlayImage;

        _runtimeMaterial = Object.Instantiate(sourceMaterial);
        _runtimeMaterial.name = $"{sourceMaterial.name}_Runtime_{runtimeNameSource.name}";

        if (portraitImage != null)
            portraitImage.material = _runtimeMaterial;

        if (portraitOverlayImage != null)
            portraitOverlayImage.material = _runtimeMaterial;

        _dimAmount = 0f;
        _blurAmount = 0f;
        _outerRimAmount = 0f;
        _innerRimAmount = 0f;

        _dimTintColor = DefaultDimTintColor;
        _outerRimColor = DefaultOuterRimColor;
        _innerRimColor = DefaultInnerRimColor;

        ApplyStaticStyle();
        ApplyDynamicValues();
    }

    public void ApplyImmediate(
        float dim,
        float outerRim,
        float innerRim,
        float blur,
        Color outerColor,
        Color innerColor)
    {
        ApplyImmediate(
            dim,
            _dimTintColor,
            outerRim,
            innerRim,
            blur,
            outerColor,
            innerColor);
    }

    public void ApplyImmediate(
        float dim,
        Color dimTintColor,
        float outerRim,
        float innerRim,
        float blur,
        Color outerColor,
        Color innerColor)
    {
        if (_runtimeMaterial == null)
            return;

        _dimAmount = Mathf.Clamp01(dim);
        _dimTintColor = dimTintColor;
        _outerRimAmount = Mathf.Clamp01(outerRim);
        _innerRimAmount = Mathf.Clamp01(innerRim);
        _blurAmount = Mathf.Clamp01(blur);
        _outerRimColor = outerColor;
        _innerRimColor = innerColor;

        ApplyDynamicValues();
    }

    public void ClearImmediate()
    {
        ApplyImmediate(
            0f,
            DefaultDimTintColor,
            0f,
            0f,
            0f,
            DefaultOuterRimColor,
            DefaultInnerRimColor);
    }

    // 정적 스타일은 균일·불변이므로 생성 시 한 번만 기록. tween 프레임마다 다시 쓰지 않는다.
    private void ApplyStaticStyle()
    {
        if (_runtimeMaterial == null)
            return;

        _runtimeMaterial.SetFloat(DimBrightnessId, DimBrightness);
        _runtimeMaterial.SetFloat(DimSaturationId, DimSaturation);

        _runtimeMaterial.SetFloat(RimWidthId, OuterRimWidth);
        _runtimeMaterial.SetFloat(RimSoftnessId, OuterRimSoftness);

        _runtimeMaterial.SetFloat(InnerRimWidthId, InnerRimWidth);
        _runtimeMaterial.SetFloat(InnerRimSoftnessId, InnerRimSoftness);

        _runtimeMaterial.SetFloat(BlurSizeId, BlurSize);
    }

    private void ApplyDynamicValues()
    {
        if (_runtimeMaterial == null)
            return;

        _runtimeMaterial.SetFloat(DimAmountId, _dimAmount);
        _runtimeMaterial.SetColor(DimTintColorId, _dimTintColor);

        _runtimeMaterial.SetFloat(RimAmountId, _outerRimAmount);
        _runtimeMaterial.SetColor(RimColorId, _outerRimColor);

        _runtimeMaterial.SetFloat(InnerRimAmountId, _innerRimAmount);
        _runtimeMaterial.SetColor(InnerRimColorId, _innerRimColor);

        _runtimeMaterial.SetFloat(BlurAmountId, _blurAmount);
    }

    public void Dispose()
    {
        // material 파괴 전에 이 컨트롤러를 target으로 도는 focus tween을 먼저 정리.
        // (tween target이 POCO이므로 KillTweenOnHierarchy로는 잡히지 않는다.)
        DOTween.Kill(this, false);

        if (_runtimeMaterial != null)
        {
            Object.Destroy(_runtimeMaterial);
            _runtimeMaterial = null;
        }
    }
}