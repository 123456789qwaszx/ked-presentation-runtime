using System;
using UnityEngine;

[Serializable]
public struct CharacterEmojiLayout
{
    [Header("Cast Transform")]
    public Vector2 anchoredPosition;
    public Vector3 localScale;
    public float rotationZ;

    [Header("Image")]
    public bool preserveAspect;
    public bool setNativeSize;

    public static CharacterEmojiLayout Default => new()
    {
        anchoredPosition = Vector2.zero,
        localScale = Vector3.one,
        rotationZ = 0f,
        preserveAspect = true,
        setNativeSize = false,
    };
    
    public static CharacterEmojiLayout HeadRight => new()
    {
        anchoredPosition = new Vector2(130f, 550f),
        localScale = new Vector3(0.15f, 0.15f, 0.15f),
        rotationZ = 0f,
        preserveAspect = true,
        setNativeSize = false,
    };

    public static CharacterEmojiLayout HeadLeft => new()
    {
        anchoredPosition = new Vector2(-130f, 550f),
        localScale = new Vector3(0.15f, 0.15f, 0.15f),
        rotationZ = 0f,
        preserveAspect = true,
        setNativeSize = false,
    };

    public static CharacterEmojiLayout AboveHead => new()
    {
        anchoredPosition = new Vector2(0f, 650f),
        localScale = new Vector3(0.15f, 0.15f, 0.15f),
        rotationZ = 0f,
        preserveAspect = true,
        setNativeSize = false,
    };
}