# PLAN — 예제 UI를 UIPresentationFlow 위에 다시 세운다

2026-10-26. `PLAN_framework-scope` 6·7단계(문서·검증)보다 먼저 한다(아미야 결정).
작업 브랜치: `feat/uipf-ui` ← `refactor/framework-scope` @ `7ed5c1a5`.

## 목표 한 문장

> ked의 예제 UI 전체를 UIPresentationFlow(UIPF) 위에서 코드 빌더로 다시 세운다. 화면은 UIPF의 구조(Refs · Presentation · 바인딩)로 만들어지고, 그림은 이름 규칙대로 폴더에 넣으면 채워진다. 본체(`Assets/Ked.Presentation/`) diff 0.

보여 줄 것 — **AI가 UIPF 구조로 화면을 만들어 낸다.**
화면 정의(빌드 명세) → 빌더가 프리팹·Refs 계약·바인딩을 만든다 → 그림을 넣으면 테마가 선다 → 대본 한 줄(`<<ui_patch dark>>`)로 테마가 바뀐다.

## 정한 것

| # | 항목 | 결정 |
|---|---|---|
| U1 | 원본 | 로컬 UIPF `dev` @ `cf3fa98`(= origin/dev). 코드는 main `ff3f281`과 같고 README·GIF만 더해졌다 |
| U2 | 반입 | **복사.** UIPF README가 적은 복사 목록 그대로 — `Scripts/{UI,Presentation,Display,Localization}` · `Editor/UI` · `Shaders` → `Assets/UIPresentationFlow/`. `Scripts/Demo` · `Demo/` · `UI/`(데모 에셋)는 들이지 않는다. 원본 커밋과 대조 명령은 `Assets/UIPresentationFlow/Documentation~/vendoring.md`. 진행 코어와 같은 규율: 사본만 고치지 않는다 |
| U3 | 화면 | **코드 빌더로 새로 세운다.** 지금 씬의 UI 계층과 `Samples/Scripts/UI`는 교체 단계(U3)에서 걷는다. 지금 예제의 UI 그림도 전부 교체한다 |
| U4 | 기능 | **실제로 동작하는 것만.** 설정은 이미 있는 값에 잇고, 동작 없는 버튼은 두지 않는다 |
| U5 | 순서 | UIPF 반입을 `PLAN_framework-scope` 6·7단계보다 먼저 한다 |

## 경계 — 무엇이 바뀌고 무엇이 안 바뀌나

- **본체 diff 0.** `Ked.Presentation.*` asmdef 넷은 손대지 않는다. UI가 본체에 주는 것은 지금의 계약 7개 그대로다:
  `IStageMaskProvider` · `IShotResponseStageProvider` · `IStageDepthContentSlotProvider` · `IPresentationDialogueBoxView` · `IPresentationOptionsBoxView` · `IUIThemePatchPort` · `IPresentationView`.
- **무대는 그대로.** `PresentationUIRoot`의 무대 쪽(Refs 73개, 무대 계약 셋 구현)은 기존 씬 오브젝트를 둔다. 퀵메뉴·대사창·선택지(HUD)만 새 `PlayScreen`으로 옮긴다. 결과적으로 `PresentationUIRoot`는 "무대"로 좁아진다.
- **대사창 소유권.** 줄·이름 텍스트의 위치·크기·글자 크기는 연출 DB(`DialogueSurfaceLayoutPresetDBSO`, 대본의 `surface_layout`)가, 그림·색·글꼴은 UIPF 테마가 정한다. 프리셋의 `overrideImage`는 끄고, 대사창 텍스트는 Text Binding에서 `fixedSize`로 둔다.
- **Unity 버전.** UIPF는 6000.6.2f1(ugui 2.6.0), ked는 6000.3.16f1(ugui 2.0.0). 코드만 들이고 에셋(Spec · FontSet · Palette · SpriteSet · 바인딩)은 ked에서 새로 만든다 — 상위 버전에서 직렬화된 에셋을 내리지 않는다. 코드에서 6000.4 이상 전용 API는 grep으로 찾지 못했다. 컴파일은 U0의 Unity 확인 항목이다.

## 코드 위치

