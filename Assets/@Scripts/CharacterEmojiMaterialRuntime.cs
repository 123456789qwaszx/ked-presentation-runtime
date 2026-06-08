using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterEmojiMaterialRuntime
{
    private readonly Image _image;

    private Material _runtimeMaterial;
    private Tween _revealTween;

    public Material RuntimeMaterial => _runtimeMaterial;

    public CharacterEmojiMaterialRuntime(Image image)
    {
        _image = image;
    }

    public bool HasAliveSource()
    {
        return _image != null;
    }

    public bool EnsureMaterial(Material baseMaterial)
    {
        if (!HasAliveSource())
        {
            ClearRuntimeState();
            return false;
        }

        if (baseMaterial == null)
        {
            ClearRuntimeState();
            return false;
        }

        if (_runtimeMaterial != null &&
            _runtimeMaterial.shader == baseMaterial.shader)
        {
            _image.material = _runtimeMaterial;
            return true;
        }

        DestroyRuntimeMaterial();

        _runtimeMaterial = Object.Instantiate(baseMaterial);
        _runtimeMaterial.name = baseMaterial.name + " (Runtime Emoji)";

        _image.material = _runtimeMaterial;
        return true;
    }

    public void ApplyPresetStatic(CharacterEmojiVisualPresetSO preset, float reveal)
    {
        if (!HasAliveSource())
        {
            ClearRuntimeState();
            return;
        }

        if (_runtimeMaterial == null || preset == null)
            return;

        _runtimeMaterial.SetFloat(CharacterEmojiShaderIds.Reveal, reveal);
        _runtimeMaterial.SetFloat(CharacterEmojiShaderIds.RevealSoftness, preset.revealSoftness);
        _runtimeMaterial.SetFloat(CharacterEmojiShaderIds.RevealDirection, GetDirectionValue(preset));

        _runtimeMaterial.SetFloat(CharacterEmojiShaderIds.EdgeRimAmount, preset.edgeRimAmount);
        _runtimeMaterial.SetFloat(CharacterEmojiShaderIds.EdgeRimWidth, preset.edgeRimWidth);
        _runtimeMaterial.SetColor(CharacterEmojiShaderIds.EdgeRimColor, preset.edgeRimColor);

        _runtimeMaterial.SetFloat(CharacterEmojiShaderIds.GlowAmount, preset.glowAmount);
        _runtimeMaterial.SetColor(CharacterEmojiShaderIds.GlowColor, preset.glowColor);
    }

    public void SetReveal(float reveal)
    {
        if (!HasAliveSource())
        {
            ClearRuntimeState();
            return;
        }

        if (_runtimeMaterial == null)
            return;

        _runtimeMaterial.SetFloat(CharacterEmojiShaderIds.Reveal, reveal);
    }

    public Tween TweenReveal(
        float from,
        float to,
        float duration,
        Ease ease,
        bool useUnscaledTime)
    {
        KillTween(false);

        if (!HasAliveSource())
        {
            ClearRuntimeState();
            return null;
        }

        if (_runtimeMaterial == null)
            return null;

        if (duration <= 0f)
        {
            SetReveal(to);
            return null;
        }

        _runtimeMaterial.SetFloat(CharacterEmojiShaderIds.Reveal, from);

        _revealTween = DOTween
            .To(
                () => _runtimeMaterial != null
                    ? _runtimeMaterial.GetFloat(CharacterEmojiShaderIds.Reveal)
                    : to,
                value =>
                {
                    if (_runtimeMaterial != null)
                        _runtimeMaterial.SetFloat(CharacterEmojiShaderIds.Reveal, value);
                },
                to,
                duration)
            .SetEase(ease)
            .SetUpdate(useUnscaledTime)
            .SetTarget(this)
            .OnComplete(() =>
            {
                SetReveal(to);
            })
            .OnKill(() =>
            {
                _revealTween = null;
            });

        return _revealTween;
    }

    public Tween TweenReveal(CharacterEmojiVisualPresetSO preset, bool useUnscaledTime)
    {
        if (preset == null)
            return null;

        return TweenReveal(
            preset.startReveal,
            preset.endReveal,
            preset.revealDuration,
            preset.revealEase,
            useUnscaledTime);
    }

    public void CompleteReveal(CharacterEmojiVisualPresetSO preset)
    {
        KillTween(false);

        if (preset == null)
            return;

        SetReveal(preset.endReveal);
    }

    public void ResetReveal(CharacterEmojiVisualPresetSO preset)
    {
        KillTween(false);

        if (preset == null)
            return;

        SetReveal(preset.startReveal);
    }

    public void KillTween(bool complete)
    {
        if (_revealTween == null)
            return;

        _revealTween.Kill(complete);
        _revealTween = null;
    }

    public void ClearRuntimeState()
    {
        KillTween(false);

        if (_runtimeMaterial != null)
        {
            Object.Destroy(_runtimeMaterial);
            _runtimeMaterial = null;
        }
    }

    public void DestroyRuntimeMaterial()
    {
        KillTween(false);

        if (_image != null && _image.material == _runtimeMaterial)
            _image.material = null;

        if (_runtimeMaterial != null)
        {
            Object.Destroy(_runtimeMaterial);
            _runtimeMaterial = null;
        }
    }

    private static float GetDirectionValue(CharacterEmojiVisualPresetSO preset)
    {
        return preset.revealDirection == CharacterEmojiRevealDirection.BottomToTop
            ? 1f
            : 0f;
    }
}