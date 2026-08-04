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

    private const float DefaultStageBlurEdgeHide = 0f;
    private const float DefaultStageBlurEdgeWidth = 2.88f;
    private const float DefaultStageBlurEdgeSoftness = 0.88f;

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

    private static readonly int StageBlurEdgeHideId = Shader.PropertyToID("_StageBlurEdgeHide");
    private static readonly int StageBlurEdgeWidthId = Shader.PropertyToID("_StageBlurEdgeWidth");
    private static readonly int StageBlurEdgeSoftnessId = Shader.PropertyToID("_StageBlurEdgeSoftness");
    
    private readonly Image _portraitImage;
    private readonly Image _portraitOverlayImage;
    
    private Material _runtimeMaterial;
    
    private float _dimAmount;
    private float _outerRimAmount;
    private float _innerRimAmount;
    private float _stageBlurEdgeHide;

    private Color _dimTintColor;
    private Color _outerRimColor;
    private Color _innerRimColor;

    public float DimAmount => _dimAmount;
    public float OuterRimAmount => _outerRimAmount;
    public float InnerRimAmount => _innerRimAmount;
    public float StageBlurEdgeHide => _stageBlurEdgeHide;

    public Color DimTintColor => _dimTintColor;
    public Color OuterRimColor => _outerRimColor;
    public Color InnerRimColor => _innerRimColor;

    public CharacterRigVisualEffectController(
        Image portraitImage,
        Image portraitOverlayImage,
        Material sourceMaterial)
    {
        _portraitImage = portraitImage;
        _portraitOverlayImage = portraitOverlayImage;

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
        _outerRimAmount = 0f;
        _innerRimAmount = 0f;
        _stageBlurEdgeHide = DefaultStageBlurEdgeHide;

        _dimTintColor = DefaultDimTintColor;
        _outerRimColor = DefaultOuterRimColor;
        _innerRimColor = DefaultInnerRimColor;

        ApplyStaticStyle();
        ApplyDynamicValues();
        ApplyStageBlurEdgeValues();

        MarkMaterialDirty();
    }

    public void ApplyImmediate(
        float dim,
        Color dimTintColor,
        float outerRim,
        float innerRim,
        Color outerColor,
        Color innerColor)
    {
        if (_runtimeMaterial == null)
            return;

        _dimAmount = Mathf.Clamp01(dim);
        _dimTintColor = dimTintColor;
        _outerRimAmount = Mathf.Clamp01(outerRim);
        _innerRimAmount = Mathf.Clamp01(innerRim);
        _outerRimColor = outerColor;
        _innerRimColor = innerColor;

        ApplyDynamicValues();
        MarkMaterialDirty();
    }

    public void SetStageBlurEdgeHideImmediate(float value)
    {
        if (_runtimeMaterial == null)
            return;

        _stageBlurEdgeHide = Mathf.Clamp01(value);
        ApplyStageBlurEdgeValues();
        MarkMaterialDirty();
    }

    // 스타일은 생성 시 한 번만 기록. tween마다 반복하지 않음.
    private void ApplyStaticStyle()
    {
        if (_runtimeMaterial == null)
            return;

        ApplyStaticStyleTo(_runtimeMaterial);
        PushMaterialToGraphics();
    }

    private void ApplyDynamicValues()
    {
        if (_runtimeMaterial == null)
            return;

        ApplyDynamicValuesTo(_runtimeMaterial);
        PushMaterialToGraphics();
    }

    private void ApplyStageBlurEdgeValues()
    {
        if (_runtimeMaterial == null)
            return;

        ApplyStageBlurEdgeValuesTo(_runtimeMaterial);
        PushMaterialToGraphics();
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
    
    private void MarkMaterialDirty()
    {
        if (_portraitImage != null)
            _portraitImage.SetMaterialDirty();

        if (_portraitOverlayImage != null)
            _portraitOverlayImage.SetMaterialDirty();
    }
    
    private void ApplyDynamicValuesTo(Material material)
    {
        material.SetFloat(DimAmountId, _dimAmount);
        material.SetColor(DimTintColorId, _dimTintColor);

        material.SetFloat(RimAmountId, _outerRimAmount);
        material.SetColor(RimColorId, _outerRimColor);

        material.SetFloat(InnerRimAmountId, _innerRimAmount);
        material.SetColor(InnerRimColorId, _innerRimColor);
    }

    private void ApplyStaticStyleTo(Material material)
    {
        material.SetFloat(DimBrightnessId, DimBrightness);
        material.SetFloat(DimSaturationId, DimSaturation);

        material.SetFloat(RimWidthId, OuterRimWidth);
        material.SetFloat(RimSoftnessId, OuterRimSoftness);

        material.SetFloat(InnerRimWidthId, InnerRimWidth);
        material.SetFloat(InnerRimSoftnessId, InnerRimSoftness);

        material.SetFloat(StageBlurEdgeWidthId, DefaultStageBlurEdgeWidth);
        material.SetFloat(StageBlurEdgeSoftnessId, DefaultStageBlurEdgeSoftness);
    }

    private void ApplyStageBlurEdgeValuesTo(Material material)
    {
        material.SetFloat(StageBlurEdgeHideId, _stageBlurEdgeHide);
    }
    
    // Mask 등에 의해 Canvas가 서브 Material을 생성했을 때, 실제 사용하는 것과 Image의 Material을 같게 함.
    private void PushMaterialToGraphics()
    {
        PushMaterialToGraphic(_portraitImage);
        PushMaterialToGraphic(_portraitOverlayImage);
    }

    private void PushMaterialToGraphic(Image image)
    {
        if (image == null)
            return;

        image.SetMaterialDirty();

        Material renderingMaterial = image.materialForRendering;
        if (renderingMaterial == null)
            return;

        ApplyStaticStyleTo(renderingMaterial);
        ApplyDynamicValuesTo(renderingMaterial);
        ApplyStageBlurEdgeValuesTo(renderingMaterial);

        image.canvasRenderer.SetMaterial(renderingMaterial, image.mainTexture);
    }
}