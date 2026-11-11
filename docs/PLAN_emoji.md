# PLAN — 이모지 (2026-11-11)

| | |
|---|---|
| 브랜치 | `ked-presentation-runtime` · `feat/emojiRigSchema`(`dev` @ `50537b0e`에서 갈랐다) |
| 정의 | [emoji-rig.md](emoji-rig.md) — 이모지 리그가 무엇인가. 이 문서는 **무엇을 어떤 순서로 만드는가**만 적는다 |
| 결정 | 아미야(소유자) 2026-11-11: 과제 셋으로 나눈다. 과제마다 소유자의 지시로 시작한다 |
| 상태 | **과제 1 — 코드와 테스트를 썼다(2026-11-11). Unity 확인 대기**(§3의 확인 목록). 과제 2 시작 전 |
| 툴 | `ked-motion-tool-characterRig` — 런타임과 같은 세션이 맡는다. 과제 3에서 맞춘다 |

## 0. 한 문장

> 이모지 리그의 틀을 스키마로 세우고(과제 1), 대본 한 줄로 이모지를 세우고(과제 2), 툴에서 만든 움직임을 그 위에서 재생한다(과제 3).

## 1. 일하는 법

- 과제는 소유자의 지시로 시작한다. 과제를 시작할 때 "정할 것"을 먼저 닫고 [emoji-rig.md](emoji-rig.md)에 적는다.
- 과제가 끝나면 **된 것 · 정한 것 · 아직 불편한 것 · 다음**을 보고하고, 이 문서의 상태를 고친다.
- 커밋은 소유자가 한다.
- Unity가 있어야 보이는 것은 과제마다 "Unity 확인 목록"으로 소유자에게 넘긴다. Unity 없이 도는 것(코어의 순수 계산 · 파서)은 테스트로 잰다.
- 이름의 대응: 소유자가 말한 `ISequenceCommandBase`는 이 저장소의 `ISequenceCommand`(구현의 바탕은 `CommandBase`), 스펙의 바탕은 `CommandSpecBase`, 공장은 `INodeCommandFactory`다.

## 2. 지금 있는 것과 모자란 것 (`50537b0e` 기준)

| | 지금 | 이모지에 모자란 것 |
|---|---|---|
| 리그 덤프 | `ExportedTuning/rig-schemas.json`에 `character`(노드 26) · `screenEffect`(노드 5) | 이모지 리그의 틀이 없다 |
| 모션 형식 | `motions/2` — 채널은 위치 · 배율 · 회전(`position` · `scale` · `euler`), 키의 시각은 초, 한 번 재생, `end: restore \| commit` | 불투명도 · 그림 바꾸기 채널이 없다. 되풀이와 구간이 없다. 리그 여러 개를 한 클립이 가리키는 길이 없다 |
| 모션 검증 | `MotionLibrary.Parse`가 채널 노드를 캐릭터 리그의 노드 목록으로 본다 | 이모지의 키는 캐릭터 리그 노드가 아니다 |
| 슬롯 | `StageReducer`가 `character` 리그만 세운다 | 이모지는 슬롯에 딸린 것이다 — 무대 상태에 어떻게 접는가 |
| 커맨드의 길 | 대본 → `CommandBridge` → `StageCommandParser`(표) → 스펙 → `CompositeCommandFactory` → 커맨드. 같은 줄을 코어의 `StageReducer`도 접는다 | 이모지의 스펙 · 공장 · 파서 항목 · 접기가 없다 |

## 3. 과제 1 — `EmojiRigSchema`와 `EmojiRigBuilder`

**끝나면**: 리그 이름 목록을 주면 [emoji-rig.md](emoji-rig.md) §3의 계층이 서고, 덤프에 이모지의 틀이 실려 툴이 받아 갈 수 있다.

본보기: `Stage/Character/Rig/CharacterRigDefinition.cs` · `CharacterRigBuilder.cs` · `Documentation~/rig-axis-order.md`, 화면 효과 리그(`Stage/ScreenEffect/ScreenEffectRigDefinition.cs` · `ScreenEffectRigBuilder.cs`).

