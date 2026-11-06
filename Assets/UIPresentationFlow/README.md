# UIPresentationFlow — UI 프레임워크 (반입본)

화면 · 패널 · 위젯의 흐름과 표시 규칙, 데이터 바인딩, 현지화를 맡는 UI 프레임워크(UIPF)다.
이 저장소에서는 **예제 UI만** 이것을 쓴다(`Samples/KedUI` · `Samples/KedUIApp`). 연출 런타임(`Ked.Presentation`)은 UIPF를 모른다.

> **이 폴더는 복사 반입본이다.** 원본은 형제 저장소 `UIPresentationFlow`다.
> 여기만 고치지 않는다 — 원본에서 고치고 다시 반입한다. 방법은 `Documentation~/vendoring.md`.

## 어셈블리 — 폴더 하나에 어셈블리 하나

| 폴더 | 어셈블리 | 하는 일 | 참조 |
|---|---|---|---|
| `Runtime/` | `UIPresentationFlow.Runtime` | 화면·패널 관리와 위젯·애니메이션(`UI/`), 표시 규칙·바인딩·스타일(`Presentation/`), 표시 상태(`Display/`), 현지화(`Localization/`), 셰이더(`Shaders/`) | UGUI · TMP |
| `Editor/` | `UIPresentationFlow.Editor` | 화면 짓기 도구(`Authoring/`), 텍스트·이미지 바인딩 빌더 | Runtime · UGUI · TMP |

## 의존 방향

```
예제 UI(Ked.Samples.UI · Editor) ──> UIPresentationFlow.Runtime
                                  └──> UIPresentationFlow.Editor ──> Runtime
```