2026-10-27 받은 UI 패키지(Ked_UI_Handoff_v1)가 `Assets/Samples/KedUI`를 정했다 — 처음 초안의 `Samples/UI`가 아니라 여기다.

```
Assets/
├─ Ked.Presentation/       본체 — 손대지 않음
├─ Ked.Progression/        진행 코어 사본 — 손대지 않음
├─ UIPresentationFlow/     UIPF 사본 (asmdef UIPresentationFlow.Runtime · UIPresentationFlow.Editor)
│   └─ Documentation~/vendoring.md
└─ Samples/
    ├─ KedUI/              새 예제 UI
    │   ├─ Runtime/        asmdef Ked.Samples.UI — 화면(Screens) · 흐름(Flow) · 부품(Kit) · 설정(Settings) · KedUIHost
    │   ├─ Editor/         asmdef Ked.Samples.UI.Editor — 빌더(Build/ · Build/Specs/)와 패키지의 KedUIArtImporter
    │   ├─ Art/            패키지 PNG 49장(.meta는 Unity가 만든다)
    │   ├─ Fonts/          Noto Sans KR Medium · Bold(가변 글꼴에서 뽑은 정적 굵기, OFL)
    │   ├─ Texts/          UI 글자 표 KedUITexts.csv(ko-KR · en-US)
    │   ├─ Generated/      빌더가 만든다 — Screens/*.prefab · KedOptionItem · 글꼴 SDF · Presentations · 프리셋 DB
    │   └─ Resources/KedUI/KedUICanvas.prefab   빌더가 만든다 — SampleBootstrap이 읽는다
    └─ Scripts/
        ├─ Game/KedUI/     호스트 어댑터(기본 어셈블리) — KedHostPorts · LegacyStageForKedUI
        └─ UI/             옛 UI — U3에서 지운다
```

새 UI를 별도 asmdef에 두는 이유: 옛 UI와 UIPF는 같은 전역 타입 이름 20개를 쓴다(`UIBase` · `UIManager` · `UIRoot` …). 새 코드가 UIPF만 보는 어셈블리에 있으면 교체 전까지 둘이 섞이지 않는다.

바인딩 에셋은 UIPF 위저드의 고정 경로(`Assets/UI/ImageBindings`)를 쓰지 않고 빌더가 `Samples/UI/ImageBindings/` 아래에 직접 만든다. 폴더 동기화(`Sync Selected Theme Folder`)는 바인딩 에셋이 있는 폴더를 보므로 경로에 묶이지 않는다 — UIPF 원본을 고칠 필요가 없다.

## 화면 — 아미야의 표를 UIPF에 놓으면

| 순서 | 화면 | UIPF 종류 · 클래스 | 이미 있는 동작 | 새로 잇는 것 |
|---|---|---|---|---|
| 1 | 대화 화면 | Root `PlayScreen`의 일부 | `IPresentationDialogueBoxView` 계약, 타자기 | 다음 진행 마커(줄 표시 완료에 연결) |
| 2 | 재생 퀵메뉴 | `PlayScreen`의 일부 | 자동 · 빨리 넘기기 · 롤백 · 백로그(`VNFeatureController` · `IPlaybackHost`), 활성 표시(오토·스킵 상태) | UI 숨김(HUD 감춤 → 클릭·키로 복귀), 메뉴 → 시스템 메뉴 |
| 3 | 선택지 | `PlayScreen`의 일부(`Option_Template`) | `IPresentationOptionsBoxView`, 선택 불가(`IsAvailable`, 진행 예제의 잠긴 이유) | 잠금 아이콘 |
| 4 | 백로그 | Panel `BacklogPanel` | 기록 · 줄로 이동(롤백 / 완료 장면 갈라지기) | 현재 위치 표시 |
| 5 | 시스템 메뉴 | Panel `SystemMenuPanel`(Page Owner) | 계속하기 · 저장 · 불러오기 · 타이틀로 | 설정 → 설정 화면 |
| 6 | 세이브 | Page `SavePage` | 슬롯 · 페이지 · 저장 · 덮어쓰기 · 빈 슬롯 | — |
| 7 | 로드 | Page `LoadPage` | 불러오기 | 삭제(`SaveSlotService.Delete`는 있음, 화면 연결만), 최근 저장 배지(`SavedAtUtc` 비교) |
| 8 | 설정 | Panel `SettingsPanel` | 대사 속도(타자기 `unitsPerSecond`) · 오토 대기(`autoModeDelaySeconds`) · 빠르게 배속(`speedupModeMultiplier`) · 음량 BGM/음성/효과음(`AudioVolumeSettings`, 저장 있음) · 화면: 테마(라이트/다크)·글자 크기(UIPF `UIContext`) | 음량 밖 값의 저장(PlayerPrefs) |
| 9 | 확인 · 알림 | Panel `ConfirmPanel`(모달) · Overlay `NoticeOverlay` | 스킵 확인 · 타이틀로 확인 · 덮어쓰기 | 불러오기 · 삭제 확인, **오류 안내**(저장 실패 · 진행 실패 — 지금 무음인 실패를 화면에 알린다) |
| 10 | 타이틀 · 예제 선택 | Root `TitleScreen` | 처음부터 · 이어하기 · 예제 전환 · 종료 | 설정, 예제 카드(두 예제 소개) |