| | 작업 | 끝나면 |
|---|---|---|
| 1-1 | `Stage/Emoji/EmojiRigDefinition.cs` — `EmojiRigSchema`: 노드(`Refs`) · 부모 · 무엇을 다는가(`NodeDef` — `EmojiRig_Image`에 Image와 CanvasGroup) · **한 번 서는 노드(`Emoji_Locator` · `Emoji_Offset`)와 리그마다 되풀이되는 노드(`EmojiRig_Place` · `EmojiRig_Image`)의 구분** | 계층의 주인이 코드 한 곳이다 |
| 1-2 | 세운 결과를 드는 것 — `EmojiRefs`(로케이터 · 오프셋 · 이름으로 찾는 리그들)와 `EmojiRigRefs`(리그 하나의 두 노드). 캐릭터 리그는 "필드 이름 = 하이어라키 이름"이지만 이모지는 리그 수가 정해져 있지 않아 리그는 이름으로 찾는다 — 리그 하나 안에서는 필드로 든다 | 커맨드가 노드를 묻지 않고 쓴다 |
| 1-3 | `Stage/Emoji/EmojiRigBuilder.cs` — ① 슬롯의 `CharSlot_Scale` 밑에 `Emoji_Locator`를 세운다(있으면 다시 쓴다) ② 로케이터 밑에 `Emoji_Offset`과, 받은 리그 이름마다 두 단을 세운다 ③ 부순다. 하이어라키 이름은 접두를 붙여 전역에서 하나뿐이게 한다(`rig-axis-order.md` §2.1의 `rolePrefix`) | 이름 목록 → 계층 |
| 1-4 | 덤프 — `PresentationTuningExporter`가 `rig-schemas.json`에 `emoji` 틀을 싣는다(리그 하나짜리 견본). 되풀이되는 노드라는 표시를 `RigSchemaDto`에 어떻게 적는지 정하고 `ExportedTuning/schema.md`에 적는다 | 툴이 틀을 복제 반입할 수 있다 |
| 1-5 | 테스트(EditMode) — 세운 계층이 스키마와 같다(노드 · 부모 · 컴포넌트) · 리그 이름이 겹치거나 비면 막는다 · 로케이터를 두 번 세우지 않는다 · 부수면 남는 것이 없다 · 덤프가 세운 것과 같다(`RigSchemaTreeUnityParityTests`의 본) | 테스트 통과 |
| 1-6 | 문서 — `Stage/Emoji/Documentation~/`에 이 리그의 까닭(자리와 뜻)을 옮겨 적고, `rig-axis-order.md` §1.3("스키마 방식이 다른 리그로 퍼졌다")에 한 줄 | 코드 곁에 까닭이 있다 |

**하지 않는 것**: 스프라이트 넣기 · 커맨드 · 모션 · 코어의 접기. `CharacterRigSchema`를 고치는 것.

**시작할 때 정할 것**([emoji-rig.md](emoji-rig.md) §8의 1~4):

| | 무엇 | 제안 |
|---|---|---|
| 1 | 노드 이름 | `Emoji_Locator` · `Emoji_Offset` · `EmojiRig_Place` · `EmojiRig_Image`. 하이어라키에서는 슬롯 · 이모지 · 리그 이름이 접두로 붙는다 |
| 2 | 로케이터의 수 | 슬롯에 하나 — RootSpace는 캐릭터와 바리에이션의 값이라 이모지마다 다를 까닭이 없다 |
| 3 | `EmojiRig_Image`의 크기와 피벗 | 스프라이트의 원본 픽셀 · 가운데(초상 · 배경이 원본 픽셀로 맞춘다) |
| 4 | `Emoji_Offset`의 값과 키 | 키를 오프셋 값에 더하는 것으로 읽는다(노드를 늘리지 않는다) |

**한 것(2026-11-11)** — Unity 없이 썼다. 컴파일과 테스트는 아래 목록으로 확인한다.

| | 파일 | |
|---|---|---|
| 1-1 · 1-2 | `Stage/Emoji/EmojiRigDefinition.cs` | `EmojiRigSchema`(노드 넷 · `Repeat` · `ReceivesKeys`) · `EmojiRigRefs` · `EmojiRefs` |
| 1-3 | `Stage/Emoji/EmojiRigBuilder.cs` | `EnsureLocator` · `BuildEmoji` · `DestroyEmoji` · `BuildTemplate`(덤프용 견본) |
| 1-4 | `Editor/PresentationTuningExporter.cs` · `Core/Tuning/RigSchemaDto.cs` · `ExportedTuning/schema.md` | 덤프에 `emoji` 틀, 노드에 `repeatsPerRig` |
| 1-5 | `Tests/EditMode/EmojiRigBuilderTests.cs`(12개) · `UnityParity/RigSchemaTreeUnityParityTests.cs`(`emoji` 경우) | |
| 1-6 | `Stage/Emoji/Documentation~/emoji-rig.md` · `rig-axis-order.md` §1.3 | |

