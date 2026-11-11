using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// emoji_off {누구} {이름=""} — 슬롯의 이모지를 내린다. 이름이 없으면 그 슬롯의 이모지 전부.
/// </summary>
[Serializable]
public sealed class ClearEmojiCommandSpec : EmojiRigCommandSpecBase
{
}

// 지금은 바로 부순다 — 퇴장의 움직임은 과제 3에서 붙는다.
// 이름이 있는데 서 있지 않으면 경고하고 건너뛴다(폴드는 Unhandled). 이름이 없으면 없어도 조용하다.
public sealed class ClearEmojiCommand : CommandBase
{
    private readonly PresentationStage _stage;
    private readonly EmojiRigBuilder _builder;
    private readonly ClearEmojiCommandSpec _spec;

    // 건너뛸 때도 내린다 — 세우는 쪽(SetEmojiCommand)과 짝이다.
    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    public ClearEmojiCommand(
        PresentationStage stage,
        EmojiRigBuilder builder,
        ClearEmojiCommandSpec spec)
    {
        _stage = stage;
        _builder = builder;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        EmojiRigTargets.Resolve(_stage, _spec.roleKey, out string slotKey, out RectTransform charSlotScale, out _);

        if (string.IsNullOrEmpty(_spec.emojiName))
        {
            List<EmojiRefs> all = new();
            _stage.emojis.RemoveAll(slotKey, charSlotScale, all);

            foreach (EmojiRefs emoji in all)
                _builder.DestroyEmoji(emoji);

            yield break;
        }

        if (!_stage.emojis.Remove(slotKey, charSlotScale, _spec.emojiName, out EmojiRefs target))
        {
            Debug.LogWarning(
                $"[ClearEmojiCommand] 슬롯 '{slotKey}'에 이모지 '{_spec.emojiName}'이 서 있지 않다. 건너뛴다.");

            yield break;
        }

        _builder.DestroyEmoji(target);
    }
}