연출 단독 예제에서는 지금처럼 시스템 메뉴의 저장·불러오기를 숨긴다.

## 그림 규칙 (초안)

UIPF에서 그림이 화면에 들어가는 길은 둘이다.

| 길 | 대상 | 파일 |
|---|---|---|
| **고정 칸 바인딩** | 화면에 한 번 놓이는 그림(패널·이름표·배경) 중 테마마다 달라지는 것 | `ImageBindings/<테마>/<화면>/<refId>.png` — 스프라이트 이름 = refId면 `Sync Selected Theme Folder` 한 번에 꽂힌다 |
| **SpriteSet 변형** | 반복 항목(선택지 행 · 백로그 행 · 슬롯 카드)과 여러 화면이 함께 쓰는 그림 | 기본 그림 1장 + 테마별 변형을 `UISpriteSet`에 등록 |

아이콘은 **기능 이름으로 한 번만** 그린다 — `Art/Icons/Icon_<기능>.png`. 어느 화면의 어느 칸에 놓일지는 빌더가 잇는다(같은 아이콘을 화면 폴더마다 복사하지 않는다).

### 아이콘 목록 (초안 — 받은 그림으로 채워지는 중, 아래 "그림" 절)

| 파일 | 쓰는 곳 |
|---|---|
| `Icon_Auto` | 퀵메뉴 · 자동 진행 |
| `Icon_Skip` | 퀵메뉴 · 빨리 넘기기 |
| `Icon_Rollback` | 퀵메뉴 · 롤백 |
| `Icon_Backlog` | 퀵메뉴 · 백로그 |
| `Icon_HideUI` | 퀵메뉴 · UI 숨김 |
| `Icon_Menu` | 퀵메뉴 · 메뉴 |
| `Icon_Lock` | 선택지 · 선택 불가 |
| `Icon_CurrentLine` | 백로그 · 현재 위치 |
| `Icon_Jump` | 백로그 · 이 줄로 이동 |
| `Icon_Continue` | 시스템 메뉴 · 계속하기 (타이틀 이어하기와 공용 가능) |
| `Icon_Save` | 시스템 메뉴 · 저장, 세이브 화면 |
| `Icon_Load` | 시스템 메뉴 · 불러오기, 로드 화면 |
| `Icon_Settings` | 시스템 메뉴 · 타이틀 · 설정 |
| `Icon_Title` | 시스템 메뉴 · 타이틀로 |
| `Icon_Delete` | 로드 화면 · 삭제 |
| `Icon_PagePrev` · `Icon_PageNext` | 세이브 · 로드 페이지 |
| `Icon_Question` · `Icon_Warning` · `Icon_Error` · `Icon_Info` | 팝업 상태(확인 · 덮어쓰기/삭제 · 오류 · 안내) |
| (선택) `Icon_TabText` · `Icon_TabPlayback` · `Icon_TabVolume` · `Icon_TabDisplay` | 설정 카테고리 탭 |
| (선택) `Icon_Quit` · `Icon_SwitchExample` | 타이틀 |

## 단계

각 단계는 커밋 묶음 하나로 끝나고, 끝에 Unity 확인을 받는다.

