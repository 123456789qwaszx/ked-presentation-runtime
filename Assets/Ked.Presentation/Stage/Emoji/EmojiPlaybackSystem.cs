using System.Collections.Generic;
using Ked.Presentation.Core;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────
// 서 있는 이모지들의 재생 — 프레임을 넘기고 그 프레임의 모습을 노드에 쓴다.
//
// 커맨드가 아니라 시스템이 든다: 머묾은 줄을 넘어 이어지는데 커맨드의 수명은 한 줄이다
// (CommandBase — 줄 경계에서 정리된다). 커맨드는 "세워서 재생하라"(Play) · "내려라"(Off)만 말한다.
//
// 무엇을 언제 재생하는가는 코어의 EmojiPlayback(순수한 상태 기계), 그 프레임의 모습은 EmojiClipMath다 —
// 툴의 미리보기가 같은 둘을 쓴다. 여기서는 시간을 넘기고 값을 RectTransform에 옮기기만 한다.
//
// 호스트가 프레임마다 Tick을 부른다(예제: SampleBootstrap.Update).
// Tick은 로케이터의 자리도 다시 푼다(EmojiLocatorSystem.Refresh) — 호스트가 부를 것이 하나이게.
// 슬롯이 부서지면 이모지의 노드도 함께 부서진다 — 통보받지 않고, Tick에서 사라진 것을 잊는다.
// ─────────────────────────────────────────────────────────────────────
public sealed class EmojiPlaybackSystem
{
    private sealed class Entry
    {
        public EmojiRefs Emoji;
        public EmojiDefinition Definition;
        public EmojiPlayback Playback;

        // 리그마다 지금 넣어 둔 스프라이트 키 — 바뀔 때만 다시 푼다.
        public string[] AppliedSprites;
    }

    private readonly EmojiRigBuilder _builder;
    private readonly EmojiLocatorSystem _locators;
    private readonly List<Entry> _entries = new();

    /// <param name="locators">로케이터의 자리를 푸는 곳. 주면 Tick마다 다시 푼다(포즈가 바뀌면 따라간다).</param>
    public EmojiPlaybackSystem(EmojiRigBuilder builder, EmojiLocatorSystem locators = null)
    {
        _builder = builder ?? throw new System.ArgumentNullException(nameof(builder));
        _locators = locators;
    }

    /// <summary>재생 중인(서 있는) 이모지의 수.</summary>
    public int Count => _entries.Count;

    /// <summary>
    /// 세운 이모지의 재생을 시작한다. settle이면 건너뛴 뒤의 모습으로 바로 간다 —
    /// 머무는 이모지는 머묾의 처음, 머묾 없는 이모지는 끝이라 그 자리에서 부순다.
    /// 정의의 값(오프셋 · 기본 자리 · 기본 스프라이트)은 부르는 쪽이 이미 넣었다(SetEmojiCommand.ApplyDefinition).
    /// </summary>
    public void Play(EmojiRefs emoji, EmojiDefinition definition, bool settle)
    {
        Forget(emoji);

        EmojiPlayback playback = new(definition);

        if (settle)
            playback.Settle();

        if (playback.IsDone)
        {
            _builder.DestroyEmoji(emoji);
            return;
        }

        Entry entry = new()
        {
            Emoji = emoji,
            Definition = definition,
            Playback = playback,
            AppliedSprites = new string[definition.Rigs.Count],
        };

        for (int i = 0; i < entry.AppliedSprites.Length; i++)
            entry.AppliedSprites[i] = definition.Rigs[i].SpriteKey;

        _entries.Add(entry);

        // 첫 프레임의 모습을 바로 쓴다 — 다음 Tick까지 중립 모습이 한 프레임 보이지 않게.
        Apply(entry);
    }

    /// <summary>
    /// 내린다. 퇴장이 있으면 그것을 재생한 뒤 부수고, 없거나 immediately면 바로 부순다.
    /// </summary>
    public void Off(EmojiRefs emoji, bool immediately)
    {
        Entry entry = Find(emoji);

        if (entry == null)
        {
            _builder.DestroyEmoji(emoji);
            return;
        }

        if (!immediately)
            entry.Playback.RequestOff();

        if (immediately || entry.Playback.IsDone)
        {
            _entries.Remove(entry);
            _builder.DestroyEmoji(emoji);
            return;
        }

        Apply(entry);
    }

    /// <summary>아직 재생 중인가(사라지지 않았는가).</summary>
    public bool IsPlaying(EmojiRefs emoji)
    {
        Entry entry = Find(emoji);
        return entry != null && entry.Emoji.Emoji_Offset != null && !entry.Playback.IsDone;
    }

