using System;
using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────
// 어느 슬롯에 어느 이모지가 서 있는가.
//
// 이모지의 오브젝트는 슬롯의 리그 밑에 산다 — 슬롯이 부서지거나 다시 선언되면(CharacterRigRegistry)
// 이모지도 함께 부서진다. 이 장부는 그것을 **따로 통보받지 않는다**: 읽고 쓸 때마다 "지금 그 슬롯의
// CharSlot_Scale 밑에 서 있는 것"만 남기고 나머지를 잊는다(Prune). 캐릭터 쪽 코드가 이모지를 모르게 하려는 것이다.
// ─────────────────────────────────────────────────────────────────────
public sealed class EmojiRigRegistry
{
    private readonly Dictionary<string, Dictionary<string, EmojiRefs>> _bySlot = new(StringComparer.Ordinal);

    /// <summary>이모지를 장부에 올린다. 같은 이름은 덮는다(오브젝트는 빌더가 이미 갈았다).</summary>
    public void Register(string slotKey, RectTransform charSlotScale, EmojiRefs emoji)
    {
        if (string.IsNullOrEmpty(slotKey))
            throw new ArgumentException("슬롯 키가 비어 있다.", nameof(slotKey));

        if (emoji == null)
            throw new ArgumentNullException(nameof(emoji));

        Prune(slotKey, charSlotScale);

        if (!_bySlot.TryGetValue(slotKey, out Dictionary<string, EmojiRefs> emojis))
        {
            emojis = new Dictionary<string, EmojiRefs>(StringComparer.Ordinal);
            _bySlot[slotKey] = emojis;
        }

        emojis[emoji.EmojiName] = emoji;
    }

    public bool TryGet(string slotKey, RectTransform charSlotScale, string emojiName, out EmojiRefs emoji)
    {
        Prune(slotKey, charSlotScale);

        emoji = null;

        return _bySlot.TryGetValue(slotKey ?? "", out Dictionary<string, EmojiRefs> emojis)
               && emojis.TryGetValue(emojiName ?? "", out emoji);
    }

    /// <summary>장부에서 뺀다. 오브젝트를 부수는 것은 부르는 쪽(EmojiRigBuilder.DestroyEmoji)이다.</summary>
    public bool Remove(string slotKey, RectTransform charSlotScale, string emojiName, out EmojiRefs emoji)
    {
        if (!TryGet(slotKey, charSlotScale, emojiName, out emoji))
            return false;

        _bySlot[slotKey].Remove(emojiName);
        return true;
    }

    /// <summary>그 슬롯에 서 있는 이모지를 전부 장부에서 빼서 results에 담는다.</summary>
    public void RemoveAll(string slotKey, RectTransform charSlotScale, List<EmojiRefs> results)
    {
        Prune(slotKey, charSlotScale);

        if (!_bySlot.TryGetValue(slotKey ?? "", out Dictionary<string, EmojiRefs> emojis))
            return;

        results?.AddRange(emojis.Values);
        _bySlot.Remove(slotKey);
    }

    /// <summary>그 슬롯에 서 있는 이모지의 수.</summary>
    public int CountOf(string slotKey, RectTransform charSlotScale)
    {
        Prune(slotKey, charSlotScale);

        return _bySlot.TryGetValue(slotKey ?? "", out Dictionary<string, EmojiRefs> emojis) ? emojis.Count : 0;
    }

    /// <summary>전부 잊는다 — 무대를 비울 때(PresentationStage.Clear). 오브젝트는 리그와 함께 부서진다.</summary>
    public void Clear() => _bySlot.Clear();

    // 부서졌거나(null), 옛 리그 밑에 남은 것(재선언 — 옛 리그는 프레임 끝에 부서진다)을 잊는다.
    private void Prune(string slotKey, RectTransform charSlotScale)
    {
        if (slotKey == null || !_bySlot.TryGetValue(slotKey, out Dictionary<string, EmojiRefs> emojis))
            return;

        List<string> stale = null;

        foreach (KeyValuePair<string, EmojiRefs> pair in emojis)
        {
            EmojiRefs emoji = pair.Value;

            bool alive = emoji != null
                         && emoji.Emoji_Offset != null
                         && emoji.Emoji_Locator != null
                         && emoji.Emoji_Locator.parent == charSlotScale;

            if (!alive)
                (stale ??= new List<string>()).Add(pair.Key);
        }

        if (stale == null)
            return;

        foreach (string name in stale)
            emojis.Remove(name);

        if (emojis.Count == 0)
            _bySlot.Remove(slotKey);
    }
}