### U0. 반입 ✅ (2026-10-27, Unity 확인 남음)
- UIPF `dev` @ `cf3fa98`의 blob을 `git archive`로 꺼내 복사했다(`.meta`째, 원본 GUID 106개 유지 — 이 저장소 `.meta` 888개와 겹침 0). 새 폴더 `.meta` 1개.
- 사본에만 있는 갈림 둘(`Assets/UIPresentationFlow/Documentation~/vendoring.md`):
  - **Editor asmdef 추가** — 원본의 `Editor/UI`에는 asmdef가 없어 그대로 두면 `Assembly-CSharp-Editor`로 컴파일되고, 그 어셈블리가 옛 UI(`Assembly-CSharp`)와 UIPF를 함께 참조해 `UIBase` 같은 이름이 CS0433 오류가 된다. **원본에도 같은 파일을 넣어야 한다.**
  - **런타임 asmdef `autoReferenced: false`** — 처음 계획은 "U0–U3 동안 CS0436 경고 감수"였다. 경고가 사용처마다 나서 "새 경고 0" 기준과 맞지 않으므로, UIPF를 이름으로 참조하는 어셈블리(새 예제 UI)에만 보이게 했다. U3에서 다시 정한다.
- 그래서 옛 UI·본체·진행은 UIPF를 전혀 보지 않는다. 바뀐 것은 UIPF 어셈블리 둘이 새로 생긴 것뿐이다.
- Unity 확인: 컴파일 오류·새 경고 0, 메뉴 `Tools/UI Presentation/…`이 보인다, 두 예제 동작이 그대로.

### U1. 바탕 — 2026-10-27 패키지 적용으로 바뀜(아래 "적용 기록")
- 바뀐 점: 폴더는 `Samples/KedUI`, 글꼴은 Noto Sans KR(패키지), 테마는 Light 하나(다크 그림이 없다), 미리보기 장면 대신 실제 씬에서 공존 스위치(`useKedUI`)로 띄운다.
- `Ked.Samples.UI` · `Ked.Samples.UI.Editor` asmdef.
- 빌더 골격: 화면 빌드 명세 → 프리팹(자식 이름 = Refs, 컴포넌트, 앵커), Presentation · 바인딩 에셋 생성, 아이콘 연결.
- 미리보기 장면 `Samples/UI/UIPreview.unity`(빌더가 만든다): 캔버스 · 층(Root · Panel · Overlay) · 부트스트랩(`UIManager` · `UIResolver` · `UIContext`)과 가짜 데이터로 화면을 띄워 보는 갤러리.
- 테마 둘(Light · Dark): `UIPalette` · `UIFontSet`(NEXON 글꼴) · 빈 `UISpriteSet`. 자리 그림(placeholder).

### U2. 화면 — 2026-10-27 빌드 명세 10화면 작성(Unity 빌드·확인 남음). 순서는 패키지 지시(메뉴 → 저장/불러오기 → 백로그 → 대화·선택지·퀵메뉴 → 설정 → 타이틀)
① `PlayScreen`(대화 · 퀵메뉴 · 선택지) ② 백로그 ③ 시스템 메뉴 ④ 세이브 ⑤ 로드 ⑥ 설정 ⑦ 확인 · 알림 ⑧ 타이틀.
화면마다: 빌드 명세 → 빌더 → 미리보기 장면에서 확인. 본체 계약 구현은 가짜 데이터로 먼저 돌린다.

### U3. 교체 — 연결은 끝났다(공존 스위치). 남은 것: 옛 UI 계층·`Samples/Scripts/UI` 삭제, `PresentationUIRoot`를 무대로 좁히기
- **Unity를 닫고** 진행한다.
- 씬 `PresentationSample`의 옛 UI 계층(타이틀 · 백로그 · 확인 · 시스템 메뉴 · HUD)을 걷고 새 화면을 놓는다. `PresentationUIRoot`를 무대로 좁힌다.
- `Samples/Scripts/UI/` 삭제, `SampleBootstrap` · `PresentationAssembler` · 흐름을 새 화면으로.
- Unity 확인: 경고 0, 두 예제의 전 기능(재생 · 롤백 · 백로그 점프 · 스킵 · 이어하기 · 슬롯 · 갈라지기).