정한 것: 시작할 때 정할 것 넷을 **제안대로** 세웠다([emoji-rig.md](emoji-rig.md) §8의 1~4 — 소유자 확인 대기). 덤프에는 "키를 받는 노드"를 싣지 않았다 — 과제 3의 형식에서 정한다.

**Unity 확인 목록**(아미야):

1. 컴파일 오류 0. 새 파일 셋(`EmojiRigDefinition.cs` · `EmojiRigBuilder.cs` · `EmojiRigBuilderTests.cs`)과 폴더 `Stage/Emoji`의 `.meta`가 있다(손으로 만든 GUID다 — Unity가 다시 만들었으면 그것을 쓴다).
2. 메뉴 `Ked / U12 / Export Presentation Tuning Dump` — `ExportedTuning/rig-schemas.json`에 `rigKind: "emoji"`가 실린다: 노드 넷(`__root` · `Emoji_Offset` · `EmojiRig_Place` · `EmojiRig_Image`), 뒤의 둘은 `repeatsPerRig: true`. `character` · `screenEffect`의 값은 바뀌지 않는다(노드마다 `repeatsPerRig: false`가 더해질 뿐).
3. EditMode 테스트 — `EmojiRigBuilderTests` 12개, `RigSchemaTreeUnityParityTests`(`emoji` 경우 포함). **2를 하기 전에는 대조 테스트가 "덤프에 rigKind 'emoji'가 없다"로 실패한다** — 낡은 덤프를 신고하는 것이다.
4. 예제 씬을 그대로 재생 — 이모지를 세우는 커맨드가 아직 없으니 전과 같다.

## 4. 과제 2 — 대본으로 이모지를 세운다

**끝나면**: `.yarn`의 한 줄로 재생 중에 이모지가 캐릭터 곁에 선다(아직 움직이지 않는다).

본보기: `Stage/Character/Commands/`(`CharacterRigCommandBase.cs` · `CharacterRigCommandFactory.cs` · `SetupCharRigCommand.cs`), `Stage/Grammar/StageCommandParser.CharRigSetup.cs` · `StageCommandParser.Table.cs`, `Playback/YarnBridge/CommandBridge.cs` · `CommandBridge.Forward.cs` · `CommandRoleGate.cs`, 코어 `Core/Reduce/StageReducer.Slot.cs`.

| | 작업 | 끝나면 |
|---|---|---|
| 2-1 | `Stage/Emoji/Commands/EmojiRigCommandBase.cs` — `EmojiRigCommandSpecBase : CommandSpecBase`(누구에게: `roleKey` · 무엇을: 이모지 이름) | 이모지 커맨드의 바탕 |
| 2-2 | 이모지 정의의 최소형 — 이름 · 오프셋 · 리그들(이름 · 스프라이트 · 기본 자리). 코어의 DTO로 두고, 손으로 쓴 견본 파일 하나를 읽는다. 과제 3에서 여기에 키가 붙는다 | 툴 없이도 과제 2가 돈다 |
| 2-3 | `SetEmojiCommandSpec` · `SetEmojiCommand : CommandBase` — 슬롯을 풀고(`CharacterRigResolver`), 정의를 찾고, 빌더로 세우고, 스프라이트 · 기본 자리 · 오프셋 · RootSpace를 넣는다 | 커맨드 하나가 이모지를 세운다 |
| 2-4 | `EmojiRigRegistry` — 어느 슬롯에 어느 이모지가 서 있는가. 슬롯이 부서지거나 다시 선언되면 딸린 이모지도 치운다 | 떠 있는 이모지를 찾고 치울 수 있다 |
| 2-5 | `EmojiRigCommandFactory : INodeCommandFactory` — `CompositeCommandFactory`에 끼운다(`SampleBootstrap`) | 스펙이 커맨드가 된다 |
| 2-6 | 대본 — `StageCommandParser.Emoji.cs` + 표의 항목, `CommandBridge`의 등록과 `Forward`, `CommandRoleGate`의 분기 | `.yarn`의 한 줄이 스펙이 된다 |
| 2-7 | 코어 — `StageReducer`가 이 커맨드를 접는다(슬롯에 딸린 이모지의 이름과 그 노드들). 접지 않기로 하면 `Unhandled`에 남긴다 — 조용히 두지 않는다 | 등가성 하네스가 이모지 줄에서 깨지지 않는다 |
| 2-8 | 데모 대본에 한 줄 + 테스트(파서 · 공장 · 접기) | Unity에서 재생 중 이모지가 선다 |

