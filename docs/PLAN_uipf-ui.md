# PLAN — 예제 UI를 UIPresentationFlow 위에 다시 세운다

2026-10-26. `PLAN_framework-scope` 6·7단계(문서·검증)보다 먼저 한다(아미야 결정).
작업 브랜치: `feat/uipf-ui` ← `refactor/framework-scope` @ `7ed5c1a5`.

## 목표 한 문장

> ked의 예제 UI 전체를 UIPresentationFlow(UIPF) 위에서 코드 빌더로 다시 세운다. 화면은 UIPF의 구조(Refs · Presentation · 바인딩)로 만들어지고, 그림은 이름 규칙대로 폴더에 넣으면 채워진다. 본체(`Assets/Ked.Presentation/`) diff 0.

보여 줄 것 — **AI가 UIPF 구조로 화면을 만들어 낸다.**
화면 정의(빌드 명세) → 빌더가 프리팹·Refs 계약·바인딩을 만든다 → 그림을 넣으면 테마가 선다. (테마 전환 시연은 하지 않는다 — 10-27 아미야, 테마는 하나)

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
├─ UIPresentationFlow/     UIPF 사본 — 원본과 같은 구조(Scripts/ · Editor/UI · Shaders), asmdef는 런타임 하나
│   └─ Documentation~/vendoring.md
└─ Samples/
    ├─ KedUI/              새 예제 UI — 화면 하나 = 묶음 하나(아래 "화면 묶음"). App/과 Art/를 뺀 것이 "VN UI 묶음"이다 —
    │   │                  재생 런타임(본체)을 모르고, UIPF 저장소의 비주얼 노벨 데모로 그대로 복사된다(아래 "VN UI를 UIPF 샘플로")
    │   ├─ Ked.Samples.UI.asmdef   런타임 asmdef(뿌리 — Common · Screens). 참조는 UIPF · TMP · UGUI뿐
    │   ├─ Common/         여러 화면이 같이 쓰는 것 — Scripts(부품 키트) · Editor(키트로 짓는 도구 · 검사 · 에디터 asmdef) · Sprites · Fonts
    │   ├─ Screens/        화면 묶음 7 — Title · Play · Menu · SaveLoad · Backlog · Settings · Modal
    │   │                  각 묶음: Scripts · Prefabs · Presentation · Sprites · Editor(+.asmref)
    │   ├─ App/            ked 앱 쪽 — 재생 런타임에 잇는 것. asmdef 둘(Ked.Samples.UI.App · .App.Editor)
    │   │   ├─ Runtime/    흐름(Flow) · 설정 값(Settings/KedSettings) · KedUIHost · 대사창 · 선택지 상자 어댑터(Play/)
    │   │   ├─ Editor/     앱 빌드 — 화면 목록 · 캔버스 · 선택지 항목(KedUIBuild · KedUIAssets · OptionItemSpec)
    │   │   ├─ Prefabs/    선택지 항목 KedOptionItem(본체 VNOptionItem) · Data/ 대사창 배치 DB(본체 형식)
    │   │   ├─ Texts/      UI 글자 표 KedUITexts.csv(ko-KR · en-US)
    │   │   └─ Resources/KedUI/KedUICanvas.prefab   빌더가 만든다 — SampleBootstrap이 읽는다
    │   └─ Art/            받았지만 어느 화면도 쓰지 않는 그림 14장(시안 배경 포함)
    └─ Scripts/
        ├─ Game/KedUI/     호스트 어댑터(기본 어셈블리) — KedHostPorts · StageForKedUI
        └─ Stage/          연출 무대 PresentationUIRoot(+ Providers/) — 옛 UI/는 U3에서 걷었다