### U4. 시연
- `<<ui_patch dark>>` — `IUIThemePatchPort` → `UIManager.SetContext`. 데모 대본에 한 장면.
- 설정 화면의 테마 · 글자 크기가 열린 화면에 바로 반영.
- README에 "AI가 UIPF 구조로 화면을 만든다" 절: 빌드 명세 하나 → 프리팹 · 바인딩 → 그림 넣기 → 테마 전환.

### U5. 문서
- SCOPE-BOUNDARY에 "꽂힌 모듈 둘"(진행 · UI), ASSEMBLY-BOUNDARY의 어셈블리 표, `PLAN_framework-scope` 분류표.
- 그다음 `PLAN_framework-scope` 6·7단계로 돌아간다.

## 그림 (2026-10-26 받은 것)

아미야가 그린 시트·낱장 19장. 아이콘은 **완성 색**(남색 글리프 + 크림색 원 버튼)이다 — 테마가 바뀌어도 아이콘은 그대로 두고, 다크 테마는 밝은 패널에 팔레트 색을 곱하는 쪽으로 간다.

- 시트는 Unity에서 이름 붙은 하위 스프라이트로 자른다(자르기 데이터는 알파 경계로 자동 검출 — 시트 A 20개, 시트 B 17개, 글리프 시트 12개). 이름이 곧 refId라 폴더 동기화가 그대로 찾는다.
- 늘려 쓰는 패널은 9분할 경계를 모서리 장식 밖에 둔다. 원 6칸이 박힌 퀵메뉴 패널은 고정 크기.
- 아직 없는 그림: 잠금, 백로그 현재 위치·이동, 타이틀로(집), 정보·오류·질문 상태, 최근 저장 배지, 다음 진행 마커. 계속하기 = 재생, 닫기 = X.

## 착수 전에 정할 것 → 정한 것 (2026-10-27)

1. **화면 정의의 형식 → (가) C# 빌드 명세.** `Editor/Build/Specs/*Spec.cs`. 노드 이름은 `nameof(화면.Refs.X)`라 명세와 Refs가 컴파일로 묶이고, 빌더가 만든 뒤 Refs 감사(이름이 정확히 하나)를 한다. 좌표는 `Design/layouts.json`의 숫자 그대로.
2. **교체 시점 → 공존 스위치.** `SampleBootstrap › 예제 UI › useKedUI`(기본 켬). 캔버스 프리팹이 없거나 세우다 실패하면 옛 UI로 연다. 두 UI가 같이 도는 일은 없다 — 새 UI가 켜지면 옛 화면(타이틀·백로그·확인·메뉴·대사창·선택지·버튼)은 시작할 때 감추고, 옛 캔버스에는 연출 무대만 남는다. 옛 코드 삭제는 U3.
3. **슬롯 카드의 그림 창 → (패키지 지시) 알려진 자리 그림 + 실제 메타데이터.** save_slot 그림의 밝은 칸을 그대로 두고 장면 ID · 플레이 시간 · 저장 시각 · 저장 이름을 적는다. 화면 캡처는 별도 작업.

## 하지 않는 것

- 본체(`Ked.Presentation/`) 수정.
- UIPF 사본만 고치기 — 고칠 일은 원본 `dev`에서 먼저 하고 다시 반입한다.
- 동작 없는 설정 항목(해상도 · 언어 등).
- 언어 전환 — `Localizer`는 들이되 표는 한국어 한 열.
- UPM 패키지화.

## 위험

| 위험 | 대응 |
|---|---|
| UIPF 6000.6 코드가 6000.3에서 컴파일되지 않음 | U0에서 바로 확인. 막히면 원본에서 고치고 다시 반입 |
| 교체 전 CS0436 경고 | 새 UI는 별도 asmdef. 경고 기간을 U0–U3로 한정 |
| Unity가 열린 채 파일·씬 변경 | U0 · U3 전에 Unity를 닫는다 |
| 대사창 이중 소유 | 위 소유권 규칙. 프리셋 `overrideImage` 끔 |
| framework-scope 6 · 7단계가 밀림 | U5에서 문서를 같이 고치고 바로 6 · 7단계로 |

