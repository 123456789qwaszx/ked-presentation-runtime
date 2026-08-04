using System;
using UnityEngine;

// 캐릭터의 FocusPoint + RigSpace offset.
[Serializable]
public struct CharacterEmojiPlacement
{
    [Header("Focus Point")]
    public CharacterFocusPreset focusPreset;

    [Tooltip("FocusPointInRigSpace 기준 추가 오프셋입니다.")]
    public Vector2 offsetFromFocusInRigSpace;

    [Header("Cast Transform")]
    public Vector3 localScale;
    public float rotationZ;

    [Header("Image")]
    public bool preserveAspect;
    public bool setNativeSize;

    public static CharacterEmojiPlacement Default => FaceRight;

    public static CharacterEmojiPlacement FaceLeft => new()
    {
        focusPreset = CharacterFocusPreset.Face,
        offsetFromFocusInRigSpace = new Vector2(-140f, 70f),
        localScale = new Vector3(0.15f, 0.15f, 0.15f),
        rotationZ = 0f,
        preserveAspect = true,
        setNativeSize = false,
    };

    public static CharacterEmojiPlacement FaceRight => new()
    {
        focusPreset = CharacterFocusPreset.Face,
        offsetFromFocusInRigSpace = new Vector2(140f, 70f),
        localScale = new Vector3(0.15f, 0.15f, 0.15f),
        rotationZ = 0f,
        preserveAspect = true,
        setNativeSize = false,
    };

    public static CharacterEmojiPlacement AboveFace => new()
    {
        focusPreset = CharacterFocusPreset.Face,
        offsetFromFocusInRigSpace = new Vector2(0f, 170f),
        localScale = new Vector3(0.15f, 0.15f, 0.15f),
        rotationZ = 0f,
        preserveAspect = true,
        setNativeSize = false,
    };
}