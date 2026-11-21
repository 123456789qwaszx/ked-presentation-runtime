# 명세 표와 줄 읽기 — 커맨드의 종류 · 인자 칸, 줄을 스텝으로 읽는 함수 (2026-11-22)

> 상태: **K1 · K2 코어 끝(Unity 밖 확인) — Unity 확인과 제안 셋의 확인이 남았다(§2 · §7)** · 브랜치 `feat/command-spec`(`dev` `b56a2ebc`에서)
> 설계: ked-story-tools `docs/PLAN.md` §4.1 · §4.2 · §6 → 넘겨받은 일: 그 저장소 `docs/handoff/runtime.md`
> 앞선 일: [PLAN_line-anchor.md](PLAN_line-anchor.md)(줄의 앵커 — §11이 미뤄 둔 "종류의 표와 도구가 묻는 창구"가 이 일이다) · [PLAN_rig-layers.md](PLAN_rig-layers.md)(어휘 83개)

## 0. 한 문장

> 연출을 고치는 툴이 yarn을 `LineId / 스텝 / 종류 / 순번`으로 읽을 수 있게, 런타임 코어가 **표 하나**(커맨드마다 종류와 인자 칸)와 **함수 하나**(줄 읽기)를 내준다. 파서와 재생은 바꾸지 않는다.

## 1. K1 — 명세 표

| | 자리 |
|---|---|
| 타입 | `Core/Spec/StageCommandSpec.cs` — `CommandKind`(일곱) · `ArgKind`(열) · `CommandArgSpec` · `StageCommandSpec` · `BoundArg` |
| 표 | `Core/Spec/StageCommandSpecTable.cs` — `Core`(이 저장소의 어휘) · `Default`(Core + 포크) · `TryGet` · `OfKind` · `IsImmediate` · `With` |
| 어휘 | `Core/Spec/StageCommandSpecTable.Core.cs` — 85줄(파서 표의 83 + 바로 실행하는 명령 2) |
| 포크의 자리 | `Core/Spec/StageCommandSpecTable.Fork.cs` — 공개 쪽은 비어 있다. 포크가 제 어휘를 여기에 적는다(공개 쪽에서 고치지 않는 파일이라 들일 때 부딪치지 않는다) |
| 내보내기 | `Core/Tuning/CommandSpecsFileDto.cs`(`command-specs/1`) → `ExportedTuning/command-specs.json`. 메뉴 `Ked/U12/Export Command Specs`(씬이 필요 없다) · 전체 덤프에도 든다 |

**한 줄의 모양** — 이름 · 종류 하나 · 한 줄 설명 · 기다리는 명령인가(`Waits`) · 바로 실행하는 명령인가(`Immediate`) · 인자 칸의 목록.
**칸의 모양** — 키 · 보이는 이름 · 갈래 · 생략값(파서 표의 글자 그대로, 없으면 null) · 적지 않아도 되는가 · 목록(이름 칸) · 낱말(토큰 칸) · 낱말 칸인가(`Flag`) · 덧붙일 말.

- 생략값이 "없다"(null — 꼭 적거나 핸들러가 제 값으로 간다)와 "비워 둔다"("" — 이징)를 가른다. JSON에서는 `hasDefault`가 가른다.
- `size`의 `keep_y`는 낱말 칸이다 — 그 낱말일 때만 자리를 차지하고, 아니면 뒤 칸(길이)이 당겨진다. `StageCommandSpec.Bind`가 대본의 인자를 칸에 맞춘다.
- 인자의 갈래는 계획서의 여덟에 둘을 더했다 — **수**(세기 · 불투명도처럼 단위 없는 값)와 **깊이 레벨**(`size`의 둘째 칸: 수 · 라벨 · x배율). 여덟 가운데 어느 것으로도 칸을 그릴 수 없어서다.
- 이름 칸의 목록 키: `character` · `pose` · `face` · `motion` · `emoji` · `shake` · `background` · `stage` · `layer` · `char_visual` · `stage_mask` · `cutin_panel` · `popup` · `screen_cover` · `screen_flash` · `screen_vignette` · `screen_noise` · `bgm` · `sfx` · `voice` · `surface_layout`.