## 적용 기록 — Ked UI 패키지 (2026-10-27)

받은 것: `Ked_UI_Handoff_v1.zip`(PNG 49 · 시안 13화면 · `layouts.json` 382노드 · manifest · 임포트 도구 · `CLAUDE_START_HERE.md`).
패키지 지시의 순서(반입 확인 → 에셋 → 공통 부품 · 확인 팝업 → 화면 → 기능 연결 → 검증)로 U1–U3 일부를 앞당겼다.

### 1. 기준선 (반입 상태 확인)

- UIPF API를 읽고 역할을 실제 API에 맞췄다(아래 표). 새 관리자를 만들지 않았다 — `UIManager`를 그대로 쓴다.
- 컴파일 기준: Unity 6000.3.16f1의 참조 어셈블리와 이 PC가 만든 csproj로 Roslyn 오프라인 컴파일(Unity가 짜는 어셈블리 경계 그대로).
  - 바꾸기 전: 13개 어셈블리 오류 0 · 경고 0 (본체 넷 · 진행 셋 · UIPF 둘 · DOTween.Modules · Core.Tests · Assembly-CSharp(-Editor)).
  - 바꾼 뒤: 15개(+ `Ked.Samples.UI` · `Ked.Samples.UI.Editor`) 오류 0 · 경고 0.
- Unity 안의 확인(임포트·빌더·재생)은 아래 "Unity에서 할 일"이다. 오프라인 컴파일은 Unity 컴파일을 대신하지 못한다.

| 시안 역할 | UIPF 실제 API | 화면 |
|---|---|---|
| Root | `UIManager.SwitchRoot` | `TitleScreen` · `PlayScreen` |
| Panel | `PushPanel` · `PopPanel` · `PopAllPanels` | `SystemMenuPanel` · `SaveLoadPanel` · `BacklogPanel` · `SettingsPanel` |
| Page | `SwitchPage(owner, …)` · `IUIPageOwner.PageRoot` | 설정 탭 넷(`Audio`·`Text`·`Playback`·`DisplaySettingsPage`) |
| 모달(확인·안내) | **Panel** — 스택 맨 위만 입력을 받고 아래 Panel은 UIPF가 막는다. 딤이 포인터를 막는다 | `ConfirmPanel` · `NoticePanel` |
| (Overlay) | `ShowOverlay` — 아래 화면을 막지 않는 층이라 모달에 쓰지 않았다 | — |
| 위젯 | `View.Widget<T>` (등록하지 않는 UIBase) | `SaveSlotCard` · `BacklogRow` |
| 테마 | `UIResolver(UIContext, UIThemeAssets(UIFontSet))` · `UIStyledElements`(Prefab 크기 정책) | 글꼴 Noto Sans KR, 사용자 글자 크기 = `UIContext.TextScale` |
| 글자 | `Localizer` · `ViewTexts`(키 `{화면}.{Ref}`) · `ILocalizedView` | `Texts/KedUITexts.csv`(ko-KR · en-US) |
| 표시 변화 | `UIDisplayRefreshDriver` · `SafeAreaRoot` | 모든 Root·Panel |

### 2. 경계 — 본체 diff 0

- 본체(`Ked.Presentation/`)는 한 줄도 바꾸지 않았다. 새 UI는 본체의 계약(`IPresentationDialogueBoxView` · `IPresentationOptionsBoxView` · `IPresentationView`)과 공개 API(`VNFeatureController` · `IPlaybackHost` · `DialogueAdvanceDispatcher` · `VNLinePresentationFlow.LineDisplayed`)만 쓴다.
- 예제 층에서 바꾼 것:
  - `PresentationSceneRefs` — 대사창·선택지 상자 칸 둘(비우면 옛 UI).
  - `PresentationAssembler` — 위 칸을 쓰고, `Build(…, out PresentationInternals)`로 재생 모드 상태와 지금 장면 이름을 예제 UI에 건넨다.
  - `SampleBootstrap` — `useKedUI` 스위치, 화면과 무관한 진행 저장 설치를 화면 설정에서 떼어 냄(`InstallProgressionSaves`), 이어하기 가능 여부(`HasProgressionResumePoint` — 부수 효과 없는 읽기).
  - `NodePlaythrough.HasResumePoint` — 읽기만 하는 확인.
  - `PresentationUIRoot.SetLegacyHudVisible` — 옛 HUD 층만 감춘다(무대 층은 그대로).
