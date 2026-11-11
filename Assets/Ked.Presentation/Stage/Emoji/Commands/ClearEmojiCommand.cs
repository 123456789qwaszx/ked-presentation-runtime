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

// 머무는 이모지를 내린다 — 퇴장(out)이 이어져 있으면 그것을 재생한 뒤 사라지고, 없으면 바로 사라진다.
// 등장 도중이어도 바로 퇴장으로 넘어간다. 건너뛸 때는 바로 부순다(끝난 뒤의 모습 = 없다).
// 장부에서는 이 줄에서 바로 빠진다 — 퇴장하는 동안은 "내려간 것"이다.
// 이름이 있는데 서 있지 않으면 경고하고 건너뛴다(폴드는 Unhandled). 이름이 없으면 없어도 조용하다.
// 머묾 없는 이모지는 장부에 없다 — 스스로 사라지므로 내릴 것이 아니다.
public sealed class ClearEmojiCommand : CommandBase
{
    private readonly PresentationStage _stage;
    private readonly EmojiPlaybackSystem _playback;
    private readonly ClearEmojiCommandSpec _spec;

    // 건너뛸 때도 내린다 — 세우는 쪽(SetEmojiCommand)과 짝이다.
    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    public ClearEmojiCommand(
        PresentationStage stage,
        EmojiPlaybackSystem playback,
        ClearEmojiCommandSpec spec)
    {
        _stage = stage;
        _playback = playback;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        EmojiRigTargets.Resolve(_stage, _spec.roleKey, out string slotKey, out RectTransform charSlotScale, out _);

        bool immediately = scope.ShouldCompressCommandExecution;

        if (string.IsNullOrEmpty(_spec.emojiName))
        {
            List<EmojiRefs> all = new();
            _stage.emojis.RemoveAll(slotKey, charSlotScale, all);

            foreach (EmojiRefs emoji in all)
                _playback.Off(emoji, immediately);

            yield break;
        }

        if (!_stage.emojis.Remove(slotKey, charSlotScale, _spec.emojiName, out EmojiRefs target))
        {
            Debug.LogWarning(
                $"[ClearEmojiCommand] 슬롯 '{slotKey}'에 이모지 '{_spec.emojiName}'이 서 있지 않다. 건너뛴다.");

            yield break;
        }

        _playback.Off(target, immediately);
    }
}