### 1.1 파서 표와 맞추는 법

파서는 그대로 둔다(소유자 D4). `StageCommandParser.Table.cs`는 이름마다 람다라 인자 자리와 생략값이 데이터로 없다 — 그래서 EditMode 시험(`StageCommandSpecTableParserTests`)이 **표의 원문을 읽는다**: 항목마다 `c.Arg(자리[, 생략값])` · `Float(c, 자리, 생략값)`을 모아 명세 표와 댄다.

| 어긋남 | 시험 |
|---|---|
| 파서 표에 이름을 더했는데 명세 표에 없다(또는 그 반대) | `이름_집합이_같다` |
| 인자 수가 다르다 · 생략값이 다르다 · 한쪽에만 생략값이 있다 | `인자_수와_생략값이_같다` |
| 바로 실행하는 명령이 파서 표에 들어갔다 | `바로_실행하는_명령은_파서_표에_없다` |
| 생략값이 글자가 아니라 상수다 | 시험의 `Constants`에 그 상수를 더할 때까지 실패한다 |
| 자리가 숫자가 아니다(`size` — `tail`) | 손으로 맞춘 항목이다. 그 항목의 원문을 시험에 적어 두었고, 원문이 바뀌면 실패해 다시 맞추게 한다 |

새 커맨드를 더하는 차례는 [command-authoring.md](command-authoring.md) §4에 한 줄 더했다.

### 1.2 종류 배정

계획서 §4.1의 초안 그대로다 — 카메라 9 · 자리와 구도 27 · 연기 9 · 화면 25 · 세우기 6(+2) · 소리 6 · 시간 1. 리그와 어긋나게 둔 것은 `StageCommandSpecTable.Core.cs`의 머리에 적었다.

## 2. 제안 — 확인 받을 것 (소유자)

| # | 물음 | 제안 | 까닭 | 다른 길 |
|---|---|---|---|---|
| C1 | `portrait` · `portrait_close`의 종류 | **세우기**(초안대로) | 촬영소에 리그를 세우고 캐스팅하는 일이다 — `slot` + `cast`와 같은 결이고, 줄의 앵커 Q2가 장면 세우기로 꼽았다. 초상의 표정 · 몸짓은 슬롯 키 `portrait`에 `face` · `motion`으로 쓰므로 연기 필터에는 그쪽이 오른다 | 화면(대사창의 그림이다) — 편 중간에 인물을 자주 바꾼다면 필터에 올라야 한다 |
| C2 | `depth_focus_on` · `depth_focus_clear`의 종류 | **화면**(초안대로) | 슬롯을 인자로 받지만 바뀌는 것은 그 사람이 아니라 나머지 무대 전체의 흐림이다. `stage_blur`와 같은 블러 계통이라 한 필터에서 함께 보여야 한다 | 카메라(초점 · 피사계 심도는 렌즈의 일이다) · 자리와 구도(`char_visual_defocus`와 쓰임이 겹친다) |
| C3 | 바로 실행하는 명령(`surface_layout` · `surface_reset`)의 표기 | **명세 표에 넣고 `Immediate`로 가린다. 종류는 세우기** | 모든 커맨드가 정확히 한 종류를 갖는다(P1). 툴은 `Immediate`를 보고 `<<at>>` 없이 적고, 줄 읽기는 앵커에 넣지 않고 따로 싣는다(`LineRead.Immediate`). N5의 "at 없는 명령은 오류"도 `IsImmediate`로 가린다. 포크의 `messenger_open` · `messenger_close`는 `.Fork.cs`에 `immediate: true`로 적는다 | 표 밖에 이름 목록만 둔다 — 종류 · 인자 칸이 없어 툴이 보여 줄 수 없다 |
| C3b | `at` · `gap` · `step` | **표 밖**(계획서대로) — JSON에는 `lineStructure: [at, gap]` · `legacyStructure: [step]`로 이름만 낸다 | 커맨드가 아니라 줄의 구조다 | — |

