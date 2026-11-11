# PLAN — 이모지 (2026-11-11)

| | |
|---|---|
| 브랜치 | `ked-presentation-runtime` · `feat/emojiRigSchema`(`dev` @ `50537b0e`에서 갈랐다) |
| 정의 | [emoji-rig.md](emoji-rig.md) — 이모지 리그가 무엇인가. 이 문서는 **무엇을 어떤 순서로 만드는가**만 적는다 |
| 결정 | 아미야(소유자) 2026-11-11: 과제 셋으로 나눈다. 과제마다 소유자의 지시로 시작한다 |
| 상태 | 과제 1 끝(`4b2478a6`) · 과제 2 끝(`bf570555`). **과제 3 — 형식과 런타임의 재생을 썼다(2026-11-11). Unity 확인 대기**(§5의 확인 목록). 툴은 그다음 |
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

**정한 것(소유자, 2026-11-11)**

| | 무엇 | 결정 |
|---|---|---|
| 1 | 대본의 모양 | `<<emoji 누구 이름>>` · `<<emoji_off 누구 이름>>`. `emoji_off`에 이름이 없으면 그 슬롯의 이모지 전부 |
| 2 | RootSpace 값의 집 | 기본값 하나로 시작한다 — 조립 루트의 칸(`emojiDefaultRootSpace`, 발에서 잰 px). 캐릭터 + 바리에이션별 표는 뒤에 붙인다 |
| 3 | 좌우 | 과제 2에서 다루지 않는다 — RootSpace 표를 만들 때 함께 정한다 |
| 4 | 건너뛰기 · 되감기 | 건너뛸 때도 세우고 내린다(`ExecuteEvenIfSkipping`). 되감기는 무대를 비우고 다시 달리므로 따로 할 일이 없다 — 이모지는 슬롯의 리그 밑에 살아 함께 부서진다 |
| 5 | 스프라이트 | `Resources/Emojis/{키}`(그림 팝업과 같은 해석기). 못 찾으면 이미지를 끈다 |
| 6 | 인물의 페이드 · 퇴장 | 묶지 않는다. 이모지는 `emoji_off`와 슬롯의 재선언으로만 사라진다 |

**한 것(2026-11-11)** — Unity 없이 썼다. 코어(정의 · 폴드)는 저장소 밖에서 따로 컴파일해 폴드 규칙을 돌려 봤다(통과). Unity 쪽은 아래 목록으로 확인한다.

| | 파일 | |
|---|---|---|
| 2-1 | `Stage/Emoji/Commands/EmojiRigCommandBase.cs` | `EmojiRigCommandSpecBase`(대상 · 이모지 이름) · `EmojiRigTargets`(슬롯과 접두 풀기) |
| 2-2 | `Core/Emoji/EmojiDefinition.cs` · `Core/Tuning/EmojisFileDto.cs` · `Stage/Emoji/EmojiLibrary.cs` · `Assets/Dialogue/emojis.json` | 정의의 최소형(`emojis/1`) — 이름 · 오프셋 · 리그들(이름 · 스프라이트 · 기본 자리). 규칙(`EmojiRules`)과 읽기(`EmojiDefinitionLoader`)는 코어에 있다 — 툴이 같은 것을 쓴다 |
| 2-3 | `Stage/Emoji/Commands/SetEmojiCommand.cs` · `ClearEmojiCommand.cs` | 세우기 · 내리기 |
| 2-4 | `Stage/Emoji/EmojiRigRegistry.cs` · `Stage/PresentationStage.cs` | 장부(`stage.emojis`). 슬롯이 부서진 것을 통보받지 않는다 — 읽고 쓸 때 "지금 그 슬롯 밑에 선 것"만 남긴다. 캐릭터 쪽 코드는 고치지 않았다 |
| 2-5 | `Stage/Emoji/Commands/EmojiRigCommandFactory.cs` · `SampleBootstrap`(칸 둘: `emojisJson` · `emojiDefaultRootSpace`) | |
| 2-6 | `Stage/Grammar/StageCommandParser.Emoji.cs` · 표 · `CommandBridge`(`BindEmoji` · `Forward`) · `CommandRoleGate` | |
| 2-7 | `Core/Reduce/StageReducer.Emoji.cs` · `StageState`(이모지 축) · `StageReducerTuning.Emojis` · 하네스 | **슬롯마다 서 있는 이모지의 이름만 접는다**(컷인 · 그림 팝업처럼 끝 상태의 기록이다). 노드는 리그 노드 트리에 넣지 않았다 — 트리에는 노드를 빼는 길이 없고(`emoji_off`), 지금 이모지는 정의의 값 그대로 서 있을 뿐이다. 움직임의 끝값은 과제 3에서 접는다. 하네스의 캡처는 캐릭터 스키마의 노드만 읽으므로 이모지 노드 때문에 어긋나지 않는다 |
| 2-8 | 데모 대본(`presentation_demo_Demo_Presentation.yarn`의 끝 — 줄 넷) · `Resources/Emojis/star` · `question` · `Tests/EditMode/EmojiCommandTests.cs`(14개) | 견본 그림은 `Samples/Content/Art/Emoji`의 둘을 복사했다(256×256 — 크다. 크기를 줄이는 것은 과제 3의 배율 키다) |

