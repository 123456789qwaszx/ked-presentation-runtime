# 이 폴더는 복사 반입본이다

`UIPresentationFlow`(UIPF)는 **다른 저장소가 원본**이다. 이 폴더에는 그 저장소의 프레임워크 코드가 한 벌 들어 있다.

```
원본   UIPresentationFlow          (형제 폴더 · Unity 프로젝트 · UI 프레임워크의 집 + 데모 둘)
반입   Assets/UIPresentationFlow/  ← 이 폴더
```

`Assets/Ked.Progression/`과 같은 방식이다. 규율도 같다. **사본만 고치지 않는다.** 고칠 일이 생기면 원본에서 먼저 고치고 다시 반입한다.

들여온 이유는 예제 UI를 UIPF 위에서 다시 세우기 위해서다(`docs/PLAN_uipf-ui.md`). 본체(`Assets/Ked.Presentation/`)는 UIPF를 모른다. UIPF를 쓰는 것은 예제 층뿐이다.

## 스냅샷

| | |
|---|---|
| 원본 | `UIPresentationFlow` · `feat/authoring-tools` @ `6fe89e7` (2026-10-27, `dev` 머지 전). `dev` @ `4a4af6f` 위에 화면 짓기 도구(`Editor/UI/Authoring`)와 에디터 asmdef를 더했다(`f912e99`, `c22bb76`은 그 주석 · 문서 고침과 블랙홀 일시 정지 창 산출물, `6fe89e7`은 레이아웃 Variant를 처음 한 번만 더하는 도구). `4a4af6f`는 `cf3fa98` 위에 이 저장소에서 드러난 빈틈 셋(끌기 넘기기 · Panel 아래 Root 막기 · 포커스 되돌리기)을 더한 것 — 원본 `docs/PLAN_from-ked.md`. 원본의 그 뒤 `feat/vn-sample` 커밋 `752d168`(Adaptive UI 데모 걷음) · `a5bcb2b`(비주얼 노벨 샘플)은 데모 · 문서만 바꿨다 — 사본 대상은 같다(아래 "대조" 0줄, 10-27) |
| 반입 | 2026-10-27 · 원본 → 사본. 처음은 `cf3fa98`, 같은 날 `3a26d8b`(= `dev` `4a4af6f`)로 다시(`Scripts/UI`의 셋을 고치고 `UIManager.PanelInput.cs` 하나를 더함), 그다음 `f912e99`로 다시(`Editor/UI`에 asmdef와 `Authoring/` 여덟 파일), `c22bb76`으로 다시(`UIPrefabs.cs` 주석), `6fe89e7`로 다시(`UIPresentationAssets.cs` — `EnsureLayoutVariant` · `MoveWidget`) |
| 원본 Unity | 6000.6.2f1 (ugui 2.6.0). 이 저장소는 6000.3.16f1 (ugui 2.0.0)이다. 코드만 들이고 에셋은 이 저장소에서 새로 만든다 |
| GUID | 원본 `.meta`를 그대로 가져왔다(106개, 같은 날 구조를 원본대로 되돌리며 `Scripts.meta`를 더해 107개, `3a26d8b`에서 `UIManager.PanelInput.cs.meta`를 더해 108개, `f912e99`에서 asmdef · `Authoring` 폴더 · 스크립트 여덟의 `.meta`를 더해 118개). 반입 때 이 저장소 전체 `.meta` 888개와 겹치는 GUID는 0개였고, 뒤에 더한 것들의 GUID도 겹치지 않는다. 사본 폴더 자신의 `Assets/UIPresentationFlow.meta`만 이 저장소에서 만든 것이다 |

**원본과 갈리면 이 표를 고친다.** 표가 낡으면 사본이 어디서 왔는지 아무도 모르게 된다.

## 무엇을 가져왔나

원본 README "요구 사항"의 복사 목록 그대로다. 폴더 구조도 원본과 같다 — 원본의 `Assets/` 아래 자리를 이 폴더 아래에 그대로 둔다.

| 원본 | 사본 |
|---|---|
| `Assets/Scripts/{UI,Presentation,Display,Localization}/` + `UIPresentationFlow.Runtime.asmdef` | `Assets/UIPresentationFlow/Scripts/` 같은 자리 |
| `Assets/Editor/UI/` | `Assets/UIPresentationFlow/Editor/UI/` |
| `Assets/Shaders/` | `Assets/UIPresentationFlow/Shaders/` |
| 폴더 `.meta` `Scripts` · `Editor` · `Shaders` | 같은 GUID 그대로 |

에디터 코드(`Editor/UI`)는 원본의 에디터 asmdef `Editor/UI/UIPresentationFlow.Editor.asmdef`(`f912e99`부터)에 든다. 예제 UI의 에디터 asmdef(`Ked.Samples.UI.Editor`)가 이것을 참조해 화면 짓기 도구를 쓴다 — asmdef 어셈블리는 `Assembly-CSharp-Editor`를 참조할 수 없다.