    /// <summary>시간을 넘긴다(초). 끝난 이모지는 부순다.</summary>
    public void Tick(float deltaSeconds)
    {
        _locators?.Refresh();

        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            Entry entry = _entries[i];

            // 슬롯과 함께 부서졌다.
            if (entry.Emoji.Emoji_Offset == null)
            {
                _entries.RemoveAt(i);
                continue;
            }

            entry.Playback.Advance(deltaSeconds);

            if (entry.Playback.IsDone)
            {
                _entries.RemoveAt(i);
                _builder.DestroyEmoji(entry.Emoji);
                continue;
            }

            Apply(entry);
        }
    }

    /// <summary>전부 잊는다. 노드는 부수지 않는다 — 무대를 비우는 쪽이 리그째 부순다.</summary>
    public void Clear() => _entries.Clear();

    // ── 그 프레임의 모습을 노드에 쓴다 ───────────────────────────────

    private static void Apply(Entry entry)
    {
        EmojiRefs emoji = entry.Emoji;
        EmojiDefinition definition = entry.Definition;
        EmojiClip clip = entry.Playback.Clip;
        float frame = entry.Playback.Frame;

        // 좌우 — 인물이 뒤집혀 있으면 로케이터가 뒤집혀 있다(EmojiLocatorSystem). 이모지는 그대로 두면 통째로 거울에 비친다.
        // 예외 둘만 한 번 더 뒤집는다(EmojiSide): 방향을 따르지 않는 이모지는 오프셋에서, 바로 서야 하는 그림은 리그의 기본 자리에서.
        bool locatorFlipped = emoji.Emoji_Locator != null && emoji.Emoji_Locator.localScale.x < 0f;
        float offsetSign = EmojiSide.OffsetSign(definition, locatorFlipped);

        // 이모지 전체 — 자리의 키는 오프셋 값에 더한다(오프셋을 옮겨도 키를 다시 잡지 않는다).
        EmojiNodePose whole = EmojiClipMath.SampleNode(clip, "", frame);

        emoji.Emoji_Offset.anchoredPosition = new Vector2(
            offsetSign * (definition.Offset.X + whole.Position.X),
            definition.Offset.Y + whole.Position.Y);
        emoji.Emoji_Offset.localScale = new Vector3(offsetSign * whole.Scale.X, whole.Scale.Y, 1f);
        emoji.Emoji_Offset.localEulerAngles = new Vector3(0f, 0f, offsetSign * whole.Rotation);

        if (emoji.OffsetCanvasGroup != null)
            emoji.OffsetCanvasGroup.alpha = whole.Alpha;

        // 리그마다 — 키는 그림 노드가 받는다. 기본 자리(Place)는 바로 서야 하는 그림의 부호만 다시 쓴다.
        for (int i = 0; i < definition.Rigs.Count && i < emoji.Rigs.Count; i++)
        {
            EmojiRigEntry rigEntry = definition.Rigs[i];
            EmojiRigRefs rig = emoji.Rigs[i];

            if (rigEntry.Upright)
            {
                // 가로 배율과 회전에 부호를 곱한다 — 그림과 이 리그의 키가 만든 그대로 보인다(자리는 부모를 따라 거울 쪽).
                float placeSign = EmojiSide.PlaceSign(definition, rigEntry, locatorFlipped);

                rig.EmojiRig_Place.localScale = new Vector3(placeSign * rigEntry.Scale, rigEntry.Scale, 1f);
                rig.EmojiRig_Place.localEulerAngles = new Vector3(0f, 0f, placeSign * rigEntry.Rotation);
            }

            EmojiNodePose pose = EmojiClipMath.SampleNode(clip, rigEntry.Name, frame);

            rig.EmojiRig_Image.anchoredPosition = new Vector2(pose.Position.X, pose.Position.Y);
            rig.EmojiRig_Image.localScale = new Vector3(pose.Scale.X, pose.Scale.Y, 1f);
            rig.EmojiRig_Image.localEulerAngles = new Vector3(0f, 0f, pose.Rotation);
            rig.CanvasGroup.alpha = pose.Alpha;

            // 그림 바꾸기 — 채널이 없거나 첫 키 앞이면 리그의 기본 스프라이트다.
            string spriteKey = pose.SpriteKey ?? rigEntry.SpriteKey;

            if (!string.Equals(spriteKey, entry.AppliedSprites[i], System.StringComparison.Ordinal))
            {
                entry.AppliedSprites[i] = spriteKey;
                SetEmojiCommand.ApplySprite(rig, spriteKey);
            }
        }
    }

    private Entry Find(EmojiRefs emoji)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            if (ReferenceEquals(_entries[i].Emoji, emoji))
                return _entries[i];
        }

        return null;
    }

    // 같은 것, 그리고 같은 자리에 같은 이름으로 섰던 옛것(빌더가 이미 갈았다)을 잊는다.
    private void Forget(EmojiRefs emoji)
    {
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            EmojiRefs other = _entries[i].Emoji;

            bool replaced = ReferenceEquals(other, emoji)
                            || other.Emoji_Offset == null
                            || (other.Emoji_Locator == emoji.Emoji_Locator
                                && string.Equals(other.EmojiName, emoji.EmojiName, System.StringComparison.Ordinal));

            if (replaced)
                _entries.RemoveAt(i);
        }
    }
}