**Unity 확인 목록**(아미야):

1. 컴파일 오류 0. 새 파일의 `.meta`는 Unity가 만든다(견본 그림 둘의 `.meta`는 원본의 임포트 설정을 복사하고 GUID만 바꿨다).
2. EditMode 테스트 — `EmojiCommandTests` 14개, 그리고 기존 것이 그대로 통과한다.
3. 예제 씬을 재생해 데모 대본의 끝(그림 팝업 다음)까지 간다:
   - `<<emoji s1 star>>` — 하린의 머리 곁에 별이 선다. 하이어라키: `s1_CharSlot_Scale › s1_Emoji_Locator › s1_star_Emoji_Offset › s1_star_body_EmojiRig_Place › …_EmojiRig_Image`.
   - `<<emoji s2 question>>` · `<<emoji s1 twinkle>>` — 시구레에게 물음표, 하린에게 별 셋이 더 선다(로케이터는 슬롯에 하나).
   - `<<emoji_off s1 star>>` — 별 하나만 내려간다. `<<emoji_off s1>>` · `<<emoji_off s2>>` — 전부 내려간다.
   - **자리가 어색하면** `[VNAppBootstrap] › 작가 자산 › Emoji Default Root Space`(기본 0, 800)와 `Assets/Dialogue/emojis.json`의 `offset`을 고친다.
4. 그 구간에서 건너뛰기와 되감기(R) — 되감은 줄의 이모지 상태가 맞는가(세운 뒤의 줄로 돌아가면 서 있고, 내린 뒤의 줄이면 없다).
5. 등가성 하네스를 켜고 재생 — 이모지 줄이 Unhandled로 남지 않는다.

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

**정한 것(소유자, 2026-11-11)**

| | 무엇 | 결정 |
|---|---|---|
| 1 | 형식의 집 | `emojis.json`을 넓힌다(`emojis/2`) — 이모지 전용. `motions.json`에 얹지 않는다 |
| 2 | 시각 | 프레임. 초당 프레임 수는 파일에 한 번 |
| 3 | 그림 바꾸기 | 계단 키. 수치 채널에도 계단 키(`step`)를 둔다 |
| 4 | 구간 | **따로 선 클립이다** — 0~12fr 구간은 13프레임짜리 애니메이션, 8~14fr 구간은 7프레임짜리 애니메이션. 타임라인을 가리키는 창이 아니다 |
| 5 | 종류 | **머묾이 있는가가 정한다** — 없으면 한 번 재생하고 스스로 사라진다, 있으면 등장 → 머묾을 되풀이 → 내리면 퇴장. 한 프레임짜리 머묾 = 멈춰서 남는다 |
| 6 | 줄을 넘을 때 | 재생은 끝까지 간다(캐릭터 모션은 줄 경계에서 끝값으로 확정된다 — 이모지는 다르다) |
| 7 | 등장 도중에 내리면 | 바로 퇴장으로 넘어간다 |
| 8 | 되감기 | 따로 처리하지 않는다 — 건너뛰면 끝난 뒤의 모습만 남기고(머무는 것은 머묾의 처음, 아닌 것은 없다), 되감은 줄의 커맨드는 실제로 다시 재생된다. 캐릭터와 같은 길이다 |
| 9 | 세우기와 재생 | `<<emoji>>`가 세우면서 바로 재생한다 |

**한 것(2026-11-11)** — 3-1 · 3-2 · 3-3. 코어는 저장소 밖에서 따로 컴파일해 규칙 · 샘플 · 재생 단계 · 접기를 돌려 봤고(통과), 견본 `emojis.json`을 같은 로더로 읽어 재생을 돌려 봤다. Unity 쪽은 아래 목록으로 확인한다.

