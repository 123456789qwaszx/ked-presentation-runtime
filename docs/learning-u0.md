# U0 — Unity 학습 모드 실행

작업 브랜치: `learning/vn-play-analytics`  
기준: `server_DB_Test` / `5ea9d8f9c5f80044a36c6f19ab2f424a1f8632eb`

Java·Spring 학습 단계에 필요한 Unity 변경은 이 브랜치에 누적한다. 이번 구현 범위는 U0이다. 서버 API는 U1 이후 서버 학습 진도에 맞춰 연결한다.

## 반영한 구성

- `PresentationSample.unity`의 Learning Mode를 켜고 Learning Chapter Json에 `qwer_scene.progression.json`을 연결했다.
- 일반 모드의 progression 참조는 유지한다. Learning Mode를 끄고 재실행하면 일반 모드로 돌아간다.
- 실행 시작 시 모드·챕터·저장 경로를 고정한다. Play 중 Inspector를 바꿔도 실행 구성을 바꾸지 않는다.
- 저장·앨범은 `Application.persistentDataPath/saves-learning`을 사용한다. 기존 `saves`와 `account.json`은 학습 모드에서 사용하지 않는다.
- 학습 모드는 기존 계정·버전·동기화·복구 객체를 만들지 않는다. 기존 Server Base Url에 값이 있어도 호출하지 않는다.
- 실제 `SaveCoordinator`의 로컬 저장 성공 뒤 `LearningProgressionReporter`가 확정 snapshot을 읽고 로그를 남긴다.
- 수동 슬롯 메뉴·저장/로드 단축키·완료된 과거 장면의 백로그 포크를 제한한다. 현재 장면의 롤백, 새 게임, 로컬 이어하기는 유지한다.
- 진행/저장을 거치지 않는 Yarn 단독·노드 사슬 디버그 실행은 학습 모드에서 제한한다.

## 실행

1. GitHub Desktop에서 Fetch 후 `learning/vn-play-analytics` 브랜치로 전환한다.
2. Unity에서 `Assets/Scenes/PresentationSample.unity`를 연다.
3. Play를 누르고 Console에서 `[저장] 경로: .../saves-learning`, `[학습] 로컬 저장 모드`를 확인한다.
4. 타이틀의 새 게임을 누른다. `[진행] 사전 대조 통과`와 `[학습] SceneEntered — qwer_scene/EP01`을 확인한다.

이 브랜치에서는 필요한 Inspector 참조도 저장되어 있다. 미저장 로컬 씬 변경이 있다면 원격 씬 설정과 다를 수 있다.

## 관찰 순서와 기대 결과

| 조작 | 저장된 재개 Episode | 확정 Scene 수 | 설명 |
| --- | --- | --- | --- |
| 새 게임으로 EP01 진입 | EP01 | 0 | 초기 snapshot 생성 |
| 성실 선택 후 EP02_01 진행 | EP01 | 0 | 아직 사무실 안의 pending |
| 복도로 이동 | EP03 | 1 | EP01[0], EP02_01[0]을 함께 확정 |
| EP03에서 자동으로 EP04 이동 | EP03 | 1 | 같은 복도 장면이라 아직 미확정 |
| EP04 종료 | EP04 | 2 | 완료 표시 true. 마지막 경로는 자동 간선 EP03[0] |

`SceneEntered` 로그에도 마지막 확정 경로가 나올 수 있다. 이것은 새 선택이나 새 커밋이 아니라 현재 로컬 snapshot의 관찰이다. 이벤트 횟수를 로그 줄 수로 세지 않는다.

qwer_scene의 EventKey는 비어 있으므로 시청 이벤트가 0개인 것은 정상이다. 자동 간선은 진행 경로에는 있지만 이후 사용자 선택 통계에서는 제외한다. Via1 뒤에도 같은 사무실이므로 그 시점에는 커밋되지 않는다. 기존 샘플의 혼동되는 Via1 안내 대사도 실제 경계에 맞췄다.

## JSON 확인

Console에 나온 저장 폴더를 연다.

- `active.json`: 현재 회차 ID.
- `playthroughs-v3/{회차 ID}.json`: 로컬 envelope.
- 파일의 `snapshot`: 이후 학습 서버에 백업할 실제 복원 데이터.

Play 중 VNAppBootstrap 컴포넌트의 컨텍스트 메뉴에서 **Learning → Log current save snapshot**을 실행하면 envelope를 제외한 현재 snapshot JSON을 Console에서 볼 수 있다. 읽기만 하며 새 snapshot을 저장하지 않는다.

`sync`, `inFlight`, `localCommitVersion` 등은 로컬 envelope의 메타데이터다. 관찰 로그는 이 값을 ACK하거나 서버 저장 완료로 바꾸지 않는다.

## 직접 확인할 U0 체크리스트

- [ ] 컴파일 오류 없이 샘플 실행.
- [ ] 학습 저장 경로와 첫 SceneEntered 확인.
- [ ] 사무실 중간에서 Play 중단 → 다시 실행 → 이어하기로 EP01에서 시작.
- [ ] 성실 선택 후 복도 이동 → SceneCommitted 경로 두 개와 EP03 확인.
- [ ] 복도 중간에서 Play 중단 → 다시 실행 → 이어하기로 EP03에서 시작.
- [ ] 현재 장면의 이전 대사로 롤백한 뒤 다른 선택을 하면 최종 확정 경로에는 되돌리기 전 선택이 남지 않음.
- [ ] 과거 사무실 백로그를 보되 그 장면으로 포크할 수 없음. 현재 복도 내부 롤백은 가능.
- [ ] 수동 슬롯 메뉴·단축키가 학습 안내만 출력함.
- [ ] EP04 종료 snapshot의 chapterCompleted가 true.

완료된 저장에서 기존 Launcher의 이어하기를 누르면 새 회차로 시작한다. U0에서는 기존 동작을 관찰한다. U4의 서버 복구 UI에서는 완료 저장을 재개 대상으로 제공하지 않을 예정이다.

로컬 전용 유지보수는 활성 회차가 아닌 불필요한 과거 파일을 정리할 수 있다. 비교할 JSON은 새 게임 전에 확인한다. 자동 서버 전송과 과거 미전송 파일 보존은 U7에서 함께 다룬다.

## 자동 검증 범위

기존 SaveLifecycle 회귀 harness에 아래 관찰 경계 테스트를 추가했다.

- 첫 진입·장면 커밋이 실제 로컬 파일에 저장된 뒤 관찰되며, 기존 동기화 상태는 ACK되지 않음.
- 로컬 쓰기 실패를 장면 저장 성공으로 보고하지 않음.
- 관찰 로그 실패가 이미 성공한 저장과 챕터 완료를 취소하지 않음.

실행 명령:

```sh
dotnet run --project tests/SaveLifecycle/SaveLifecycle.csproj --configuration Release
```

Save lifecycle 워크플로가 이 학습 브랜치의 push에서도 실행되도록 했다. 이 harness는 저장·진행 보고 경계를 검증하며 실제 Unity 씬 컴파일·렌더링·키 입력을 대신하지 않는다. U0 완료 판정은 위의 직접 플레이 체크를 거친 뒤 한다.
