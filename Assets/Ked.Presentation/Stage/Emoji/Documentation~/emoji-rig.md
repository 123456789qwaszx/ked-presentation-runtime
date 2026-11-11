# 이모지 리그 — 계층과 그 까닭

정의와 결정의 기록은 저장소의 [docs/emoji-rig.md](../../../../../docs/emoji-rig.md)다. 이 문서는 코드 곁에서 **왜 이 노드가 이 자리인가**만 적는다.
캐릭터 리그의 같은 문서: [rig-axis-order.md](../../Character/Rig/Documentation~/rig-axis-order.md).

## 0. 두 문장

> **이모지 리그는 스프라이트 하나의 뼈대다**(두 단). 이모지는 그 리그 여러 개를 오프셋 하나 밑에 묶은 것이다.
>
> **계층은 `EmojiRigSchema`가 소유한다.** 프리팹이 없다 — `EmojiRigBuilder`가 스키마로 세운다.

## 1. 계층

```
CharSlot_Scale               (캐릭터 리그 — 이 스키마의 것이 아니다)
 └ Emoji_Locator             슬롯에 하나.   캐릭터 + 바리에이션이 정하는 RootSpace
    └ Emoji_Offset           이모지에 하나. 이모지별 오프셋 + 이모지 전체를 움직이는 키
       └ EmojiRig_Place      리그마다.      기본 자리 — 키를 받지 않는다
          └ EmojiRig_Image   리그마다.      스프라이트를 들고 키를 받는다  [Image · CanvasGroup]
```

| 노드 | 몇 번 서나 | 붙는 점 | 크기 | 키 |
|---|---|---|---|---|
| `Emoji_Locator` | 슬롯에 하나 | 부모 rect의 **아래 가운데**(발) | 0 | — |
| `Emoji_Offset` | 이모지에 하나 | 가운데 | 0 | 받는다 |
| `EmojiRig_Place` | 리그마다 | 가운데 | 0 | 받지 않는다 |
| `EmojiRig_Image` | 리그마다 | 가운데 | 스프라이트의 원본 픽셀(넣기 전에는 100×100) | 받는다 |

모든 노드는 **점에 붙는다**(anchorMin = anchorMax) — 늘어나는 노드가 없다. 피벗은 전부 가운데다.

## 2. 자리의 뜻

- **`Emoji_Locator`가 발에 붙는다.** `CharSlot_Scale`은 무대 크기로 늘어난 rect이고 피벗이 발(아래 가운데)이다 — 서 있는 인물의 기준점은 발이다(`rig-axis-order.md` §3.6). RootSpace 값(머리가 어디인가)은 그 발에서 잰다. 캐릭터별 그림 보정(`CharacterPortrait_VisualOffset`)은 형제라 이모지에 전해지지 않으므로, 로케이터가 캐릭터 + 바리에이션별 값으로 그것을 대신한다.
- **`Emoji_Locator`는 `CharSlot_Scale`의 마지막 자식이다.** 초상보다 뒤에 그려 인물 위에 보인다.
- **`Emoji_Offset`이 경계다.** 그 위는 캐릭터의 것, 그 아래는 이모지의 것이다. 이모지 전체를 움직이는 키는 오프셋 값에 **더해서** 읽는다 — 값과 키가 한 노드에 있지만 오프셋을 옮겨도 키를 다시 잡지 않는다.
- **`EmojiRig_Place` / `EmojiRig_Image`가 두 단이다.** 값을 든 노드(기본 자리)와 키를 받는 노드를 나눴다. 기본 자리를 옮겨도 키는 그대로다.
- **축을 노드로 쪼개지 않았다.** 캐릭터 리그가 `Track_X` · `Track_Y`를 따로 둔 것은 커맨드마다 트윈이 따로 돌아 같은 프로퍼티를 두고 싸우기 때문이다. 이모지 리그는 타임라인 하나가 움직인다 — 한 노드가 위치 · 배율 · 회전 · 불투명도 · 그림을 전부 받는다.
- **불투명도는 `CanvasGroup`이다.** 덤프의 가시성 축(`hasCanvasGroup` · `canvasGroupAlpha`)과 같은 길이다. 태어날 때 1이다(인물의 초상은 0으로 태어나 `fade_in`으로 들어오지만, 이모지는 타임라인이 처음부터 정한다).
- **그림은 클릭을 받지 않는다**(`raycastTarget = false`).

## 3. 이름

하이어라키 이름은 접두를 붙여 전역에서 하나뿐이게 한다(캐릭터 리그의 `rolePrefix`와 같은 까닭 — 이름으로 찾아도 안전하고, 누가 누구인지 보이고, 노드 키로 대조할 수 있다).

```
{rolePrefix}Emoji_Locator                       harin_Emoji_Locator
{rolePrefix}{이모지}_Emoji_Offset                harin_flower_Emoji_Offset
{rolePrefix}{이모지}_{리그}_EmojiRig_Place        harin_flower_big_EmojiRig_Place
{rolePrefix}{이모지}_{리그}_EmojiRig_Image        harin_flower_big_EmojiRig_Image
```

리그의 이름은 툴이 붙인다. 한 이모지 안에서 겹치면 막는다 — 키가 리그를 이름으로 가리킨다.

## 4. 캐릭터 리그와 다른 점

| | 캐릭터 리그 | 이모지 리그 |
|---|---|---|
| 노드의 수 | 고정(스키마의 배열 그대로) | 틀은 고정, **리그의 수는 이모지마다 다르다**(`Repeat.PerRig`) |
| 세운 것을 드는 법 | 필드 이름 = 하이어라키 이름(`CharacterRigRefs`) | 리그는 이름으로 찾는다(`EmojiRefs.TryGetRig`). 리그 하나 안에서는 필드다(`EmojiRigRefs`) |
| 프리팹 | 받되 믿지 않는다(검사하고 어긋나면 다시 세운다) | 없다 |
| 덤프 | 세운 리그 그대로 | **틀의 견본**(리그 하나) — 되풀이되는 노드는 `repeatsPerRig` |

## 5. 노드를 고칠 때

1. `EmojiRigSchema.Refs` · `Nodes`(부모 · 몇 번 서나 · 키를 받는가 · 다는 것)
2. `EmojiRigRefs` / `EmojiRefs`의 필드와 `EmojiRigBuilder.Build`
3. 덤프를 다시 낸다(`Ked / U12 / Export Presentation Tuning Dump`) — 안 하면 `RigSchemaTreeUnityParityTests`가 낡은 덤프를 신고한다
4. `EmojiRigBuilderTests` · `RigSchemaTreeUnityParityTests`
