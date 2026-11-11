using System;
using System.Collections.Generic;
using Ked.Presentation.Core;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────
// emojis.json — 이모지의 구성(이름 · 오프셋 · 리그들). motions.json과 같은 결이다: 작가 자산이라
// 대본 번들 곁에 살고, 여기서는 읽기만 한다. 만드는 쪽은 모션 툴이다(과제 3 — 지금은 손으로 쓴 견본).
// 파일이 없으면 이모지 0개로 조용히 동작한다 — 이모지를 안 쓰는 프로젝트가 정상 경로다.
//
// 형식과 뜻은 코어의 EmojisFileDto · EmojiDefinitionLoader · EmojiRules가 규범이다(툴이 같은 것을 쓴다).
// ─────────────────────────────────────────────────────────────────────
public sealed class EmojiLibrary
{
    public const string BundleFileName = "emojis.json";

    /// <summary>스프라이트 키의 뿌리 — Resources/Emojis/{키}.</summary>
    public const string SpriteResourcesRoot = "Emojis";

    private readonly Dictionary<string, EmojiDefinition> _emojis;

    public static EmojiLibrary Empty { get; } = new(new Dictionary<string, EmojiDefinition>(StringComparer.Ordinal));

    private EmojiLibrary(Dictionary<string, EmojiDefinition> emojis)
    {
        _emojis = emojis;
    }

    public int Count => _emojis.Count;

    /// <summary>리듀서 튜닝(StageReducerTuning.Emojis)에 그대로 넘긴다 — 재생과 폴드가 같은 정의를 본다.</summary>
    public IReadOnlyDictionary<string, EmojiDefinition> Definitions => _emojis;

    public bool TryGet(string emojiName, out EmojiDefinition emoji)
        => _emojis.TryGetValue(emojiName ?? "", out emoji);

    /// <summary>본문을 읽는다. 파일을 어디서 가져오는지(TextAsset · 경로)는 호스트가 정한다.</summary>
    public static EmojiLibrary Parse(string json, string sourceLabel)
    {
        EmojisFileDto file;

        try
        {
            file = JsonUtility.FromJson<EmojisFileDto>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[EmojiLibrary] {sourceLabel}: JSON을 읽지 못했다 — {e.Message}. 이모지 0개로 동작한다.");
            return Empty;
        }

        if (file == null)
        {
            Debug.LogError($"[EmojiLibrary] {sourceLabel}: 빈 문서다. 이모지 0개로 동작한다.");
            return Empty;
        }

        if (file.schema != EmojiDefinitionLoader.Schema)
        {
            Debug.LogWarning(
                $"[EmojiLibrary] {sourceLabel}: schema '{file.schema}' (기대 '{EmojiDefinitionLoader.Schema}'). " +
                "읽기는 시도한다 — 어긋난 이모지는 검증이 거른다.");
        }

        Dictionary<string, EmojiDefinition> emojis = EmojiDefinitionLoader.Build(
            file,
            (name, why) => Debug.LogWarning($"[EmojiLibrary] {sourceLabel}: 이모지 '{name}'을 무시한다 — {why}."));

        return new EmojiLibrary(emojis);
    }
}