C1 · C2는 표의 한 줄씩이다 — 바꾸면 `StageCommandSpecTable.Core.cs`의 그 줄을 옮기고 JSON을 다시 낸다.

## 3. K1에서 알게 된 것

- **`size_reset`의 길이는 시간 토큰이 아니다.** 파서 표가 `Float(c, 1, 0f)`로 읽는다 — `13fr` · `0.4s`를 적으면 경고하고 0으로 간다. 명세 표에는 길이 칸으로 적고 덧붙일 말에 남겼다. 고치려면 파서를 고쳐야 한다(이번 범위 밖).
- **`pause`의 생략값은 파서 표에 없다.** 핸들러가 0.18초로 간다. 줄 읽기가 같은 값을 쓰고(`YarnLineReader.DefaultPauseSeconds`), EditMode 시험이 파서와 맞춰 본다.
- `char_scale_to` · `bg_scale`의 배율 생략값은 0이다(파서 표 그대로 적었다) — 생략하면 0배가 된다.
- 브리지의 Yarn 등록(`CommandBridge*.cs`)은 시험이 닿지 않는다(Playback은 EditMode 시험이 참조하지 않는다). 바로 실행하는 명령의 목록은 브리지의 등록과 손으로 맞춘 것이다.

## 4. K2 — 줄 읽기

| | 자리 |
|---|---|
| 읽는 함수 | `Core/Read/YarnLineReader.cs` — `ReadFile(원문)` → `YarnFileRead`(줄마다 하나 · `TryGet(LineId)`) · `Read(원문, LineId)` |
| 결과 | `Core/Read/LineRead.cs` — `LineRead` · `LineReadStep` · `LineReadCommand` · `SourceSpan` |
| 시간표 | `Core/Read/LineTimetable.cs` — `LineTimetable.Build(줄, 타자기, 길이를 묻는 함수)` · `TypewriterPacing` |

```
줄(LineRead)       LineId · 노드 · 화자 · 글 · 대사 줄의 줄 번호 · 들여쓰기 · 묶음의 자리 · 문제
 ├ 스텝            앵커 이름 · 갈래(앞 / start / 글 / end) · 대사 조각 · 간격 · 기다림의 합 · 앵커의 길이
 │   └ 커맨드      이름 · 인자 · 명세 표의 칸(Bound) · 종류 · 지연 · 길이 · 기다리는가 · 줄 번호 · 종류 안의 순번
 └ 바로 실행한 것  앵커 밖 — 명세 표의 Immediate와 Yarn의 set · declare · call · wait
```

