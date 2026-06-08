using DG.Tweening;
using UnityEngine;

public enum CharacterEmojiRevealDirection
{
    TopToBottom = 0,
    BottomToTop = 1
}

[CreateAssetMenu(
    menuName = "CPS/CharRig/Emoji Visual Preset",
    fileName = "CharacterEmojiVisualPreset")]
public sealed class CharacterEmojiVisualPresetSO : ScriptableObject
{
    [Header("Material")]
    public Material baseMaterial;

    [Header("Reveal")]
    [Range(0f, 1f)] public float startReveal = 0f;
    [Range(0f, 1f)] public float endReveal = 1f;

    [Min(0f)] public float revealDuration = 0.12f;
    public Ease revealEase = Ease.OutCubic;

    public CharacterEmojiRevealDirection revealDirection =
        CharacterEmojiRevealDirection.TopToBottom;

    [Range(0.001f, 0.3f)]
    public float revealSoftness = 0.08f;

    [Header("Reveal Edge Rim")]
    [Range(0f, 2f)] public float edgeRimAmount = 0.6f;

    [Range(0.001f, 0.3f)]
    public float edgeRimWidth = 0.06f;

    public Color edgeRimColor = Color.white;

    [Header("Glow")]
    [Range(0f, 2f)] public float glowAmount = 0.15f;
    public Color glowColor = Color.white;
}