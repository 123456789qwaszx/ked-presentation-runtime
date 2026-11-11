using UnityEngine;

/// <summary>
/// 이모지 — 슬롯에 딸려 서는 작은 연출(docs/emoji-rig.md · docs/PLAN_emoji.md §4).
///
///   emoji     {누구} {이름}      →  SetEmojiCommandSpec    (정의대로 세운다 — 같은 이름이 서 있으면 새로 세운다)
///   emoji_off {누구} {이름=""}   →  ClearEmojiCommandSpec  (내린다 — 이름이 없으면 그 슬롯의 이모지 전부)
///
/// 이름은 emojis.json의 것이다. 정의에 있는지는 커맨드가 본다(파서는 정의를 모른다) — 없으면 경고하고 건너뛴다.
/// </summary>
public sealed partial class StageCommandParser
{
    private void EnqueueSetEmojiSpec(string roleKey, string emojiName)
    {
        if (string.IsNullOrWhiteSpace(emojiName))
        {
            Debug.LogWarning("[StageCommandParser] <<emoji>>에 이모지 이름이 없다. 건너뛴다.");
            return;
        }

        Emit(new SetEmojiCommandSpec
        {
            roleKey = roleKey,
            emojiName = emojiName.Trim(),
        });
    }

    private void EnqueueClearEmojiSpec(string roleKey, string emojiName)
        => Emit(new ClearEmojiCommandSpec
        {
            roleKey = roleKey,
            emojiName = (emojiName ?? "").Trim(),
        });
}