- **추출기를 넓히지 않고 따로 두었다.** `YarnCommandTextExtractor`는 등가성 하네스 전용이다 — 재생이 만나는 순서 그대로의 커맨드 열이 필요해 분기가 있는 파일을 믿지 못한다. 줄 읽기는 순서가 필요 없고 원문의 자리가 필요하다. 글을 읽는 손(커맨드 · 표지 · 화자 · 태그)과 앵커의 차례(`LineBeatSchedule.BuildAnchors`)는 같은 함수를 쓴다 — 추출기의 `StripComment` · `StripTags`를 어셈블리 안에 열었다.
- **분기가 있는 파일** — 대사 줄 바로 위의 묶음만 읽는다. `if` · `elseif` · `else` · `endif` · `jump` · `detour` · `return` · `stop` · `once` · `endonce`와 선택지(`->`)는 묶음을 끊는다. 그 위에 남은 `at` · `gap`은 어느 줄의 것인지 원문으로 알 수 없으므로 파일의 문제(`YarnFileRead.Problems`)로 적는다 — 원문 검사의 오류다(줄의 앵커 AR10). 선택지의 글은 대사 줄이 아니다.
- **지연** — 같은 앵커에서 앞선 기다리는 명령(`pause` · `screen_cover`)의 길이의 합이다. `pause`는 걸러지지 않고 목록에 남으며(종류 "시간"), 종류로 걸러 보아도 뒤 커맨드의 지연으로 남는다. 기다리는 명령은 `Waits` · `Hold`로 표시한다.
- **앵커의 길이**(`LineReadStep.Length`) — 간격과 기다림의 합 가운데 큰 쪽이다. 간격은 앵커가 열린 때부터 잰다(넘겨받은 문서 6절).
- **원문에서의 위치** — `LineRead.Block`이 그 줄의 커맨드 묶음의 범위다(줄 번호와 글자 자리 — 받은 글 그대로의 자리라 CRLF에서도 맞다). 대사 줄은 들지 않는다. 묶음이 없으면 빈 범위이고 대사 줄의 머리를 가리킨다. 커맨드 · 간격마다 줄 번호가 있다.
- **옛 표기** — `<<step>>`은 앞 앵커(`~0` · `~1` …)의 경계, `at` 없는 명령은 `start`(`Bare`). 줄마다 `LegacySteps` · `BareCommands`로 센다. 문제로 올리지는 않는다(N5에서 오류).
- **바로 실행하는 명령** — `at` 없이 쓴 것이 옛 표기가 아니다. 앵커에 넣지 않고 `LineRead.Immediate`에 싣는다. `<<at 이름 surface_layout …>>`처럼 `at`을 붙이면 줄의 문제로 적는다.
- **표에 없는 명령** — 종류 없이 싣고 줄의 문제로 적는다. 포크의 대본은 포크의 표(`Default` 또는 JSON을 되읽은 표)로 읽는다.
- 주소의 마지막 칸은 `LineReadCommand.KindIndex`다 — 그 앵커의 같은 종류 안에서 몇 번째인가.

### 4.1 프레임 시간표

보통 재생의 기준 시간표를 내는 순수 함수다. 저장하지 않고, 재생은 이것을 쓰지 않는다.

| 앵커 | 열리는 때 |
|---|---|
| 앞 앵커 | 앞의 앞 앵커가 열린 때 + 그 길이 |
| `start` | 앞 앵커가 모두 끝난 뒤. 타자기는 `StartDelay` 뒤에 시작한다 |
| 글 표지 | 타자기가 그 글자에 닿은 때. 앞 앵커의 길이가 차지 않았으면 그때까지 타자기가 선다(뒤의 글도 그만큼 밀린다) |
| `end` | 타자기가 끝난 때 |
| 넘김(`Ready`) | `end`가 열린 때 + `end`의 길이 |

- 커맨드는 앵커가 열린 때 + 지연에 시작한다. 길이는 대본의 길이 칸(없으면 표의 생략값)이고, 대본에 없는 길이(모션 · 이모지의 클립)는 `lengthOf`로 묻는다 — 답이 없으면 모르는 길이(`Duration` null)다.
- 글 표지 · `end`의 때는 어림이다(`Estimated`). 글이 없는 구간(앞 앵커 · `start`)만 정확하다.
- `TypewriterPacing`은 재생의 타자기(`EllipsisBreathTypewriter`)와 같은 식 · 같은 기본값이다 — 초당 글자 수 · 숨의 배수 · 줄 머리의 느려짐 · 상한. 흔들림(jitter)은 없다. 속도는 프로젝트의 값이라(예제 씬은 초당 20자) 툴이 넣는다.

**어림이 어긋나는 곳**