- 저장 포맷 · 챕터 진행 규칙 · 리플레이 알고리즘은 그대로다. 저장·불러오기·갈라지기는 `ManualSaveFlow`/`SaveSlotService`를 그대로 부른다.

### 3. 입력 소유권

`VNAdvanceInputPoller`(재생 입력의 유일한 구동자)를 재생 화면에서 Panel 없이 대사를 볼 때만 켠다. 타이틀 · Panel이 열림 · UI 숨김에서는 끈다 — 같은 Space·Enter가 메뉴와 대사를 함께 넘기지 않고, 메뉴가 열린 동안 자동·빨리 넘기기가 멈춘다. 돌려줄 때는 프레임 끝(LateUpdate)에 켜서 창을 닫은 그 키를 폴러가 다시 읽지 않는다. 재생 단위·기억은 건드리지 않는다.
Esc는 맨 위 한 층만: 확인 → 취소, 안내 → 확인, 그 밖의 Panel → 뒤로, UI 숨김 → 다시 보임.

### 4. 기능 연결 표

| 화면 | 연결 | 비고 |
|---|---|---|
| 타이틀 | 처음부터 · 이어하기(기록 없으면 막음) · 불러오기(진행 예제) · 다른 예제로 · 설정 · 종료 | 예제 카드 그림은 그 예제 대본의 실제 배경. 시안 샘플 그림을 쓰지 않는다 |
| 대화 | 대사창 계약 구현, 다음 줄 표시(`LineDisplayed`), 꼬리표(예제 · 노드 이름) | 줄·이름 자리는 새 프리셋 DB(`KedSurfaceLayoutPresetDB`, 키·종류는 옛 DB와 같다) |
| 퀵메뉴 | 자동(`ToggleAuto`) · 빨리 넘기기(`Begin/EndRapidSkip` 토글 — Ctrl과 같은 기능) · 되감기(R과 같은 길) · 기록 · 숨기기 · 메뉴 | 켜짐 표시는 `VNPlaybackRuntimeState` |
| 선택지 | 선택지 상자 계약 구현, 항목 프리팹 `KedOptionItem`(본체 `VNOptionItem` 그대로) | 선택 불가는 재생 쪽 판정 그대로(투명도) |
| 메뉴 | 계속하기 · 저장 · 불러오기 · 설정 · **이 에피소드 건너뛰기** · 타이틀로(확인 뒤 `ExitAsync`) | 에피소드 건너뛰기는 시안에 없지만 옛 화면의 기능이라 남겼다 |
| 저장/불러오기 | 목록 · 새로 저장 · 덮어쓰기(확인) · 불러오기(재생 중이면 확인) · 쪽 넘김 | 서비스가 번호 칸이 아니라 목록이고 덮어쓰면 끝으로 가므로 "최근 저장 = 01"로 보인다. 빈 칸은 새로 저장 칸 하나만 누를 수 있다. 읽을 수 없는 슬롯(콘텐츠 버전)은 막는다. 저장 완료 알림은 실제로 저장된 뒤에만 |
| 백로그 | 기록 · 화자 이름(대본 표의 "이름: 대사"에서 읽음) · 지금 줄 표시 · 이동(지금 장면 = 되감기 / 지난 장면 = 확인 뒤 갈라지기) | 갈 수 없는 줄은 이동 버튼이 없다 |
| 설정 · 음량 | 전체 음량·음소거(`AudioListener`) · 배경음·효과음·음성(본체 `AudioVolumeSettings`) | 시안의 80/65/75/100은 쓰지 않는다 — 실제 값 |
| 설정 · 대사 | 글자 크기(`UIContext.TextScale` 80–130%) · 출력 속도(타자기 `unitsPerSecond`) · 대사창 불투명도 | 미리보기는 고른 속도로 찍는다 |
| 설정 · 재생 | 자동 진행 대기(`autoModeDelaySeconds`) · 빠르게 재생 배속(`speedupModeMultiplier`) | 배속 보기 2× · 4× · 본체 기본값(12×) |
| 설정 · 화면 | 창 모드 · 전체 화면(`Screen.fullScreenMode`) · 키 안내 | 에디터 게임 창에서는 바뀌지 않는다(화면에 적음) |
| 확인 · 안내 | 요청마다 제목·본문·버튼(타이틀로 · 덮어쓰기 · 불러오기 · 에피소드 건너뛰기 · 갈라지기 · 저장 실패 · 불러오기 실패) | 콜백은 한 번(두 번 누름·Esc 겹침에도) |

