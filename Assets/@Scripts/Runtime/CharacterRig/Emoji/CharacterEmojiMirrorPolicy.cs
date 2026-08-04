using System;
using UnityEngine;

// 각 enum의 0 = 해당 축의 Default 동작.
// 따라서 세 0이 모두 모이면 CharacterEmojiMirrorProfile.Default와 동일함.
// (Default profile은 직렬화 시 (0,0,0)이 됨.)
// 새 mirror 축을 추가할 때도 0번 멤버를 "Default 동작"으로 두면 기존 에셋이 마이그레이션 없이 동일하게 동작.
public enum CharacterEmojiPlacementMirrorPolicy
{
    MirrorWithCharacterFacing = 0,
    None = 10,
}

public enum CharacterEmojiMotionMirrorPolicy
{
    None = 0,
    MirrorWithCharacterFacing = 10,
}

public enum CharacterEmojiSpriteMirrorPolicy
{
    KeepUpright = 0,
    MirrorWithCharacterFacing = 10,
}

[Serializable]
public sealed class CharacterEmojiMirrorProfile
{
    [Tooltip("FocusPoint 기준 emoji placement offset을 캐릭터 facing에 맞춰 X축 대칭할지 결정합니다.")]
    public CharacterEmojiPlacementMirrorPolicy placementMirror =
        CharacterEmojiPlacementMirrorPolicy.MirrorWithCharacterFacing;

    [Tooltip("이동 delta, direction, rotationZ, pivot 같은 motion 값을 캐릭터 facing에 맞춰 X축 대칭할지 결정합니다.")]
    public CharacterEmojiMotionMirrorPolicy motionMirror =
        CharacterEmojiMotionMirrorPolicy.None;

    [Tooltip("Emoji sprite 자체를 캐릭터 facing에 맞춰 좌우반전할지 결정합니다. 기본값은 upright 유지입니다.")]
    public CharacterEmojiSpriteMirrorPolicy spriteMirror =
        CharacterEmojiSpriteMirrorPolicy.KeepUpright;

    public static CharacterEmojiMirrorProfile Default => new()
    {
        placementMirror = CharacterEmojiPlacementMirrorPolicy.MirrorWithCharacterFacing,
        motionMirror = CharacterEmojiMotionMirrorPolicy.None,
        spriteMirror = CharacterEmojiSpriteMirrorPolicy.KeepUpright,
    };
}

public readonly struct CharacterEmojiMirrorContext
{
    public readonly CharacterFacing facing;
    public readonly CharacterEmojiMirrorProfile profile;

    public int SignX => facing == CharacterFacing.Left ? -1 : 1;

    public bool ShouldMirrorPlacement =>
        facing == CharacterFacing.Left &&
        profile != null &&
        profile.placementMirror == CharacterEmojiPlacementMirrorPolicy.MirrorWithCharacterFacing;

    public bool ShouldMirrorMotion =>
        facing == CharacterFacing.Left &&
        profile != null &&
        profile.motionMirror == CharacterEmojiMotionMirrorPolicy.MirrorWithCharacterFacing;

    public bool ShouldMirrorSprite =>
        facing == CharacterFacing.Left &&
        profile != null &&
        profile.spriteMirror == CharacterEmojiSpriteMirrorPolicy.MirrorWithCharacterFacing;

    public CharacterEmojiMirrorContext(
        CharacterFacing facing,
        CharacterEmojiMirrorProfile profile)
    {
        this.facing = facing;
        this.profile = profile ?? CharacterEmojiMirrorProfile.Default;
    }

    public Vector2 MirrorPlacementOffset(Vector2 value)
    {
        return ShouldMirrorPlacement
            ? new Vector2(-value.x, value.y)
            : value;
    }

    public float MirrorPlacementRotationZ(float value)
    {
        return ShouldMirrorPlacement ? -value : value;
    }

    public Vector2 MirrorMotionVector(Vector2 value)
    {
        return ShouldMirrorMotion
            ? new Vector2(-value.x, value.y)
            : value;
    }

    public Vector3 MirrorMotionVector(Vector3 value)
    {
        return ShouldMirrorMotion
            ? new Vector3(-value.x, value.y, value.z)
            : value;
    }

    public float MirrorRotationZ(float value)
    {
        return ShouldMirrorMotion ? -value : value;
    }

    public Vector3 MirrorEulerZ(Vector3 value)
    {
        if (!ShouldMirrorMotion)
            return value;

        value.z = -value.z;
        return value;
    }

    public Vector2 MirrorPivot(Vector2 pivot)
    {
        return ShouldMirrorMotion
            ? new Vector2(1f - pivot.x, pivot.y)
            : pivot;
    }

    public bool MirrorStartPositive(bool startPositive)
    {
        return ShouldMirrorMotion ? !startPositive : startPositive;
    }

    public CharRigDirection MirrorDirection(CharRigDirection direction)
    {
        if (!ShouldMirrorMotion)
            return direction;

        switch (direction)
        {
            case CharRigDirection.Left:
                return CharRigDirection.Right;

            case CharRigDirection.Right:
                return CharRigDirection.Left;

            default:
                return direction;
        }
    }
}