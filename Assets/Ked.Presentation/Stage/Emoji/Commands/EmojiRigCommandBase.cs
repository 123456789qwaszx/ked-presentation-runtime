using System;
using UnityEngine;

/// <summary>이모지 커맨드의 바탕 — 누구에게(슬롯), 무엇을(이모지 이름).</summary>
[Serializable]
public abstract class EmojiRigCommandSpecBase : CommandSpecBase
{
    [Header("Role")]
    [Tooltip("대본이 캐릭터를 부르는 이름 — 슬롯 키 · 캐릭터 키 · @별칭. CharacterRigResolver가 슬롯으로 풉니다.")]
    public string roleKey;

    [Header("Emoji")]
    [Tooltip("emojis.json의 이모지 이름.")]
    public string emojiName;
}

/// <summary>이모지 커맨드가 함께 쓰는 것 — 슬롯을 풀고 접두를 만든다.</summary>
public static class EmojiRigTargets
{
    /// <summary>
    /// 대상을 슬롯으로 푼다. 이모지는 캐릭터를 모른다 — 여기서 받는 것은 설 자리(CharSlot_Scale)와 접두뿐이다.
    /// </summary>
    public static void Resolve(
        PresentationStage stage,
        string roleKey,
        out string slotKey,
        out RectTransform charSlotScale,
        out string rolePrefix)
    {
        slotKey = CharacterRigResolver.ResolveSlotKey(stage, roleKey);

        CharacterRigRefs rig = CharacterRigResolver.ResolveRig(stage, roleKey);
        charSlotScale = rig.CharSlot_Scale;

        // 캐릭터 리그의 접두와 같은 규칙이다(SetupCharRigCommandSpec.ResolvedRolePrefix).
        rolePrefix = slotKey.EndsWith("_", StringComparison.Ordinal) ? slotKey : $"{slotKey}_";
    }
}
