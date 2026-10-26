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

```
Assets/
├─ Ked.Presentation/       본체 — 손대지 않음
├─ Ked.Progression/        진행 코어 사본 — 손대지 않음
├─ UIPresentationFlow/     UIPF 사본 (asmdef UIPresentationFlow.Runtime · 새로 UIPresentationFlow.Editor)
│   └─ Documentation~/vendoring.md
└─ Samples/
    ├─ UI/                 새 예제 UI — asmdef Ked.Samples.UI
    │   ├─ Screens/        화면 클래스(Refs 계약)와 본체 계약 구현
    │   ├─ Flow/           화면 흐름(지금 VNScreenBindings의 몫)
    │   ├─ Settings/       설정 값 저장·적용
    │   ├─ Editor/         빌더 — asmdef Ked.Samples.UI.Editor
    │   └─ Art/ · Presentations/ · ImageBindings/ · TextBindings/ · Theme/ · Prefabs/
    └─ Scripts/UI/         옛 UI — U3에서 지운다
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

### U1. 바탕
- `Ked.Samples.UI` · `Ked.Samples.UI.Editor` asmdef.
- 빌더 골격: 화면 빌드 명세 → 프리팹(자식 이름 = Refs, 컴포넌트, 앵커), Presentation · 바인딩 에셋 생성, 아이콘 연결.
- 미리보기 장면 `Samples/UI/UIPreview.unity`(빌더가 만든다): 캔버스 · 층(Root · Panel · Overlay) · 부트스트랩(`UIManager` · `UIResolver` · `UIContext`)과 가짜 데이터로 화면을 띄워 보는 갤러리.
- 테마 둘(Light · Dark): `UIPalette` · `UIFontSet`(NEXON 글꼴) · 빈 `UISpriteSet`. 자리 그림(placeholder).

### U2. 화면 — 아미야 순서대로
① `PlayScreen`(대화 · 퀵메뉴 · 선택지) ② 백로그 ③ 시스템 메뉴 ④ 세이브 ⑤ 로드 ⑥ 설정 ⑦ 확인 · 알림 ⑧ 타이틀.
화면마다: 빌드 명세 → 빌더 → 미리보기 장면에서 확인. 본체 계약 구현은 가짜 데이터로 먼저 돌린다.

### U3. 교체
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

## 착수 전에 정할 것

1. **화면 정의의 형식.** (가) C# 빌드 명세 — 화면 클래스의 `Refs`가 UIPF 계약이므로 원천이 하나다. (나) JSON 명세 + 생성기(Refs enum까지 생성) — AI가 다루기 더 쉬운 텍스트지만 코드 생성 단계가 하나 는다.
2. **교체 시점.** 미리보기 장면에서 화면을 다 만든 뒤 한 번에 교체(U3) / 화면 하나씩 바로 교체. 한 번에 교체를 권한다 — 두 UI 프레임이 한 씬에서 같이 돌면 층 순서와 입력이 엉킨다.
3. **슬롯 카드의 그림 창.** 지금 ked는 저장 때 화면을 찍지 않는다. (가) 저장 때 화면 캡처(새 기능) (나) 그 장면의 배경 그림 (다) 마지막 대사 글자.

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
