using System;
using UnityEngine;

[CreateAssetMenu(menuName = "CPS/CharRig/Emoji Preset", fileName = "EmojiPreset_")]
public sealed class CharacterEmojiPresetSO : ScriptableObject
{
    public string emojiKey = "sparkle";

    [Header("Sprite")]
    public Sprite sprite;

    [Header("Layout (applied to CharacterEmoji_Anchor or Pad/Scale)")]
    public EmojiLayout layout = EmojiLayout.Default;

    [Header("Fade")]
    public float fadeIn = 0.12f;
    public float fadeOut = 0.12f;

    [Header("Persistent Effect")]
    public EmojiPersistentEffect effect = EmojiPersistentEffect.None;

    public EffectParams effectParams = EffectParams.Default;

    [Serializable]
    public struct EmojiLayout
    {
        public Vector2 anchoredPos;
        public Vector2 scale;     // localScale x/y
        public float rotationZ;
        public Vector2 anchorMin; // (선택) Anchor를 쓸 거면
        public Vector2 anchorMax;

        public static EmojiLayout Default => new()
        {
            anchoredPos = Vector2.zero,
            scale = Vector2.one,
            rotationZ = 0f,
            anchorMin = new Vector2(0.5f, 0.5f),
            anchorMax = new Vector2(0.5f, 0.5f),
        };
    }

    public enum EmojiPersistentEffect
    {
        None = 0,
        Sway,      // 천천히 좌우 흔들림
        Bob,       // 위아래 바운스
        Shake,     // 작은 흔들림(지속)
        Pulse,     // 스케일 펄스
    }

    [Serializable]
    public struct EffectParams
    {
        public float amp;
        public float freq;
        public float duration; // 0이면 무한

        public static EffectParams Default => new() { amp = 1f, freq = 1f, duration = 0f };
    }
}