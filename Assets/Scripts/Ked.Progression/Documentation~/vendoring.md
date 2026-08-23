# 이 폴더는 복사 반입본이다

`Ked.Progression`은 **다른 저장소가 원본**이다. 여기 있는 것은 그 저장소의
`Runtime/`을 그대로 옮겨 놓은 한 벌이다.

```
원본   ked-progression      (형제 폴더 · 자체 dotnet 테스트 · 자체 태그)
반입   Assets/Scripts/Ked.Progression/   ← 이 폴더
```

`Assets/Scripts/Ked.Presentation.Core`가 저작 도구(VnTool)에 `src/Ked.Presentation.Core`로
복사 반입돼 있는 것과 같은 방식이다. **다만 방향이 반대다** — 연출 코어는 여기가 원본이고,
진행 코어는 여기가 사본이다.

## 스냅샷

| | |
|---|---|
| 원본 커밋 | `ab30af3` (`chore: 0.2.0`) |
| 원본 태그 | `0.2.0` |
| 반입일 | 2026-08-23 |

**갈리면 이 표를 고친다.** 표가 낡으면 사본이 어디서 왔는지 아무도 모른다.

## 왜 UPM 패키지가 아닌가

`0.2.0` 태그를 git URL로 물리는 방식으로 한 번 세웠고 실제로 섰다
(`feat/progression-driver` 브랜치 `5b3aa2c8`). 그런데 진행 코어는 지금 **매일 바뀌는 중**이라,
한 줄 고칠 때마다 커밋 → 태그 → 푸시 → 유니티 재해결이 붙는다.
그 왕복이 지금 작업의 가장 큰 비용이다.

복사 반입은 그 왕복을 0으로 만든다. 대신 **갈림을 사람이 지킨다** — 아래 대조를 쓴다.

## 대조와 반입

원본과 갈렸는지 본다(`.artifacts`·`obj`가 없어야 정확하다):

```bash
diff -r -x "*.csproj" -x "*.csproj.meta" \
  ../ked-progression/Runtime \
  Assets/Scripts/Ked.Progression
```

원본 → 사본으로 다시 받는다:

```bash
rm -rf Assets/Scripts/Ked.Progression/{Flow,Loading,Reachability,Save,Spec,State,Transition,Vocabulary}
cp -r ../ked-progression/Runtime/. Assets/Scripts/Ked.Progression/
rm -f Assets/Scripts/Ked.Progression/Ked.Progression.csproj*
```

여기서 고친 것은 **원본 저장소에도 같은 손을 대야 한다.** 원본에는 dotnet 테스트가
붙어 있고, 그것이 이 코드의 유일한 자동 검증이다 — 유니티 쪽엔 이 층의 테스트가 0이다.

## 안 가져온 것

| | 왜 |
|---|---|
| `Tests/` | 15파일 중 7파일이 `System.Text.Json`을 쓴다. 유니티에 그 어셈블리가 없어 **컴파일이 깨진다.** 패키지 시절엔 `testables`에 안 넣어서 조용했지만, `Assets/` 안에서는 `UNITY_INCLUDE_TESTS`가 켜져 그대로 컴파일된다 |
| `Ked.Progression.csproj` | 유니티가 자기 프로젝트 파일을 따로 만든다. 두면 IDE와 루트 `.sln`이 둘 다 집는다 |
| `Directory.Build.props` · `package.json` · `CHANGELOG.md` · `docs/` | 패키지 표면. 사본에서는 뜻이 없다 |

`.meta`는 **전부 원본에서 그대로 가져왔다.** 원본이 `.meta`를 커밋해 두었기에
GUID가 두 저장소에서 같다 — 나중에 다시 패키지로 돌아가더라도 참조가 안 끊긴다.
