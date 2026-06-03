using System.Collections.Generic;

public sealed partial class YarnCommandBridge
{
    // Inline markup용: 현재 재생 중인 라인 도중 즉시 표시
    public void PlayInlineEmojiByCharacterNow(string roleKey, string cue = "")
    {
        string emojiKey = cue;

        SetCharacterEmojiCommandSpecCharR spec = BuildSetCharacterEmojiSpec(roleKey, emojiKey);
        var specs = new List<CommandSpecBase>(1) { spec };

        _playbackDriver.PlayImmediate(specs);
    }

    public void HideInlineEmojiByCharacterNow(string roleKey)
    {
        SetCharacterEmojiCommandSpecCharR spec = BuildSetCharacterEmojiSpec(roleKey, "");
        var specs = new List<CommandSpecBase>(1) { spec };
        
        _playbackDriver.PlayImmediate(specs);
    }
}