```

새 UI를 별도 asmdef에 두는 이유: 옛 UI와 UIPF는 같은 전역 타입 이름 20개를 쓴다(`UIBase` · `UIManager` · `UIRoot` …). 새 코드가 UIPF만 보는 어셈블리에 있으면 교체 전까지 둘이 섞이지 않는다. U3에서 옛 UI를 걷어 이제 겹치는 이름은 없다.

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

### U0. 반입 ✅ (2026-10-27, Unity 확인 10-27)
- UIPF `dev` @ `cf3fa98`의 blob을 `git archive`로 꺼내 복사했다(`.meta`째, 원본 GUID 106개 유지 — 이 저장소 `.meta` 888개와 겹침 0). 새 폴더 `.meta` 1개.
- 사본에만 있는 갈림 둘(`Assets/UIPresentationFlow/Documentation~/vendoring.md`):
  - **Editor asmdef 추가** — 원본의 `Editor/UI`에는 asmdef가 없어 그대로 두면 `Assembly-CSharp-Editor`로 컴파일되고, 그 어셈블리가 옛 UI(`Assembly-CSharp`)와 UIPF를 함께 참조해 `UIBase` 같은 이름이 CS0433 오류가 된다. **원본에도 같은 파일을 넣어야 한다.** — U3에서 옛 UI를 걷어 CS0433의 원인은 없어졌다. 하지만 이 asmdef는 그것과 별개로 필요하다. 사본은 원본의 `Scripts/` 한 층을 걷어 런타임 asmdef가 폴더 뿌리에 있고, 그래서 `Editor/UI`가 런타임 asmdef 아래에 든다. Unity는 asmdef 아래의 `Editor` 폴더를 에디터 전용으로 보지 않는다. 빼면 에디터 코드(가드 없이 `UnityEditor` 사용)가 런타임 어셈블리에 섞여 플레이어 빌드가 깨진다. 10-27에 빼려다 오프라인 컴파일에서 런타임 어셈블리가 78 → 88파일이 되는 것을 보고 되돌렸다. → 같은 날 사본 구조를 원본대로 되돌려(아래 "UIPF 사본 구조를 원본대로") 지웠다.
  - **런타임 asmdef `autoReferenced: false`** — 처음 계획은 "U0–U3 동안 CS0436 경고 감수"였다. 경고가 사용처마다 나서 "새 경고 0" 기준과 맞지 않으므로, UIPF를 이름으로 참조하는 어셈블리(새 예제 UI)에만 보이게 했다. → U3에서 원본 값 `true`로 되돌렸다.
- 그래서 옛 UI·본체·진행은 UIPF를 전혀 보지 않는다. 바뀐 것은 UIPF 어셈블리 둘이 새로 생긴 것뿐이다. (U3 뒤: 갈림을 모두 없애 어셈블리는 원본처럼 런타임 하나 + 에디터 코드는 `Assembly-CSharp-Editor`)
- Unity 확인: 컴파일 오류·새 경고 0, 메뉴 `Tools/UI Presentation/…`이 보인다, 두 예제 동작이 그대로.

### U1. 바탕 — 2026-10-27 패키지 적용으로 바뀜(아래 "적용 기록")
- 바뀐 점: 폴더는 `Samples/KedUI`, 글꼴은 Noto Sans KR(패키지), 테마는 Light 하나(다크 그림이 없다), 미리보기 장면 대신 실제 씬에서 공존 스위치(`useKedUI`)로 띄운다.
- `Ked.Samples.UI` · `Ked.Samples.UI.Editor` asmdef.
- 빌더 골격: 화면 빌드 명세 → 프리팹(자식 이름 = Refs, 컴포넌트, 앵커), Presentation · 바인딩 에셋 생성, 아이콘 연결.
- 미리보기 장면 `Samples/UI/UIPreview.unity`(빌더가 만든다): 캔버스 · 층(Root · Panel · Overlay) · 부트스트랩(`UIManager` · `UIResolver` · `UIContext`)과 가짜 데이터로 화면을 띄워 보는 갤러리.
- 테마 둘(Light · Dark): `UIPalette` · `UIFontSet`(NEXON 글꼴) · 빈 `UISpriteSet`. 자리 그림(placeholder).

### U2. 화면 — 2026-10-27 빌드 명세 10화면 작성, Unity 빌드·동작 확인(10-27). 순서는 패키지 지시(메뉴 → 저장/불러오기 → 백로그 → 대화·선택지·퀵메뉴 → 설정 → 타이틀)
① `PlayScreen`(대화 · 퀵메뉴 · 선택지) ② 백로그 ③ 시스템 메뉴 ④ 세이브 ⑤ 로드 ⑥ 설정 ⑦ 확인 · 알림 ⑧ 타이틀.
화면마다: 빌드 명세 → 빌더 → 미리보기 장면에서 확인. 본체 계약 구현은 가짜 데이터로 먼저 돌린다.

### U3. 교체 ✅ (2026-10-27, Unity 확인 10-27)
Unity를 닫고 했다.

- **씬 `PresentationSample`** (YAML 문서 1,090 → 291):
  - 무대 `PresentationUIRoot`를 옛 관리자가 실행 중에 붙이던 자리 `[UIRoot]/Root`에 미리 놓았다. `[UIRoot]`는 `[StageCanvas]`로 이름을 바꿨다. 위치는 옛 관리자가 붙일 때처럼 0, 앵커는 그대로다. 캔버스 설정(1920×1080 · Match 0.5 · Overlay · 정렬 0)도 실행 때와 같다.
  - 걷은 것:
    - `[UIManager]`: 옛 관리자 · 타이틀 · 백로그 · 확인 · 시스템 메뉴, 아무도 쓰지 않던 `Prefabs_Bg`
    - 무대 안의 `DialogueUILayer`(옛 대사창·선택지)와 `ButtonLayer`(옛 퀵메뉴·버튼)
    - 빈 층 캔버스 `Panels` · `Overlay` · `Top`
  - `Root`에 붙어 있던 CanvasScaler(800×600)를 지웠다. 중첩 캔버스라 실행에서는 원래 무시되던 것이다. 그런데 본체 튜닝 덤프 도구(`PresentationTuningExporter`)는 무대 부모 쪽의 첫 CanvasScaler를 기준 해상도로 읽는다. 지우면 `[StageCanvas]`의 값(1920×1080 · 0.5)을 읽는다. 옮기기 전의 `[UIManager]`와 같은 값이다.
  - 남은 오브젝트가 걷은 것을 가리키는 참조 0, 끊긴 fileID 0. 바뀐 문서는 일곱뿐이다(무대 · Root 둘 · `[StageCanvas]` 둘 · 부트스트랩 · 씬 루트 목록).
- **코드**:
  - `Samples/Scripts/UI/`를 지웠다(52파일, 약 5,600줄). 무대만 `Samples/Scripts/Stage/`로 `.meta`째 옮겼다. 옮긴 것은 `PresentationUIRoot` · `RefCache` · `Providers/` 넷 · 디버그 `ScreenFocusGridOverlay`다. 스크립트 GUID가 그대로라 씬 컴포넌트가 이어진다.
  - `PresentationUIRoot`는 옛 UI 프레임(`UIRoot`) 대신 `MonoBehaviour`다. 칸 찾기 규칙은 옛것과 같다(자식 이름, 깊이 우선 첫 것). 화면 층 · 버튼 · 이벤트는 뺐다. 조립이 `Initialize()`로 칸을 먼저 찾는다(여러 번 불러도 한 번).
  - `SampleBootstrap`:
    - 공존 스위치(`useKedUI`) · 옛 UI 갈래(`VNScreenBindings`) · `uiManager` · `optionItem` · `surfaceLayoutPresetDb` 칸을 지웠다.
    - `무대` 칸을 더했다.
    - 새 UI 캔버스가 없으면 예제를 세우지 않고 오류로 알린다. 돌아갈 옛 UI가 없다. 입력 폴러도 함께 멈춘다(연결 전이라 매 프레임 null).
  - `PresentationAssembler`: 옛 관리자 · 그림 패치 설치를 지웠다. 대사창 · 선택지 상자는 화면 쪽이 넘긴 것만 쓴다(없으면 조립 오류).
  - `StageForKedUI`(옛 `LegacyStageForKedUI`): 무대가 꺼져 있으면 켜기만 한다.
  - `<<ui_patch>>`: 옛 그림 패치와 함께 걷었다. 지금은 `UIThemePatchNotConnected`가 받아 경고만 남기고 화면은 그대로 둔다. 테마를 하나만 쓰므로 잇지 않는다(U4 뺌). 지금 예제 대본에는 ui_patch가 없다.
  - UIPF 런타임 asmdef의 `autoReferenced`를 원본 값 `true`로 되돌렸다. 사본 갈림은 Editor asmdef 하나로 줄었다(`vendoring.md`). 남은 하나는 사본의 폴더 구조 때문이었다 — 구조를 원본대로 되돌려 없앴다(아래).
- **에셋 삭제**: 옛 UI만 쓰던 것이다. 다른 에셋 · 코드 · 대본에서 GUID나 경로로 참조하는 곳이 없음을 확인했다.
  - `Samples/Prefabs/`: 백로그 줄 · 저장 슬롯 · 선택지 항목 프리팹
  - `Samples/Resources/ui/test/`: 옛 그림 패치 테마
  - `Resources/legacy/background_02`
  - `Resources/`의 낱장 UI 그림 7개
- **남긴 것**:
  - `Samples/Data/DialogueSurfaceLayoutPresetDB`(옛 대사창 배치): 본체 도구 `PresentationTuningExporter`가 경로로 읽는다. 본체 diff 0이라 그대로 둔다.
  - `Assets/Fonts/NEXON Maplestory`: 옛 UI만 썼지만 예제 밖 폴더라 두었다.
  - `Samples/Art/Emoji`: 연출용이다(`PLAN_framework-scope`).
- **오프라인 컴파일**: 15개 어셈블리 오류 0 · 경고 0. `Assembly-CSharp`는 68파일에서 24파일로 줄었다.
- **독립 리뷰**: 두 건을 고쳤다(위 CanvasScaler, 입력 폴러). 씬 YAML 정합 · 지운 에셋의 참조 · 무대 표시와 CanvasGroup · 정렬 · 초기화 순서는 회귀 없음.
- **Unity 확인**: 아래 "U3 확인"과 7절.

#### U3 확인
2026-10-27 Unity 확인(아미야): 아래 넷을 요청했고 "잘 된다".
- [x] 컴파일 오류 · 새 경고 0, 씬을 열어 Missing Script 0(`[StageCanvas]/Root/PresentataionUIRoot`)
- [x] 무대 — 배경 · 인물 · 화면 효과 · 무대 마스크 · 샷(팬 · 줌)이 전과 같다
- [x] 두 예제의 전 기능 — 재생 · 되감기 · 백로그 이동 · 빨리 넘기기 · 이어하기 · 슬롯 · 갈라지기
- [x] 디버그 — `DebugView`의 `ScreenFocusGridOverlay` · `CharacterFocusDebugView`

#### UIPF 사본 구조를 원본대로 (2026-10-27, 아미야 결정)
Unity를 닫고 했다. 사본에 남은 갈림(Editor asmdef)의 원인이 사본만의 폴더 구조였으므로, 구조를 원본에 맞췄다.
- `UI/` · `Presentation/` · `Display/` · `Localization/`(파일 163 + 폴더 `.meta` 4)와 런타임 asmdef(+`.meta`)를 `Assets/UIPresentationFlow/Scripts/` 아래로 옮겼다(`.meta`째, GUID 그대로).
- `Scripts.meta`를 원본 `Assets/Scripts.meta` 그대로(GUID `b92fa89c…`, 바이트 같음) 더했다.
- 우리가 더했던 `Editor/UIPresentationFlow.Editor.asmdef`(+`.meta`)를 지웠다. 에디터 코드 10파일은 원본처럼 `Assembly-CSharp-Editor`에 든다(런타임 asmdef 밖의 `Editor` 폴더).
- 어셈블리: 15 → 14개. `UIPresentationFlow.Runtime` 78파일 그대로, `Assembly-CSharp-Editor` 1 → 11파일. 오프라인 컴파일 오류 0 · 경고 0.
- `vendoring.md`: 대응표 · 대조 명령을 새 구조로, "사본에만 있는 것"은 없음.
- 대조(`vendoring.md`의 명령, 원본 `cf3fa98` blob, PC에서): `Scripts`(Demo 제외) · `Editor` · `Shaders` · 폴더 `.meta` 셋 모두 출력 0줄. 파일 수 197 = 197.
- Unity 확인(10-27, 아미야 "잘 된다"): [x] 컴파일 오류 · 새 경고 0 · [x] `Tools/UI Presentation/…` 메뉴 · [x] `Tools/Ked UI/Build Sample UI` 보고 문제 0

### U4. 시연 — 하지 않는다 (2026-10-27, 아미야)
테마는 하나(Light)만 쓴다. 그래서 테마 전환 시연(`<<ui_patch dark>>` → `SetContext`)과 다크 그림은 만들지 않는다.
- `<<ui_patch>>`는 `UIThemePatchNotConnected`가 받아 경고만 남기고 화면은 그대로 둔다.
- 원래 U4에 있던 README 절("AI가 UIPF 구조로 화면을 만든다")은 테마 전환을 빼고 U5로 옮겼다.

### 화면 묶음 (2026-10-27, 아미야 제안 → 두 화면으로 시험 → 전체 적용)
UIPF 위에 만든 화면을 "화면 하나 = 묶음 하나"(코드 · 프리팹 · 그림 · Presentation)로 두고 `.meta`째 반입하는 구성. UIPF 원본도 아직 이 구조가 아니어서 여기서 먼저 써 봤다. 바꾼 곳은 `Samples/KedUI`뿐이고 UIPF 사본은 건드리지 않았다.

(10-27 뒤 — Runtime · Editor · 글자 표 · 캔버스 · 선택지 항목 · 대사창 배치 DB는 `App/`으로 옮겼다. 위 "코드 위치"가 지금 모양이다.)

```
Samples/KedUI/
├─ Ked.Samples.UI.asmdef   런타임 asmdef를 뿌리로(Common/ · Screens/ · Runtime/을 덮는다)
├─ Common/
│   ├─ Scripts/            부품 키트 7 (옛 Runtime/Kit. 끌기 넘기기 KedDragToScroll은 UIPF로 옮겨 지웠다 — 아래 "UIPF로 되돌림")
│   ├─ Editor/             부품 키트(KedUIKit) · 묶음 설정(KedUIUnits) · 글꼴 · 그림 임포트 · 검사(KedUIValidate) + 에디터 asmdef. 규칙과 도구는 10-27부터 UIPF Editor/UI/Authoring(아래 "UIPF로 되돌림")
│   ├─ Sprites/            키트가 쓰는 15장 + 여러 화면이 쓰는 3장(background_title · icon_close · icon_rollback)
│   └─ Fonts/              Noto Sans KR TTF 2 · OFL · SDF 2 · UIPF 글꼴 묶음(KedUIFontSet)
├─ Screens/
│   ├─ Title/              TitleScreen
│   ├─ Play/               PlayScreen(+ 대사창 · 선택지 상자 어댑터) · 선택지 항목 KedOptionItem · Data/ 대사창 배치 DB
│   ├─ Menu/               SystemMenuPanel
│   ├─ SaveLoad/           SaveLoadPanel(+ 위젯 SaveSlotCard)
│   ├─ Backlog/            BacklogPanel(+ 위젯 BacklogRow)
│   ├─ Settings/           SettingsPanel + Page 넷(Page는 주인 Panel의 묶음)
│   └─ Modal/              ConfirmPanel · NoticePanel(명세 하나가 둘을 만든다)
│      각 묶음: Scripts/ · Prefabs/ · Presentation/({화면}Presentation.asset) · Sprites/(그 화면만 쓰는 그림, 없으면 폴더 없음)
│               · Editor/(빌드 명세 + Ked.Samples.UI.Editor.asmref)
├─ Runtime/                앱 쪽 — Flow · Settings/KedSettings · KedUIHost
├─ Editor/                 앱 빌드(KedUIBuild · KedUIAssets) + .asmref
├─ Art/                    아무 화면도 쓰지 않는 그림 14장
└─ Texts/ · Resources/ · asset-manifest.json
```

- 묶음에 넣지 않는 것: 화면 흐름(`KedUIFlow.*`) · 설정 값(`KedSettings`) · 캔버스와 호스트 · 글자 표(CSV 하나). 화면을 쓰는 앱 쪽이다.
- 빌더 규칙(`Common/Editor/KedUIUnits.cs`):
  - 그림은 이름으로 찾는다(KedUI 아래 어디든, 같은 이름 둘이면 문제). 패키지 임포트 도구도 같은 규칙. `asset-manifest.json`은 받은 그대로.
  - 화면 스크립트가 `Screens/{묶음}/Scripts/`에 있으면 프리팹 · Presentation · Data는 그 묶음에. 아니면 `Generated/`(지금은 그런 화면이 없어 폴더도 없다).
  - 묶음 감사: 묶음 안 에셋이 직접 가리키는 것이 제 묶음 · Common · UIPF · 본체(게임 쪽 프레임워크 — 선택지 항목 `VNOptionItem`, 대사창 배치 DB 형식) · 패키지뿐인지. 빌드 보고에 나온다.
- 옮긴 것은 모두 `.meta`째(GUID 그대로). 새로 만든 것: 폴더 `.meta` · 묶음마다 `.asmref` 하나 · `KedUIUnits.cs`.
- 시험(백로그 · 설정, `cd517f70`) → 10-27 Unity 확인(아미야 "너무 좋다") → 나머지 다섯 묶음과 `Generated/`의 남은 것(선택지 항목 · 대사창 배치 DB · 글꼴 묶음)까지 옮겼다(`1640c416`).
- 옮기며 Presentation 파일 이름만 바꾸고 안의 오브젝트 이름(`m_Name`)을 두어 Unity 경고가 났다 → 12개를 파일 이름에 맞추고, 빌더도 어긋나면 맞추게 했다(`0597b859`). UIPF는 `presentationId`로 찾으므로 동작은 같다.
- 10-27 Unity 확인(아미야 "잘 동작함"): 컴파일 0, 빌드 보고 문제 0(묶음 감사 7), `Generated/`가 다시 생기지 않음, 전 화면 동작.

판단 기준과 결과:

| 기준 | 결과 |
|---|---|
| 1. 묶음이 닫혀 있다(다른 묶음 · `Art/` · `Generated/`를 가리키지 않는다) | 정적 검사(GUID 참조를 셈): 7묶음 모두 밖을 가리키는 것 0. Unity 묶음 감사도 7묶음 통과(10-27) |
| 2. 빌더가 그대로 돈다 — 생성물이 묶음에 갱신되고 `Generated/`에 같은 이름이 새로 생기지 않는다, 보고 문제 0 | 시험 두 묶음 · 전체 적용 모두 Unity 확인(10-27, 아미야 "잘 동작함") |
| 3. 묶음 안 에디터 코드가 `.asmref`로 에디터 어셈블리에 붙는다 | 오프라인 컴파일: 명세 여덟이 모두 `Ked.Samples.UI.Editor`로(런타임 37파일 그대로), 14개 어셈블리 오류 0 · 경고 0. Unity 확인(10-27) |
| 4. 화면 하나를 빼고 다시 넣어도 이어진다 | 1이 성립하면 GUID로 이어진다 — 필요하면 Unity에서 따로 시험 |

### 제작 원칙 — 명세가 원본, 프리팹은 산출물 (2026-10-27, 아미야 결정)
AI가 프리팹(Unity 직렬화 데이터)을 직접 쓰지 않는다. AI는 화면 스크립트와 빌드 명세를 쓰고, 프레임워크 쪽 빌더가 그 프로젝트의 Unity로 프리팹을 만들고, 사람은 Unity에서 결과를 보고 판정한다.

| 구분 | 무엇 | 규칙 |
|---|---|---|
| 원본 | 화면 스크립트(`Screens/*/Scripts`) · 빌드 명세(`Screens/*/Editor`) · 그림 · 부품 키트(`Common`) | 고칠 것은 여기서 고친다 |
| 산출물 | 화면 프리팹(`Screens/*/Prefabs`) · 캔버스(`Resources/KedUI`) · 글꼴 SDF | 빌드마다 덮어쓴다. **손으로 고치지 않는다** — 다음 빌드에서 사라진다. 클론만으로 실행되게 **커밋한다** |
| 처음 한 번 만드는 데이터 | Presentation(`Screens/*/Presentation`) · 대사창 배치 DB(`Play/Data`) | 없을 때만 빌드가 만든다. 그 뒤로는 에셋에서 고친다(빌드가 덮지 않는다). 글꼴 묶음은 글꼴 칸만 빌드마다 다시 건다 |

- **특수 화면.** 명세로 나타내기 어려운 화면(비정형 배치 · 복잡한 연출)은 프리팹을 손이나 에디터 스크립트로 직접 만들어도 된다. 조건은 셋이다. 같은 `UIBase<TRefs>`를 쓴다, 묶음 규칙(`Screens/{묶음}/…`)을 따른다, 검사(`Validate Sample UI`)를 지난다. 명세를 넓혀 모든 화면을 덮으려 하지 않는다 — 넓힐지는 사람이 정한다. 아직 빌더는 손으로 만든 화면을 캔버스에 걸지 못한다. 필요해지면 그때 넓힌다.
- **검사는 빌드와 따로.** `Tools/Ked UI/Validate Sample UI`(`Common/Editor/KedUIValidate`)는 빌드하지 않고 묶음 아래 모든 프리팹 · Presentation을 본다: 그림 이름 겹침 · Refs(이름이 정확히 하나) · Presentation 이름과 `presentationId` · 묶음 감사. 빌드도 저장한 뒤 같은 검사를 돈다.
- **배포(반입) 두 방식.** 묶음은 원본과 산출물(`.meta` 포함)을 함께 담는다.
  - 받는 쪽 Unity 버전 · 패키지가 같다 → 산출물째 들인다(검수한 그대로, GUID 그대로).
  - 다르다 → 원본과 산출물의 `.meta`를 들이고 그 자리 위에 다시 빌드한다(직렬화 형식 문제를 피하고 GUID는 지킨다). 파일 안쪽 fileID가 버전을 넘어 그대로인지는 확인하지 않았다.
- **재생성.** 같은 명세로 다시 빌드하면 산출물이 같다 — 10-27 여러 번 빌드한 뒤 묶음의 프리팹 13(화면 12 · 선택지 항목 1) · 캔버스의 git 변경 0(Unity가 같은 내용이라 다시 쓰지도 않았다). 고침(10-27 뒤): 그 빌드들은 플레이 중에 돌았던 것으로 보이고, 같은 이름의 자식이 여러 개인 프리팹(슬롯 카드 6장)은 편집 모드에서 다시 빌드할 때마다 파일 안 fileID가 바뀐다 — 아래 "UIPF로 되돌림"의 재빌드.
- **사람의 역할.** 요구와 품질 기준을 정하고, AI의 설계 판단(명세를 어디까지 넓힐지, 공용으로 올릴지)을 정하고, Unity에서 눈으로 검수 · 승인한다. 문제를 찾으면 프리팹이 아니라 원본을 고쳐 다시 빌드한다.
- **배치 모드.** `-executeMethod Ked.Samples.UI.Editor.KedUIBuild.BuildBatch` · `…KedUIValidate.ValidateBatch`(문제가 있으면 종료 코드 1). Unity가 이 프로젝트를 열고 있으면 배치 모드는 프로젝트를 열지 못한다. 클라우드 세션에서는 PC의 Unity를 돌릴 수 없다 — PC에서 돌린다.

빌더 자리(10-27): 화면을 모르는 도구(`KedUIKit` · `KedUIUnits` · `KedUIFonts` · `KedUIArtImporter` · `KedUIValidate`, 에디터 asmdef)는 `Common/Editor` — 그 가운데 게임에 매이지 않은 부분은 같은 날 UIPF로 옮겼다(아래 "UIPF로 되돌림"의 B), 화면 목록과 캔버스를 아는 앱 빌드(`KedUIBuild` · `KedUIAssets`)는 `KedUI/Editor`(+`.asmref`). 실행 쪽과 짝이 맞는다 — 공용 `Common/Scripts` ↔ `Common/Editor`, 앱 `Runtime/` ↔ `Editor/`, 화면 `Screens/X/Scripts` ↔ `Screens/X/Editor`.

### UIPF로 되돌림 (2026-10-27, 아미야 결정 "C, A까지만 먼저")
여기서 쓰다 드러난 것을 UIPF 원본으로 되돌렸다. 원본에서 먼저 고치고(`feat/from-ked` @ `3a26d8b`, `dev` 머지 전), 다시 반입하고, 이쪽의 임시 해결책을 지웠다. 범위와 다음 일(B — 빌더 가운데 어느 게임에도 쓸 부분을 원본 도구로)은 원본 `docs/PLAN_from-ked.md`.

| | 원본에서 | 이쪽에서 지운 것 |
|---|---|---|
| A-1 끌기 | `UI_EventHandler`가 끌기를 받는 쪽이 없으면 가장 가까운 부모의 끌기 처리기(ScrollRect)로 넘긴다. 같은 오브젝트에 끌기를 받는 다른 컴포넌트(Slider 등)가 있으면 넘기지 않는다 | `Common/Scripts/KedDragToScroll.cs`(+ `.meta`)와 `BacklogSpec`의 붙이는 줄 |
| A-2 Root 막기 | Panel이 하나라도 있으면 지금 Root의 `CanvasGroup.interactable`을 끈다. 기본 켬(`BlockRootUnderPanels`), `blocksRaycasts`는 그대로 | 흐름의 Root 막기는 `_exiting` 몫만 남았다(값은 UIPF와 같아 겹쳐도 무해) |
| A-3 포커스 | Panel을 쌓을 때의 선택을 기억했다가 꺼낼 때 되돌린다. 전부 꺼내면 맨 처음 선택으로 | 흐름의 `_focusBeforePanel` 스택. 되돌리지 못했을 때 고르는 `EnsureFocus`만 남았다 |

- 원본 문서도 이번에 더했다: `docs/AUTHORING.md`(위 "제작 원칙"을 게임에 매이지 않게 다시 쓴 것), README의 복사 안내(이 저장소에서 겪은 폴더 층 · `.meta` · asmdef · 에셋 내리지 않기)와 실사용 절.
- 반입 대조(`vendoring.md`의 명령, `3a26d8b`): 0줄. UIPF `.meta` 108개(+ `UIManager.PanelInput.cs.meta`, GUID 겹침 0).
- 오프라인 컴파일(6000.3 참조 어셈블리): 14개 어셈블리 오류 0 · 경고 0. `UIPresentationFlow.Runtime` 79파일, `Ked.Samples.UI` 36파일.
- `BacklogPanel.prefab`에는 지운 컴포넌트가 빈 스크립트로 남는다 — 산출물이라 손으로 고치지 않고, Unity에서 `Build Sample UI`로 다시 짓는다.
- Unity 재빌드(10-27, 편집 모드로 두 번): 지운 컴포넌트가 걷혔다. 함께 드러난 것 둘.
  - 이전에 커밋한 프리팹 10개의 버튼에 `UI_EventHandler`가 있었다. 이것은 `KedButton.Awake`(→ `ButtonFeedback.Bind`)가 실행 중에만 붙이므로, 이전 빌드는 플레이 중에 돈 것이다. 편집 모드 결과(10개 프리팹에서 47개가 빠짐)를 커밋했다. 실행하면 같은 것이 붙어 동작은 같다. 빌더가 플레이 중에는 거부하게 하는 것은 B에서.
  - 저장/불러오기 패널은 두 번의 편집 모드 빌드 결과가 서로 달랐다. 같은 이름의 자식(슬롯 카드 6장의 `CardButton` 등)이 여러 개라 Unity가 파일 안 fileID를 다시 매긴다. fileID를 뺀 비교에서는 `UI_EventHandler` 11개 말고 같다. GUID 참조는 그대로다. 그래서 "재생성 git 변경 0"은 이름이 겹치지 않는 프리팹에서만 맞다.
- Unity 확인(10-27): 백로그 목록을 이동 버튼 위에서 끌기, 메뉴를 연 채 방향키 · Enter가 재생 화면에 닿지 않음, 메뉴 · 설정을 닫으면 연 버튼으로 포커스, `Validate Sample UI` 문제 0. 원본 블랙홀 데모도 확인한 뒤 아미야가 UIPF `dev`에 합쳤다(`4a4af6f`).

#### B — 빌더의 공용 부분을 UIPF 도구로 (10-27, 아미야 결정: 게임에 매이지 않은 것만 · 앱이 목록과 뿌리를 쥔다 · 블랙홀 일시 정지 창으로 증명)
UIPF `feat/authoring-tools` @ `f912e99`(`dev` 머지 전)에 `Editor/UI/Authoring`과 에디터 asmdef `UIPresentationFlow.Editor`를 두고 다시 반입했다(대조 0줄, UIPF `.meta` 118개).

| 이쪽에 있던 것 | 지금 |
|---|---|
| `KedNode` · `KedAnchor` · 자리 계산(`Node` · `Fill` · `Stretch` · `StretchX` · `Place` · 기준점 가운데로) | UIPF `UINode` · `UIAnchor` · `UINodes`. 명세 여덟과 키트는 이름만 바꿨다(`KedNode` → `UINode`). 키트의 `Node` · `Fill` …은 UIPF를 부르는 한 줄 |
| 직렬화 칸 쓰기(`SetFloat` · `SetObject`) | UIPF `UISerialized` |
| 그림 색인 · 겹침(`KedUIUnits.ArtIndex`) | UIPF `UISpriteIndex`. KedUI는 겹침을 모두 문제로(`KedUIUnits.Art`) |
| 묶음 규칙 · 감사(`KedUIUnits`) | UIPF `UIScreenUnits`. `KedUIUnits`에는 뿌리와 허용할 곳(UIPF 사본 · 본체)만 남았다(`Rules`) |
| 검사(Refs · Presentation) | UIPF `UIScreenValidation`. `KedUIValidate`는 엄하게(위젯 Refs · presentationId) 부르고 묶음 감사 · 그림 이름을 더한다 |
| 프리팹 저장 · Presentation 만들기 | UIPF `UIPrefabs.Save` · `UIPresentationAssets.Ensure`. 빌드는 플레이 중이면 멈춘다(`UIPrefabs.CanBuild`) |
| 글꼴 만들기 · 비우기 · 굵기 표 | UIPF `UIFontAssets`. `KedUIFonts`에는 KedUI의 글꼴과 자리만 |

- 남은 것(ked의 몫): 부품 꼴(`KedUIKit` — 그림 · 글꼴 · 버튼 · 슬라이더 · 스위치), 설정, 검사 메뉴, 명세 여덟, 앱 빌드(`KedUIBuild` · `KedUIAssets`), 그림 임포트.
- 오프라인 컴파일(6000.3): 15개 어셈블리 오류 0 · 경고 0. `UIPresentationFlow.Editor` 18파일(옛 `Editor/UI` 10 + `Authoring` 8)이 새 어셈블리가 되어 `Assembly-CSharp-Editor`는 1파일.
- 자리 계산은 옮기기 전과 같은 식이다 — 다시 지으면 슬롯 카드 프리팹(fileID만) 말고 git 변경 0이어야 한다. 보고의 검사 줄은 "검사: 화면 묶음 7 · 프리팹 13(화면 · 위젯 N) · Presentation 12"로 바뀐다.
- Unity 확인(10-27, 아미야 "문제 없는 것을 확인함"): `Build Sample UI`(편집 모드) 뒤 git 변경은 `SaveLoadPanel.prefab`의 fileID뿐, `Validate Sample UI` 문제 0. UIPF 블랙홀 일시 정지 창도 명세로 다시 지어 같은 화면임을 확인했다(UIPF `c22bb76`).
- 다시 반입: UIPF `c22bb76`(`UIPrefabs.cs` 주석 — fileID 설명을 관찰한 것만으로 고침). 대조 0줄.

### VN UI를 UIPF 샘플로 — 묶음과 App 나누기 · 화면비 Variant (10-27, 아미야 결정)
UIPF 저장소의 Adaptive UI 데모를 걷고, 그 자리에 이 VN UI를 샘플로 들인다. Page · 위젯 · 스크롤 목록 · 모달 · 포커스처럼 블랙홀 데모가 보여 주지 못한 것을 다 갖고 있어서다(아미야).

정한 것(넷 모두 추천안):
- **ked가 원본이고 UIPF는 복사본이다.** 블랙홀과 같은 방식이다. UIPF 쪽의 데모 흐름(가짜 데이터)만 UIPF가 원본이다.
- **재생 런타임에 묶인 것은 `KedUI/App/`으로 뺀다.** 복사 규칙은 "KedUI에서 `App/`과 `Art/`를 뺀 것"이다.
- **UIPF의 Adaptive UI 데모는 전부 걷는다.** `Scripts/Demo`, `Demo/AdaptiveUI`, `Assets/UI`, 씬을 걷는다.
- **화면비 Variant를 ked 원본에 하나 더한다.** 지금은 레이아웃 Variant 시연이 Adaptive에만 있다.

이 저장소에서 한 것:
- 그대로는 복사할 수 없었다. 재생 화면이 본체 계약에 묶여 있었다 — 대사창 · 선택지 상자(`IPresentationDialogueBoxView` · `IPresentationOptionsBoxView`, Yarn), 선택지 항목(`VNOptionItem`), 대사창 배치 DB, 설정 범위(`KedSettings`).
- 어댑터는 보통 클래스였다(MonoBehaviour가 아니다). 그래서 둘로 갈랐다.
  - 묶음 쪽 `PlayDialogueBox` · `PlayOptionsBox`는 UI 일만 한다: 글자 · 자리 · 배율 · 불투명도 · 보이기.
  - App 쪽 `KedDialogueBoxView` · `KedOptionsBoxView`가 본체 계약을 그 위에 구현한다: 연출 프리셋 → `PlayDialogueLayout`, Yarn 페이드.
  - `PlayScreen`은 `Dialogue` · `Options`(묶음 타입)를 내준다.
- 그 밖에 옮긴 것:
  - 타이틀 카드 모델 `KedExampleCard`는 Title 묶음으로.
  - 설정 범위는 Settings 묶음의 `KedSettingRanges`로(`KedSettings`가 같은 값을 쓴다).
  - `KedUIUtil`은 public으로.
- 옮긴 곳(모두 `.meta`째, GUID 그대로):
  - `Runtime/` · `Editor/` · `Texts/` · `Resources/` → `App/`
  - `Screens/Play/Data/KedSurfaceLayoutPresetDB` → `App/Data/`
  - `Screens/Play/Prefabs/KedOptionItem` · `Screens/Play/Editor/OptionItemSpec` → `App/Prefabs/` · `App/Editor/`
  - 옛 `Editor/Ked.Samples.UI.Editor.asmref` → `App/Editor`의 asmdef(`Ked.Samples.UI.App.Editor`)
- asmdef:
  - `Ked.Samples.UI`는 UIPF · TMP · UGUI만 참조한다(본체 · 진행 · Yarn 참조를 걷었다).
  - `Ked.Samples.UI.Editor`는 본체 참조를 걷었다.
  - App 런타임(`Ked.Samples.UI.App`)은 본체 · 진행 · Yarn을 참조한다. 기본 어셈블리가 쓰도록 autoReferenced다.
  - KedUIHost(캔버스의 MonoBehaviour)는 GUID가 그대로라 어셈블리가 바뀌어도 이어진다.
- 묶음은 자리를 고정하지 않는다. 뿌리는 `Ked.Samples.UI.asmdef`가 있는 폴더, UIPF 코드 자리는 `UIPresentationFlow.Runtime.asmdef`로 찾는다(`KedUIUnits`). 앱 빌드가 부르도록 명세 · 키트 · 글꼴 · 묶음 설정을 public으로 했다.
- 묶음 감사의 "허용할 곳"에서 본체를 뺐다. 정적 검사: 묶음이 묶음 밖(제 묶음 · Common · UIPF 코드 말고)을 가리키는 곳 0, 묶음 → App 0.
- 그림 임포트 설정(`KedUIArtImporter`, 메뉴 `Apply Art Import Settings`)은 manifest 줄 중 PNG가 없는 것을 건너뛰고 로그로 알린다(전에는 멈췄다). 복사본에는 `Art/`가 없어서다. 화면이 쓰는 그림이 없으면 빌드의 키트가 문제로 올린다.
- **화면비 Variant**: 재생 화면 Presentation에 `UltraWide_HudInBoard`(UltraWide = 21:9 이상, 우선순위 10)를 더한다. 꼬리표와 퀵메뉴를 화면 끝이 아니라 가운데 1600 판의 양 끝에 둔다 — 기본이면 21:9에서 퀵메뉴가 대사창 오른쪽 끝보다 300쯤 밖으로 나간다.
  - 처음 한 번 만드는 데이터라, 앱 빌드가 Presentation을 만든 뒤 `PlaySpec.EnsureVariants`를 부른다. 이미 있으면 건드리지 않는다.
  - 도구는 UIPF에 더한 `UIPresentationAssets.EnsureLayoutVariant` · `MoveWidget`(UIPF `feat/authoring-tools`).
  - 레이아웃 패치는 `Screens/Play/Presentation/PlayScreenPresentation.UltraWide_HudInBoard.asset`.
- 오프라인 컴파일(6000.3): 17개 어셈블리 오류 0 · 경고 0.
- Unity 확인: 남음
  - Unity를 닫고 이동을 반영한다.
  - `Build Sample UI`(편집 모드) → 문제 0.
  - 재생 · 메뉴 · 선택지 · 백로그 · 설정이 전과 같은지.
  - Game 뷰 21:9에서 꼬리표 · 퀵메뉴가 판 안쪽으로 오는지, 16:9로 돌리면 제자리로 가는지.
  - `Validate Sample UI` 문제 0.

### U5. 문서
- SCOPE-BOUNDARY에 "꽂힌 모듈 둘"(진행 · UI), ASSEMBLY-BOUNDARY의 어셈블리 표, `PLAN_framework-scope` 분류표.
- U3로 낡은 곳(본체 문서라 U3에서는 고치지 않았다):
  - ASSEMBLY-BOUNDARY §5 — `Samples/Scripts/UI/Providers/` → `Samples/Scripts/Stage/Providers/`.
  - ASSEMBLY-BOUNDARY §6 — `IUIThemePatchPort`의 구현은 `UI/Core/Patcher/UIThemePatchAdapter.cs`가 아니라 `PresentationAssembler`의 `UIThemePatchNotConnected`(테마 하나라 잇지 않는다).
  - `PLAN_framework-scope` 분류표 — "UI 프레임·화면·바인딩 `Samples/Scripts/UI/`", "대사창·선택지 뷰 베이스는 `Samples/Scripts/UI/DialogueBox/`", 예제 에셋 줄의 프리팹 · UI 이미지.
- README에 "AI가 UIPF 구조로 화면을 만든다" 절: 빌드 명세 하나 → 프리팹 · Refs 계약 · 바인딩 → 그림 넣기(U4에서 옮김, 테마 전환은 뺌).
- 그다음 `PLAN_framework-scope` 6·7단계로 돌아간다.

## 그림 (2026-10-26 받은 것)

아미야가 그린 시트·낱장 19장. 아이콘은 **완성 색**(남색 글리프 + 크림색 원 버튼)이다 — 테마가 바뀌어도 아이콘은 그대로 두고, 다크 테마는 밝은 패널에 팔레트 색을 곱하는 쪽으로 간다.

- 시트는 Unity에서 이름 붙은 하위 스프라이트로 자른다(자르기 데이터는 알파 경계로 자동 검출 — 시트 A 20개, 시트 B 17개, 글리프 시트 12개). 이름이 곧 refId라 폴더 동기화가 그대로 찾는다.
- 늘려 쓰는 패널은 9분할 경계를 모서리 장식 밖에 둔다. 원 6칸이 박힌 퀵메뉴 패널은 고정 크기.
- 아직 없는 그림: 잠금, 백로그 현재 위치·이동, 타이틀로(집), 정보·오류·질문 상태, 최근 저장 배지, 다음 진행 마커. 계속하기 = 재생, 닫기 = X.

## 착수 전에 정할 것 → 정한 것 (2026-10-27)

1. **화면 정의의 형식 → (가) C# 빌드 명세.** `Editor/Build/Specs/*Spec.cs`(화면 묶음 뒤로는 `Screens/*/Editor/*Spec.cs`). 노드 이름은 `nameof(화면.Refs.X)`라 명세와 Refs가 컴파일로 묶이고, 빌더가 만든 뒤 Refs 감사(이름이 정확히 하나)를 한다. 좌표는 `Design/layouts.json`의 숫자 그대로.
2. **교체 시점 → 공존 스위치.** `SampleBootstrap › 예제 UI › useKedUI`(기본 켬). 캔버스 프리팹이 없거나 세우다 실패하면 옛 UI로 연다. 두 UI가 같이 도는 일은 없다 — 새 UI가 켜지면 옛 화면(타이틀·백로그·확인·메뉴·대사창·선택지·버튼)은 시작할 때 감추고, 옛 캔버스에는 연출 무대만 남는다. 옛 코드 삭제는 U3. → U3에서 스위치째 걷었다. 새 UI 캔버스가 없으면 예제를 세우지 않는다.
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
  - U3 뒤: `useKedUI`와 `SetLegacyHudVisible`은 없어졌다. `PresentationSceneRefs`의 `UIManager` 칸은 `Stage`로, 대사창 · 선택지 상자 칸은 필수로 바뀌었다(위 U3).
- 저장 포맷 · 챕터 진행 규칙 · 리플레이 알고리즘은 그대로다. 저장·불러오기·갈라지기는 `ManualSaveFlow`/`SaveSlotService`를 그대로 부른다.

### 3. 입력 소유권

`VNAdvanceInputPoller`(재생 입력의 유일한 구동자)를 재생 화면에서 Panel 없이 대사를 볼 때만 켠다. 타이틀 · Panel이 열림 · UI 숨김에서는 끈다 — 같은 Space·Enter가 메뉴와 대사를 함께 넘기지 않고, 메뉴가 열린 동안 자동·빨리 넘기기가 멈춘다. 돌려줄 때는 프레임 끝(LateUpdate)에 켜서 창을 닫은 그 키를 폴러가 다시 읽지 않는다. 재생 단위·기억은 건드리지 않는다.
Esc는 맨 위 한 층만: 확인 → 취소, 안내 → 확인, 그 밖의 Panel → 뒤로, UI 숨김 → 다시 보임.
Panel이 열린 동안 Root(타이틀·재생 화면)의 CanvasGroup도 막는다 — 그러지 않으면 방향키·Enter가 아래 Root 버튼·선택지에 닿는다. 처음에는 흐름이 막았고, 10-27부터 UIPF가 막는다(`BlockRootUnderPanels`, 아래 "UIPF로 되돌림"). 타이틀로 나가는 동안(`ExitAsync`)은 Panel이 없어도 흐름이 막는다.
키보드 포커스: Panel을 닫으면 연 순간의 포커스로 돌아간다. 처음에는 흐름이 쌓아 두었고, 10-27부터 UIPF가 되돌린다. 되돌리지 못하면(연 버튼이 사라졌거나 막혔으면) 흐름이 고른다 — 남은 Panel의 첫 버튼, 재생 화면이면 떠 있는 선택지. UI 숨김은 재생 화면이 따로 기억한다.
빨리 넘기기는 버튼으로 켜면 스스로 꺼지지 않으므로 타이틀 복귀·불러오기·갈라지기 때 끈다.

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
- **`ui_patch` → UIPF 테마** — 옛 그림 패치는 U3에서 걷었다. 대본의 `<<ui_patch>>`는 경고만 남긴다. 테마를 하나만 쓰므로 잇지 않는다(U4 뺌).
- 키 안내는 입력 바인딩의 **기본 키**를 적은 것이다. 인스펙터에서 키를 바꾸면 안내도 고쳐야 한다.
- 백로그 화자는 대본의 이름이다. 화자 정책 DB의 표시 이름(`fallbackDisplayName`)과 다를 수 있다.
- 시안의 가변 글꼴 `NotoSansKR.ttf`는 들이지 않았다 — 기본 인스턴스가 Thin이라 TMP가 가는 글자로 그린다. 같은 글꼴에서 정적 Medium · Bold를 뽑아 썼다(OFL, 예약 이름은 'Source'뿐).
- 캔버스 비율 맞춤은 **Expand**(시안 권장 Match 0.5 아님) — 0.5면 2560×1080에서 1600×900 판의 위아래 60씩이 잘린다. 무대 캔버스 `[StageCanvas]`(1920×1080 · Match 0.5)는 그대로다.

### 6. Unity에서 할 일

1. Unity를 연다 — 새 PNG · TTF의 `.meta`를 Unity가 만든다(이 커밋에는 없다).
2. `Tools/Ked UI/Build Sample UI` — 그림 임포트 설정(패키지 도구) → TMP 글꼴 → 화면 프리팹 12 → 데이터 자산 → `Resources/KedUI/KedUICanvas` → Refs 감사. 콘솔 보고에 "문제"가 없어야 한다.
3. 만들어진 `.meta` · 생성물 · `Resources/KedUI/`를 커밋한다(10-27 커밋함. 생성물 자리는 그 뒤 화면 묶음으로 바뀌었다).
   - 빌더는 끝에 두 TMP 글꼴의 동적 데이터를 비우고 저장한다(`TMP_FontAsset.ClearFontAssetData(true)` — 아틀라스 1×1, 자산 몇 KB). 비우지 않으면 글자 몇 개만 들어가도 2048² 아틀라스가 자산에 통째로 저장되어 8MB가 넘는다.
   - 플레이하거나 프리팹을 열면 쓴 글자가 다시 채워져 글꼴 SDF 자산(화면 묶음 시험 뒤로는 `Common/Fonts/*.asset`, 그 전은 `Generated/Fonts/`)이 바뀐다. **그 변경은 커밋하지 않는다** — `git restore`로 되돌리거나, 빌더를 다시 돌리거나, 글꼴 자산 톱니 메뉴 `Clear Dynamic Data`로 비운다. 플레이어 빌드에는 `Clear Dynamic Data On Build`(켜 둠)가 같은 일을 한다.
4. 플레이 — 두 예제(인스펙터 '예제')에서 아래 확인 목록.

### 7. 확인 목록 (패키지 IMPLEMENTATION §9)

2026-10-27 Unity 확인(사용자): 빌드 뒤 플레이에서 동작 확인, 해상도별 확인, 시안 비교 — 의도대로 나왔다. 아래는 항목별로 확인한다고 들은 것만 체크했다.

- [ ] 컴파일 오류·새 경고 0, 빌드 보고 문제 0, 플레이 중 Missing 참조·Refs 경고 0
- [ ] 확인 팝업: 수락 · 취소 · × · Esc · 다시 열기 · 두 번 누르기(콜백 한 번) · 아래 대사가 넘어가지 않음
- [ ] 메뉴 → 저장/불러오기/설정/백로그 → 닫기 후 재생이 그대로(멈추지 않았고 기억이 남아 있음)
- [ ] 저장 → 불러오기가 기존 호스트로 이어짐, 목록은 기존 서비스의 것
- [ ] 진행·자동·빨리 넘기기·되감기·선택지·백로그 이동이 그대로, 같은 입력이 두 번 먹지 않음
- [ ] 긴 한국어·영어(KedUIHost › localeId = en-US) · 두 줄 확인 문구 · 긴 저장 이름 · 글자 크기 130%
- [x] 1600×900 화면을 Design/*.png와 비교 캡처, 1920×1080 · 1920×1200 · 2560×1080 잘림·입력 (10-27)