**하지 않는 것**: 움직임 · 구간 · 툴의 형식.

**시작할 때 정할 것**:
1. 대본의 모양 — `<<emoji 누구 이름>>`인가. 내리는 커맨드를 과제 2에 넣는가.
2. RootSpace 값의 집 — 캐릭터 + 바리에이션별 값을 어디에 두고 누가 재는가. 과제 2는 기본값 하나로 시작할 수 있다.
3. 좌우 — 캐릭터의 어느 쪽에 뜨는가를 누가 정하는가.
4. 건너뛰기 · 되감기 — 건너뛸 때도 세우는가(`SetupCharRigCommand`는 `ExecuteEvenIfSkipping`). 줄을 되감으면 치우는가.
5. 스프라이트를 가리키는 법 — 키와 그것을 푸는 곳(초상의 `PortraitResolver` 같은 것).
6. 인물의 페이드 · 퇴장과 이모지.

**Unity 확인 목록**(끝날 때 채운다).

## 5. 과제 3 — `EmojiMotionCommand`와 그 기반, 그리고 툴

**끝나면**: 툴에서 만든 이모지가 런타임에서 같은 모습으로 움직인다.

본보기: `Stage/Character/Commands/MotionCommandCharR.cs`, 코어 `Core/Motion/`(`MotionClip` · `MotionMath` · `MotionRules`), `Stage/Character/Motion/MotionLibrary.cs`, 툴과 맞추는 테스트(`ToolExportMotionParityTests`).

| | 작업 | 끝나면 |
|---|---|---|
| 3-1 | 형식 — 이모지 하나가 나가는 한 덩어리: 구성(2-2) + 타임라인(리그 이름 · 노드 · 채널 · 키) + 이름 붙은 구간 + 등장 · 머묾 · 퇴장에 이은 구간. 채널에 불투명도와 그림 바꾸기를 더한다. `motions/2`를 넓히는가, 이모지의 형식을 따로 두는가를 정한다 | 형식 문서 한 장 |
| 3-2 | 코어 — 그 형식을 읽어 "그 프레임의 모습"을 내는 순수 계산(`MotionMath` 재사용)과 검증 규칙 | 툴과 런타임이 같은 계산을 쓴다 |
| 3-3 | `EmojiMotionCommandSpec` · `EmojiMotionCommand` — 전체를 한 번 재생 / 구간을 이름으로 재생 / 머묾을 되풀이. 수명(줄 경계에서 정리되는가) · 건너뛰기 | 대본에서 이모지가 움직인다 |
| 3-4 | 툴(`ked-motion-tool-characterRig`) — 틀을 덤프에서 받고, 이모지를 구성하고(리그 추가 · 이름 · 스프라이트), 키를 잡고, 구간을 나누고, 3-1의 형식으로 내보낸다. 툴의 계획서(R3~R4)를 [emoji-rig.md](emoji-rig.md)에 맞춰 다시 쓴다 — R3-2(`f842ace`)는 버린 설계 위에 지은 것이다 | 툴에서 이모지를 만든다 |
| 3-5 | 맞추는 테스트 — 툴이 낸 파일을 런타임이 읽어 프레임마다 같은 값인지 | 툴 = 게임 |
| 3-6 | 그 뒤 — 툴의 사용감을 다듬는다(소유자) | |

**시작할 때 정할 것**:
1. 키의 시각 단위 — `motions/2`는 초다. 이모지의 구간은 프레임으로 적는다.
2. 그림 바꾸기 채널이 가리키는 것 — 스프라이트 키의 목록.
3. 머묾의 끝 — 내리는 커맨드가 오면 퇴장으로 넘어가는가, 머무는 시간을 이모지가 아는가.
4. 되감기 — 줄을 되감았을 때 이모지가 어느 구간의 어느 프레임인가.

**Unity 확인 목록**(끝날 때 채운다).

## 6. 진행

| 과제 | 상태 | 커밋 |
|---|---|---|
| 1 | 코드와 테스트 작성 — Unity 확인 대기 | |
| 2 | 시작 전 | |
| 3 | 시작 전 | |
