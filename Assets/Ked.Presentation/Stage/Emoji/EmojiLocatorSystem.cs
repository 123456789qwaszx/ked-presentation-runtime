using System;
using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────
// 슬롯의 Emoji_Locator를 제자리에 둔다 — 그 슬롯에 지금 선 캐릭터와 포즈의 자리(EmojiLocatorTuningDBSO),
// 그리고 인물의 방향.
//
// 좌우: 이모지와 표의 자리는 뒤집히지 않은(오른쪽을 보는) 인물을 기준으로 만든다. 인물이 뒤집혀 있으면
// 로케이터의 자리 x와 가로 배율을 뒤집는다 — 그 밑의 이모지가 통째로 거울에 비친다(이모지의 값과 키는 그대로다).
// 예외(FixedSide 이모지 · Upright 리그)는 재생 쪽이 한 번 더 뒤집는다(EmojiSide).
//
// 자리를 푸는 곳은 여기 하나다: 세울 때(Place)와, 서 있는 동안 포즈나 방향이 바뀌었을 때(Refresh).
// 캐릭터 쪽 코드는 이모지에 알리지 않는다 — 이쪽이 CastRegistry를 읽어 값이 달라졌으면 다시 놓는다
// (장부 EmojiRigRegistry가 통보받지 않고 "지금 선 것"만 남기는 것과 같은 방식이다).
// 표에 없는 캐릭터 · 표가 없는 호스트 · 캐스팅되지 않은 슬롯은 기본 자리에 선다.
// ─────────────────────────────────────────────────────────────────────
public sealed class EmojiLocatorSystem
{
    private readonly struct Placed
    {
        public readonly string SlotKey;
        public readonly RectTransform Locator;

        public Placed(string slotKey, RectTransform locator)
        {
            SlotKey = slotKey;
            Locator = locator;
        }
    }

    private readonly PresentationStage _stage;
    private readonly EmojiLocatorTuningDBSO _tuning;
    private readonly Vector2 _defaultPosition;
    private readonly List<Placed> _placed = new();

    /// <param name="tuning">캐릭터 + 포즈별 표. null이면 모두 기본 자리다.</param>
    /// <param name="defaultPosition">표에 없을 때의 자리 — 발(CharSlot_Scale의 아래 가운데)에서 잰 px, y 위.</param>
    public EmojiLocatorSystem(PresentationStage stage, EmojiLocatorTuningDBSO tuning, Vector2 defaultPosition)
    {
        _stage = stage ?? throw new ArgumentNullException(nameof(stage));
        _tuning = tuning;
        _defaultPosition = defaultPosition;
    }

    /// <summary>놓아 둔(살아 있는지는 가리지 않은) 로케이터의 수.</summary>
    public int Count => _placed.Count;

    /// <summary>그 슬롯의 로케이터가 설 자리 — 지금 선 캐릭터와 포즈로 풀고, 인물이 뒤집혀 있으면 x를 뒤집는다.</summary>
    public Vector2 Resolve(string slotKey)
    {
        Vector2 position = ResolveUnmirrored(slotKey);

        return IsFlipped(slotKey) ? new Vector2(-position.x, position.y) : position;
    }

    /// <summary>그 슬롯의 인물이 뒤집혀 있는가(왼쪽을 본다) — 로케이터가 거울에 비친다. 캐스팅 전이면 아니다.</summary>
    public bool IsFlipped(string slotKey)
    {
        return !string.IsNullOrEmpty(slotKey)
               && _stage.castRegistry.TryPeekFacing(slotKey, out CharacterFacing facing)
               && facing == CharacterFacing.Left;
    }

    // 표의 값 그대로 — 뒤집히지 않은 인물 기준.
    private Vector2 ResolveUnmirrored(string slotKey)
    {
        if (_tuning == null || string.IsNullOrEmpty(slotKey))
            return _defaultPosition;

        // Peek — 캐스팅 전의 슬롯에 이모지를 세우는 것은 잘못이 아니다(경고하지 않는다).
        if (!_stage.castRegistry.TryPeekCharacter(slotKey, out string character))
            return _defaultPosition;

        _stage.castRegistry.TryGetVariant(slotKey, out string variant);

        return _tuning.TryResolve(character, variant, out Vector2 position) ? position : _defaultPosition;
    }

    // 자리와 방향을 로케이터에 쓴다 — 달라진 것만.
    private void Apply(string slotKey, RectTransform locator)
    {
        Vector2 position = Resolve(slotKey);

        if (locator.anchoredPosition != position)
            locator.anchoredPosition = position;

        Vector3 scale = IsFlipped(slotKey) ? new Vector3(-1f, 1f, 1f) : Vector3.one;

        if (locator.localScale != scale)
            locator.localScale = scale;
    }

    /// <summary>로케이터를 제자리에 놓고, 포즈나 방향이 바뀌면 따라가도록 기억한다. 슬롯에 하나다 — 같은 슬롯은 덮는다.</summary>
    public void Place(string slotKey, RectTransform locator)
    {
        if (locator == null)
            throw new ArgumentNullException(nameof(locator));

        for (int i = _placed.Count - 1; i >= 0; i--)
        {
            if (string.Equals(_placed[i].SlotKey, slotKey, StringComparison.Ordinal))
                _placed.RemoveAt(i);
        }

        _placed.Add(new Placed(slotKey, locator));
        Apply(slotKey, locator);
    }

    /// <summary>
    /// 놓아 둔 로케이터를 다시 푼다 — 포즈 · 캐릭터 · 방향이 바뀐 슬롯의 로케이터가 그 자리에서 옮겨지고 뒤집힌다.
    /// 슬롯과 함께 부서진 것은 잊는다. 프레임마다 부른다(EmojiPlaybackSystem.Tick이 부른다).
    /// </summary>
    public void Refresh()
    {
        for (int i = _placed.Count - 1; i >= 0; i--)
        {
            Placed placed = _placed[i];

            if (placed.Locator == null)
            {
                _placed.RemoveAt(i);
                continue;
            }

            Apply(placed.SlotKey, placed.Locator);
        }
    }

    /// <summary>전부 잊는다. 노드는 건드리지 않는다 — 무대를 비우는 쪽이 리그째 부순다.</summary>
    public void Clear() => _placed.Clear();
}