- 글자를 TMP가 아니라 글에서 센다 — 리치 텍스트 태그 · 다른 Yarn 마크업이 든 줄.
- 타자기의 식이 두 곳에 있다(Playback의 `EllipsisBreathTypewriter`와 코어의 `TypewriterPacing`). 재생을 건드리지 않으려고 옮겨 적었다 — 타자기가 코어의 식을 부르게 하면 한 곳이 된다(재생이 바뀌므로 Unity 확인이 필요하다. 하지 않았다).
- 대사창이 열리는 시간(`StartDelay`)은 0으로 두었다 — 재생에서 재서 넣는다.

## 5. 확인 (Unity 밖)

- 코어 시험 — 코어와 순수 시험을 .NET 8로 빌드해 Unity의 NUnit(3.5)으로 돌렸다. 기준(`dev`) 567 → K1 585 → K2 **610, 모두 통과.**
- 파서 대조 시험 — `Library/ScriptAssemblies`의 `Ked.Presentation.Stage.dll`(실제 무대 파서)에 대고 돌렸다. 6개 통과. 일부러 어긋나게 한 여섯(명세 표의 생략값 · 파서 표에 새 이름 · 인자 추가 · `size` 원문 · `Float` 생략값 · 상수 아닌 생략값)에서 모두 실패하는 것을 보았다.
- 코어는 `netstandard2.1` · C# 9로 따로 빌드해 보았다(Unity의 프로필). 익스포터는 Unity 어셈블리에 대고 컴파일만 했다.
- 예제 대본 15편 · 225줄을 읽었다 — 선형인 편은 추출기와 같은 커맨드 열이다. 분기가 있는 5편도 문제 없이 읽는다. `real_test_Mirage_Tavern.yarn`은 걷힌 어휘(`place_left` · `12fr` …)를 쓴다 — 줄의 문제 34건으로 나온다.
- `ExportedTuning/command-specs.json`은 같은 DTO를 Unity 밖에서 낸 것이다(JsonUtility의 꼴에 맞췄다).

## 6. 바뀐 곳

| | |
|---|---|
| 새 파일 | `Core/Spec/` 넷 · `Core/Read/` 셋 · `Core/Tuning/CommandSpecsFileDto.cs` · 시험 넷(`StageCommandSpecTableTests` · `StageCommandSpecTableParserTests` · `YarnLineReaderTests` · `LineTimetableTests`) · `ExportedTuning/command-specs.json` |
| 고친 파일 | `Core/Reduce/YarnCommandTextExtractor.cs`(`StripComment` · `StripTags`를 `internal`로 — 동작은 그대로) · `Editor/PresentationTuningExporter.cs`(명세 표 내보내기) |
| 그대로 | 파서 · 브리지 · 재생 · 실행기 · 폴드 |

코어가 바뀐다 — 코어 사본을 쓰는 곳(ked-story-tools · 작업공간 포크 · 모션 툴 · 추출기 · compositor의 오라클)은 다음에 들일 때 `Core/Spec` · `Core/Read`가 따라온다. 포크는 `StageCommandSpecTable.Fork.cs`에 제 어휘를 적어야 EditMode 시험이 지나간다.

## 7. Unity에서 볼 것 (소유자)

1. 컴파일 · EditMode 시험 — 새 시험 넷. `StageCommandSpecTableParserTests`가 파서 표의 원문을 찾는가(시험 파일의 자리에서 `../../Stage/Grammar/StageCommandParser.Table.cs`).
2. 메뉴 `Ked/U12/Export Command Specs` — `ExportedTuning/command-specs.json`이 다시 나오고, 지금 든 파일과 내용이 같은가(공백 · 줄바꿈만 다르면 다시 낸 것을 쓴다).
3. 재생은 볼 것이 없다 — 재생 경로를 고치지 않았다.

## 8. 남은 것

- §2의 확인(C1 – C3).
- 타자기의 식을 한 곳으로(§4.1) — 할지 말지.
- K5(도구 연결) · N5(옛 표기 걷기 — `at` 없는 명령의 오류는 `StageCommandSpecTable.IsImmediate`로 가린다) — 넘겨받은 문서 4절 · 5절.
