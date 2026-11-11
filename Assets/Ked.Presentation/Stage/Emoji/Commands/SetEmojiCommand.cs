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

// 슬롯을 풀고 · 정의를 찾고 · 빌더로 세우고 · 정의의 값을 넣고 · 재생을 맡긴다(EmojiPlaybackSystem).
//   Emoji_Locator   ← RootSpace(지금은 기본값 하나 — 캐릭터 + 바리에이션별 표는 뒤에 붙는다)
//   Emoji_Offset    ← 정의의 offset
//   EmojiRig_Place  ← 리그의 place · scale · rotation(기본 자리 · 배율 · 회전)
//   EmojiRig_Image  ← 리그의 sprite(Resources/Emojis/{키}), 크기는 원본 픽셀
// 같은 이름이 서 있으면 부수고 새로 세운다. 모르는 이모지는 경고하고 건너뛴다(폴드는 Unhandled).
//
// 머묾이 있는가가 종류를 정한다(EmojiDefinition.Stays):
//   머무는 이모지  — 장부(stage.emojis)에 오른다. 등장 → 머묾을 되풀이. emoji_off로 내린다.
//   머묾 없는 이모지 — 한 번 재생하고 스스로 사라진다. 장부에 오르지 않는다(이름으로 내릴 것이 아니다).
// 건너뛸 때는 끝난 뒤의 모습만 남긴다: 머무는 이모지는 머묾의 처음으로 세우고, 머묾 없는 이모지는 세우지 않는다.
// 재생은 줄을 넘어 이어진다 — 다음 줄로 넘어가도 끝까지 간다(캐릭터 모션과 다르다).
public sealed class SetEmojiCommand : CommandBase
{
    private readonly PresentationStage _stage;
    private readonly EmojiRigBuilder _builder;
    private readonly EmojiLibrary _library;
    private readonly EmojiPlaybackSystem _playback;
    private readonly Vector2 _defaultRootSpace;
    private readonly SetEmojiCommandSpec _spec;

    // 건너뛸 때도 실행한다 — 머무는 이모지는 서 있어야 뒤의 emoji_off가 대상을 찾는다.
    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    // wait — 머묾 없는 이모지가 사라질 때까지 줄을 붙잡는다. 머무는 이모지에는 뜻이 없다(끝나지 않는다).
    public override bool WaitForCompletion => _spec.wait;

    public SetEmojiCommand(
        PresentationStage stage,
        EmojiRigBuilder builder,
        EmojiLibrary library,
        EmojiPlaybackSystem playback,
        Vector2 defaultRootSpace,
        SetEmojiCommandSpec spec)
    {
        _stage = stage;
        _builder = builder;
        _library = library;
        _playback = playback;
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

        bool settle = scope.ShouldCompressCommandExecution;

        // 건너뛰는 중의 머묾 없는 이모지 — 끝난 뒤에는 남는 것이 없다. 세우지 않는다.
        if (settle && !definition.Stays)
            yield break;

        EmojiRigTargets.Resolve(_stage, _spec.roleKey, out string slotKey, out RectTransform charSlotScale, out string rolePrefix);

        RectTransform locator = _builder.EnsureLocator(charSlotScale, rolePrefix);
        locator.anchoredPosition = _defaultRootSpace;

        string[] rigNames = new string[definition.Rigs.Count];

        for (int i = 0; i < rigNames.Length; i++)
            rigNames[i] = definition.Rigs[i].Name;

        EmojiRefs emoji = _builder.BuildEmoji(locator, rolePrefix, definition.Name, rigNames);

        ApplyDefinition(emoji, definition);

        if (definition.Stays)
            _stage.emojis.Register(slotKey, charSlotScale, emoji);

        _playback.Play(emoji, definition, settle);

        if (!_spec.wait || definition.Stays)
            yield break;

        while (_playback.IsPlaying(emoji))
            yield return null;
    }

    /// <summary>정의의 값을 세운 이모지에 넣는다 — 오프셋 · 리그마다 기본 자리와 스프라이트.</summary>
    public static void ApplyDefinition(EmojiRefs emoji, EmojiDefinition definition)
    {
        emoji.Emoji_Offset.anchoredPosition = new Vector2(definition.Offset.X, definition.Offset.Y);

        for (int i = 0; i < definition.Rigs.Count; i++)
        {
            EmojiRigEntry entry = definition.Rigs[i];
            EmojiRigRefs rig = emoji.Rigs[i];

            // 기본 자리 · 배율 · 회전 — 키는 그 위(EmojiRig_Image)에서 중립(자리 0 · 배율 1 · 회전 0)을 기준으로 잡힌다.
            rig.EmojiRig_Place.anchoredPosition = new Vector2(entry.Place.X, entry.Place.Y);
            rig.EmojiRig_Place.localScale = new Vector3(entry.Scale, entry.Scale, 1f);
            rig.EmojiRig_Place.localEulerAngles = new Vector3(0f, 0f, entry.Rotation);

            ApplySprite(rig, entry.SpriteKey);
        }
    }

    /// <summary>리그의 그림 노드에 스프라이트를 넣는다(Resources/Emojis/{키}) — 세울 때와 그림 바꾸기 채널이 쓴다.</summary>
    public static void ApplySprite(EmojiRigRefs rig, string spriteKey)
    {
        Sprite sprite = ResourcesSpriteResolver.Resolve(EmojiLibrary.SpriteResourcesRoot, spriteKey);

        // 못 찾으면 이미지를 끈다 — sprite 없는 Image는 흰 사각형을 그린다(해석기가 이미 경고를 남겼다).
        rig.Image.sprite = sprite;
        rig.Image.enabled = sprite != null;

        // 크기는 원본 픽셀 — 초상 · 배경과 같은 정책(CharRigImageSizingMode.SourcePixels).
        if (sprite != null)
            CharRigImageSizingPolicy.Apply(rig.Image, sprite, CharRigImageSizingMode.SourcePixels);
    }
}