### 5. 만들지 않은 것 · 시안과 다른 것

- **1.5× 배속** — 본체 하한이 2배(`speedupModeMinMultiplier`)라 보기에서 뺐다.
- **읽은 대사만 넘기기** — 재생 기록에 읽음 표시가 없다. 화면에 그 사실만 적었다.
- **선택지 질문 줄**("어떻게 대답할까요?") — Yarn 선택지에 질문 데이터가 없어 넣지 않았다.
- **썸네일 캡처** — 별도 작업. 지금은 알려진 자리 그림.
- **슬롯 삭제 · 이름 바꾸기** — 서비스에는 있지만 시안에 없어 화면을 만들지 않았다.
- **잠금 아이콘 · 최근 저장 배지** — 그림이 없다.
- **빠르게 재생 버튼** — 시안 퀵메뉴에 없다(S 키 그대로).
- **`ui_patch` → UIPF 테마** — 대본의 `<<ui_patch>>`는 아직 옛 UI에 걸려 있다(U4).
- 키 안내는 입력 바인딩의 **기본 키**를 적은 것이다. 인스펙터에서 키를 바꾸면 안내도 고쳐야 한다.
- 백로그 화자는 대본의 이름이다. 화자 정책 DB의 표시 이름(`fallbackDisplayName`)과 다를 수 있다.
- 시안의 가변 글꼴 `NotoSansKR.ttf`는 들이지 않았다 — 기본 인스턴스가 Thin이라 TMP가 가는 글자로 그린다. 같은 글꼴에서 정적 Medium · Bold를 뽑아 썼다(OFL, 예약 이름은 'Source'뿐).
- 캔버스 비율 맞춤은 **Expand**(시안 권장 Match 0.5 아님) — 0.5면 2560×1080에서 1600×900 판의 위아래 60씩이 잘린다. 옛 UI 캔버스(1920×1080)는 그대로다.

### 6. Unity에서 할 일

1. Unity를 연다 — 새 PNG · TTF의 `.meta`를 Unity가 만든다(이 커밋에는 없다).
2. `Tools/Ked UI/Build Sample UI` — 그림 임포트 설정(패키지 도구) → TMP 글꼴 → 화면 프리팹 12 → 데이터 자산 → `Resources/KedUI/KedUICanvas` → Refs 감사. 콘솔 보고에 "문제"가 없어야 한다.
3. 만들어진 `.meta` · `Generated/` · `Resources/KedUI/`를 커밋한다.
4. 플레이 — 두 예제(인스펙터 '예제')에서 아래 확인 목록.

### 7. 확인 목록 (패키지 IMPLEMENTATION §9)

- [ ] 컴파일 오류·새 경고 0, 빌드 보고 문제 0, 플레이 중 Missing 참조·Refs 경고 0
- [ ] 확인 팝업: 수락 · 취소 · × · Esc · 다시 열기 · 두 번 누르기(콜백 한 번) · 아래 대사가 넘어가지 않음
- [ ] 메뉴 → 저장/불러오기/설정/백로그 → 닫기 후 재생이 그대로(멈추지 않았고 기억이 남아 있음)
- [ ] 저장 → 불러오기가 기존 호스트로 이어짐, 목록은 기존 서비스의 것
- [ ] 진행·자동·빨리 넘기기·되감기·선택지·백로그 이동이 그대로, 같은 입력이 두 번 먹지 않음
- [ ] 긴 한국어·영어(KedUIHost › localeId = en-US) · 두 줄 확인 문구 · 긴 저장 이름 · 글자 크기 130%
- [ ] 1600×900 화면을 Design/*.png와 비교 캡처, 1920×1080 · 1920×1200 · 2560×1080 잘림·입력
