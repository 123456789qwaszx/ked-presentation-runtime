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
| 원본 | `UIPresentationFlow` · `dev` @ `cf3fa98` (2026-10-26). 코드는 `main` @ `ff3f281`과 같고, `dev`에는 README와 GIF만 더해졌다 |
| 반입 | 2026-10-27 · 원본 → 사본 |
| 원본 Unity | 6000.6.2f1 (ugui 2.6.0). 이 저장소는 6000.3.16f1 (ugui 2.0.0)이다. 코드만 들이고 에셋은 이 저장소에서 새로 만든다 |
| GUID | 원본 `.meta`를 그대로 가져왔다(106개). 이 저장소 전체 `.meta` 888개와 겹치는 GUID는 0개다 |

**원본과 갈리면 이 표를 고친다.** 표가 낡으면 사본이 어디서 왔는지 아무도 모르게 된다.

## 무엇을 가져왔나

원본 README "요구 사항"의 복사 목록 그대로다. 원본의 `Scripts/` 한 층을 걷고 이 폴더 바로 아래에 놓았다. 그래서 런타임 asmdef가 이 폴더의 뿌리에 온다.

| 원본 | 사본 |
|---|---|
| `Assets/Scripts/{UI,Presentation,Display,Localization}/` | `Assets/UIPresentationFlow/{UI,Presentation,Display,Localization}/` |
| `Assets/Scripts/UIPresentationFlow.Runtime.asmdef` | `Assets/UIPresentationFlow/UIPresentationFlow.Runtime.asmdef` |
| `Assets/Editor/UI/` | `Assets/UIPresentationFlow/Editor/UI/` |
| `Assets/Shaders/` | `Assets/UIPresentationFlow/Shaders/` |

가져오지 않은 것은 다음과 같다.
- `Assets/Scripts/Demo/`: Adaptive UI 데모 코드
- `Assets/Demo/`: 데모 에셋, 블랙홀 데모
- `Assets/UI/`: 데모의 바인딩·테마 에셋

## 사본에만 있는 것 — 갈림 두 가지

| 갈림 | 이유 | 언제까지 |
|---|---|---|
| `Editor/UIPresentationFlow.Editor.asmdef` (+`.meta`) 추가 | 원본의 `Editor/UI`에는 asmdef가 없다. 그대로 두면 `Assembly-CSharp-Editor`로 컴파일되는데, 그 어셈블리는 옛 예제 UI가 든 `Assembly-CSharp`도 함께 참조한다. 그러면 두 어셈블리가 같은 전역 이름(`UIBase` 등)을 들고 있어 CS0433 오류가 난다. 이 asmdef는 런타임 asmdef만 참조하도록 감싼다 | **원본에 같은 파일을 넣어야 한다.** 원본에 들어가면 이 줄을 지운다 |
| `UIPresentationFlow.Runtime.asmdef`의 `autoReferenced: true → false` | 옛 예제 UI(`Assets/Samples/Scripts/UI`, 기본 어셈블리)는 UIPF와 같은 전역 타입 이름 20개를 쓴다. 자동 참조로 두면 기본 어셈블리에 CS0436 경고가 사용처마다 난다. 끄면 UIPF는 이름으로 참조한 어셈블리(새 예제 UI의 asmdef)에만 보인다 | 이 저장소만의 설정이다. 원본에서는 데모(asmdef 없음)가 자동 참조에 기댄다. 옛 UI를 걷는 PLAN U3에서 다시 정한다 |

그 밖의 파일은 원본 blob과 바이트 단위로 같다(LF).

## 대조

원본과 갈렸는지 확인한다. 형제 폴더에 원본이 있다고 가정한다.

```bash
diff -r -q --strip-trailing-cr \
  ../UIPresentationFlow/Assets/Scripts/UI            Assets/UIPresentationFlow/UI
diff -r -q --strip-trailing-cr \
  ../UIPresentationFlow/Assets/Scripts/Presentation  Assets/UIPresentationFlow/Presentation
diff -r -q --strip-trailing-cr \
  ../UIPresentationFlow/Assets/Scripts/Display       Assets/UIPresentationFlow/Display
diff -r -q --strip-trailing-cr \
  ../UIPresentationFlow/Assets/Scripts/Localization  Assets/UIPresentationFlow/Localization
diff -r -q --strip-trailing-cr \
  ../UIPresentationFlow/Assets/Editor/UI             Assets/UIPresentationFlow/Editor/UI
diff -r -q --strip-trailing-cr \
  ../UIPresentationFlow/Assets/Shaders               Assets/UIPresentationFlow/Shaders
diff --strip-trailing-cr \
  ../UIPresentationFlow/Assets/Scripts/UIPresentationFlow.Runtime.asmdef \
  Assets/UIPresentationFlow/UIPresentationFlow.Runtime.asmdef
```

2026-10-27 기준 결과는 다음과 같다.
- 앞의 여섯 줄은 출력이 **0줄**이다(`.meta` 포함).
- 마지막 줄은 `autoReferenced` 한 줄만 다르다(위 "갈림").
- `Editor/UIPresentationFlow.Editor.asmdef`는 비교 범위 밖이다(`Editor/UI`의 형제).

## 반입 — 원본 → 사본

1. **Unity를 닫는다.** 열린 채 파일을 바꾸면 `.meta`가 새로 생긴다.
2. 원본에서 커밋 하나를 고르고, 그 커밋의 blob으로 가져온다. 작업 트리에는 커밋하지 않은 변경이 섞일 수 있어서 쓰지 않는다.
   ```bash
   git -C ../UIPresentationFlow archive --format=tar <커밋> \
     Assets/Scripts Assets/Editor Assets/Shaders | tar -x -C /tmp/uipf
   ```
3. 위 표의 자리에 `.meta`째 덮어쓴다. `Demo`는 빼고 가져온다.
4. "갈림" 두 가지를 다시 적용한다. 원본에 이미 들어간 갈림은 건너뛴다.
5. 위 "대조"를 돌리고, 스냅샷 표를 고친다.

## 왜 UPM 패키지가 아닌가

원본은 아직 UPM 패키지가 아니다. `package.json`이 없고, 네임스페이스가 없으며, 데모가 런타임 asmdef 안에 있다. 패키지로 만드는 일은 원본에서 따로 한다. 그 전까지는 진행 코어와 같은 복사 반입을 쓴다.