| | 파일 | |
|---|---|---|
| 3-1 | [emoji-format.md](emoji-format.md) · `Core/Tuning/EmojisFileDto.cs` | 형식 `emojis/2`와 로더. 수치 키 `f v i o step`, 그림 키 `f s` |
| 3-2 | `Core/Emoji/EmojiClip.cs` · `EmojiDefinition.cs`(규칙) · `EmojiPlayback.cs` | 그 프레임의 모습(`EmojiClipMath`) · 종류(`Stays`) · 재생의 단계(순수한 상태 기계) |
| 3-3 | `Stage/Emoji/EmojiPlaybackSystem.cs` · `SetEmojiCommand` · `ClearEmojiCommand` · `SampleBootstrap`(`Update`에서 `Tick`) | 재생은 커맨드가 아니라 시스템이 든다(줄을 넘는다). 커맨드는 세워서 맡기고(`Play`) 내리라고만 한다(`Off`) |
| | `Stage/Emoji/EmojiRigDefinition.cs` | **틀이 바뀌었다** — `Emoji_Offset`에 `CanvasGroup`(이모지 전체의 불투명도). 덤프를 다시 낸다 |
| | `Core/Reduce/StageReducer.Emoji.cs` | 접으면 머무는 이모지만 남는다 |
| | `Assets/Dialogue/emojis.json` · 데모 대본 | 견본 셋: `star`(머문다 — 등장 5 · 머묾 16 · 퇴장 6) · `question`(한 번, 22프레임) · `twinkle`(리그 셋, 한 번, 24프레임) |
| | `Tests/EditMode/EmojiPlaybackTests.cs`(17개) | |

**`EmojiMotionCommand`는 따로 두지 않았다** — 세우는 커맨드가 바로 재생한다(정한 것 9). 구간을 이름으로 골라 재생하는 커맨드가 필요해지면 그때 얹는다.

**Unity 확인 목록**(아미야):

1. 컴파일 오류 0. 새 파일의 `.meta`는 Unity가 만든다.
2. 메뉴 `Ked / U12 / Export Presentation Tuning Dump` — `rig-schemas.json`의 `emoji › Emoji_Offset`이 `hasCanvasGroup: true`가 된다. (안 하면 `RigSchemaTreeUnityParityTests`는 그대로 통과한다 — 노드 수와 자리는 같다. 그래도 덤프는 실물과 같아야 한다.)
3. EditMode 테스트 — `EmojiPlaybackTests` 17개, `EmojiCommandTests` 14개, `EmojiRigBuilderTests` 12개, 그리고 기존 것.
4. 예제 씬을 재생해 데모 대본의 끝(그림 팝업 다음)까지 간다:
   - `<<emoji s1 star>>` — 별이 튀어나와(크게 → 제 크기) 머무는 동안 작게 두근거린다.
   - `<<emoji s2 question>>` — 물음표가 기울어 나와 까딱이다가 위로 흐르며 사라진다(1초쯤). 내리지 않아도 없어진다.
   - `<<emoji s1 twinkle>>` — 별 셋이 차례로 떴다가 사라진다.
   - `<<emoji_off s1 star>>` — 별이 줄어들며 흐려져 사라진다.
5. 재생 도중에 다음 줄로 넘긴다 — 물음표가 끝까지 재생된다.
6. 건너뛰기 · 되감기(R) — 별이 서 있는 줄로 되감으면 별이 서 있고(머묾), 물음표의 줄로 되감으면 물음표가 다시 재생된다. 그 뒤의 줄로 되감으면 물음표는 없다.
7. 등가성 하네스 — 이모지 줄이 Unhandled로 남지 않는다.

**남은 것**: 3-4(툴이 틀을 받고 · 이모지를 구성하고 · 키를 잡고 · 이 형식으로 내보낸다) · 3-5(툴이 낸 파일과 런타임의 대조) · 3-6(툴의 사용감).

## 6. 진행

| 과제 | 상태 | 커밋 |
|---|---|---|
| 1 | 끝 — 소유자가 Unity에서 확인하고 덤프를 다시 냈다 | `4b2478a6` |
| 2 | 끝 — 소유자가 Unity에서 확인했다 | `bf570555` |
| 3 | 3-1 · 3-2 · 3-3(형식 · 코어 · 런타임의 재생) 작성 — Unity 확인 대기. 3-4 · 3-5(툴 · 대조)는 그다음 | |