가져오지 않은 것은 다음과 같다.
- `Assets/Demo/`: 블랙홀 데모, 비주얼 노벨 샘플(이 저장소 VN UI 묶음의 복사본 + 데모 앱 — 이 저장소가 묶음의 원본이다)
- `Assets/Scripts/Demo/` · `Assets/UI/`: Adaptive UI 데모의 코드와 바인딩 · 테마 에셋. 원본에서 `752d168`로 걷었다

## 사본에만 있는 것 — 없다

가져온 파일은 모두 원본 blob과 바이트 단위로 같다(LF, `.meta` 포함). 폴더 구조도 같다.

반입 때(2026-10-27)는 갈림이 셋 있었고, 같은 날 모두 없앴다.

| 반입 때의 갈림 | 왜 있었나 | 어떻게 없앴나 |
|---|---|---|
| 원본의 `Scripts/` 한 층을 걷어 네 폴더와 런타임 asmdef를 이 폴더 뿌리에 둠 | 경로를 짧게 하려고 | 원본 구조로 되돌렸다(`Scripts/` 아래로 옮김, `.meta`째) |
| `Editor/UIPresentationFlow.Editor.asmdef` 추가 | 위 구조 때문에 `Editor/UI`가 런타임 asmdef 아래에 들었다. Unity는 asmdef 아래의 `Editor` 폴더를 에디터 전용으로 보지 않아서, 이것이 없으면 에디터 코드가 런타임 어셈블리에 섞인다. 반입 때 먼저 드러난 증상은 옛 예제 UI와 이름이 겹친 CS0433이었다 | 구조를 되돌려 필요 없어졌다. 지웠다. 뒤에 원본이 같은 이름의 asmdef를 `Editor/UI`에 두었다(`f912e99`) — 이것은 원본의 것이고 까닭도 다르다(앱의 빌드 명세가 참조) |
| 런타임 asmdef `autoReferenced: true → false` | 옛 예제 UI(기본 어셈블리)가 UIPF와 같은 전역 타입 이름 20개를 써서, 자동 참조면 CS0436 경고가 사용처마다 났다 | PLAN U3에서 옛 UI를 걷고 원본 값 `true`로 |

다시 갈림이 생기면 이 절에 이유와 "언제까지"를 적는다.

## 대조

원본과 갈렸는지 확인한다. 원본 커밋의 blob을 풀어 비교한다(원본 작업 트리에는 커밋하지 않은 변경이 섞일 수 있다).

```bash
mkdir -p /tmp/uipf && git -C ../UIPresentationFlow archive --format=tar <커밋> \
  Assets/Scripts Assets/Scripts.meta Assets/Editor Assets/Editor.meta Assets/Shaders Assets/Shaders.meta \
  | tar -x -C /tmp/uipf
diff -r -q --strip-trailing-cr -x Demo -x Demo.meta /tmp/uipf/Assets/Scripts  Assets/UIPresentationFlow/Scripts
diff -r -q --strip-trailing-cr                     /tmp/uipf/Assets/Editor   Assets/UIPresentationFlow/Editor
diff -r -q --strip-trailing-cr                     /tmp/uipf/Assets/Shaders  Assets/UIPresentationFlow/Shaders
for m in Scripts Editor Shaders; do diff --strip-trailing-cr /tmp/uipf/Assets/$m.meta Assets/UIPresentationFlow/$m.meta; done
```

결과가 0줄이면 같다. 결과(2026-10-27, `cf3fa98`)는 PLAN_uipf-ui의 U3 기록에, `3a26d8b`(= `dev` `4a4af6f`)와 `f912e99` · `c22bb76` · `6fe89e7`은 "UIPF로 되돌림"에 있다.

## 반입 — 원본 → 사본

1. **Unity를 닫는다.** 열린 채 파일을 바꾸면 `.meta`가 새로 생긴다.
2. 원본에서 커밋 하나를 고르고, 그 커밋의 blob으로 가져온다. 작업 트리에는 커밋하지 않은 변경이 섞일 수 있어서 쓰지 않는다.
   ```bash
   git -C ../UIPresentationFlow archive --format=tar <커밋> \
     Assets/Scripts Assets/Editor Assets/Shaders | tar -x -C /tmp/uipf
   ```
3. 위 표의 자리에 `.meta`째 덮어쓴다. `Demo`는 빼고 가져온다.
4. 갈림이 있으면(위 절) 다시 적용한다. 지금은 없다.
5. 위 "대조"를 돌리고, 스냅샷 표를 고친다.

## 왜 UPM 패키지가 아닌가

원본은 아직 UPM 패키지가 아니다. `package.json`이 없고, 네임스페이스가 없다(런타임 asmdef 안에 있던 Adaptive UI 데모는 `752d168`에서 걷었다). 패키지로 만드는 일은 원본에서 따로 한다. 그 전까지는 진행 코어와 같은 복사 반입을 쓴다.
