using System;
using System.Collections;
using Ked.Presentation.Core;
using UnityEngine;

/// <summary>
/// emoji {누구} {이름} — 슬롯에 이모지를 세운다(docs/PLAN_emoji.md §4).
/// </summary>
[Serializable]
public sealed class SetEmojiCommandSpec : EmojiRigCommandSpecBase
{
}

// 슬롯을 풀고 · 정의를 찾고 · 빌더로 세우고 · 정의의 값을 넣는다. 아직 움직이지 않는다(과제 3).
//   Emoji_Locator   ← RootSpace(지금은 기본값 하나 — 캐릭터 + 바리에이션별 표는 뒤에 붙는다)
//   Emoji_Offset    ← 정의의 offset
//   EmojiRig_Place  ← 리그의 place
//   EmojiRig_Image  ← 리그의 sprite(Resources/Emojis/{키}), 크기는 원본 픽셀
// 같은 이름이 서 있으면 부수고 새로 세운다. 모르는 이모지는 경고하고 건너뛴다(폴드는 Unhandled).
public sealed class SetEmojiCommand : CommandBase
{
    private readonly PresentationStage _stage;
    private readonly EmojiRigBuilder _builder;
    private readonly EmojiLibrary _library;
    private readonly Vector2 _defaultRootSpace;
    private readonly SetEmojiCommandSpec _spec;

    // 건너뛸 때도 세운다 — 세워져 있어야 뒤의 커맨드(emoji_off · 과제 3의 모션)가 대상을 찾는다.
    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    public SetEmojiCommand(
        PresentationStage stage,
        EmojiRigBuilder builder,
        EmojiLibrary library,
        Vector2 defaultRootSpace,
        SetEmojiCommandSpec spec)
    {
        _stage = stage;
        _builder = builder;
        _library = library;
        _defaultRootSpace = defaultRootSpace;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_library.TryGet(_spec.emojiName, out EmojiDefinition definition))
        {
            Debug.LogWarning(
                $"[SetEmojiCommand] 이모지 '{_spec.emojiName}'이 {EmojiLibrary.BundleFileName}에 없다. 건너뛴다. " +
                $"roleKey='{_spec.roleKey}'.");

            yield break;
        }

        EmojiRigTargets.Resolve(_stage, _spec.roleKey, out string slotKey, out RectTransform charSlotScale, out string rolePrefix);

        RectTransform locator = _builder.EnsureLocator(charSlotScale, rolePrefix);
        locator.anchoredPosition = _defaultRootSpace;

        string[] rigNames = new string[definition.Rigs.Count];

        for (int i = 0; i < rigNames.Length; i++)
            rigNames[i] = definition.Rigs[i].Name;

        EmojiRefs emoji = _builder.BuildEmoji(locator, rolePrefix, definition.Name, rigNames);

        ApplyDefinition(emoji, definition);

        _stage.emojis.Register(slotKey, charSlotScale, emoji);
    }

    /// <summary>정의의 값을 세운 이모지에 넣는다 — 오프셋 · 리그마다 기본 자리와 스프라이트.</summary>
    public static void ApplyDefinition(EmojiRefs emoji, EmojiDefinition definition)
    {
        emoji.Emoji_Offset.anchoredPosition = new Vector2(definition.Offset.X, definition.Offset.Y);

        for (int i = 0; i < definition.Rigs.Count; i++)
        {
            EmojiRigEntry entry = definition.Rigs[i];
            EmojiRigRefs rig = emoji.Rigs[i];

            rig.EmojiRig_Place.anchoredPosition = new Vector2(entry.Place.X, entry.Place.Y);

            Sprite sprite = ResourcesSpriteResolver.Resolve(EmojiLibrary.SpriteResourcesRoot, entry.SpriteKey);

            // 못 찾으면 이미지를 끈다 — sprite 없는 Image는 흰 사각형을 그린다(해석기가 이미 경고를 남겼다).
            rig.Image.sprite = sprite;
            rig.Image.enabled = sprite != null;

            // 크기는 원본 픽셀 — 초상 · 배경과 같은 정책(CharRigImageSizingMode.SourcePixels).
            if (sprite != null)
                CharRigImageSizingPolicy.Apply(rig.Image, sprite, CharRigImageSizingMode.SourcePixels);
        }
    }
}
