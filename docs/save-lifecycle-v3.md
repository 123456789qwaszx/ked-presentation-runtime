# 회차 저장·동기화 v3

작업 브랜치: `server_DB_Test`. 기준 커밋: `362bca438bae8cb13900ffeaca9caf8362af6132`.

## 범위와 결정

- [x] 회차 ID와 경로가 고정된 `PlaythroughSession`.
- [x] `LocalFileSaveStore`가 회차별 단일 session을 소유.
- [x] snapshot과 pending 이력을 한 envelope로 원자적 저장.
- [x] 전송 전 snapshot·batch·baseRevision·commitVersion을 함께 캡처하고 디스크에 보존.
- [x] 응답 유실 후에도 기존 work를 먼저 재전송. 새 커밋과 합치지 않음.
- [x] ACK는 최신 파일에 전송한 접두부와 커밋 번호만 반영.
- [x] 새 게임·북마크·장면 fork에서 네트워크 flush 제거.
- [x] 최초 장면 진입 snapshot을 저장한 다음 active 교체.
- [x] 회차 ID 큐를 사용하는 순차 worker. 활성·과거 회차를 같은 경로로 전송.
- [x] 409 이동 journal과 재시작 복구. 과거 회차의 충돌도 별도 fork로 보존.
- [x] 전환 중복 방지. 복구 대기 중 새 게임을 선택하면 이전 이어하기 요청 무효화.
- [x] 부분 복구의 완료 상태를 기록하고 다음 실행에서 재개.
- [x] 늦은 복구 응답은 로컬 진행과 명시적 active 선택을 덮지 않음.
- [x] 완료되거나 유효하지 않은 재개 지점은 별도 새 회차로 시작.
- [x] 기존 파일 마이그레이션은 사용자 요청에 따라 제외.

자동 삭제, catalog, lazy hydrate, 병렬 업로드, 주기적 네트워크 재시도는 이번 범위에 포함하지 않는다.
모든 회차 보관 정책을 유지한다. 기존 형식으로의 역호환이나 기존 로컬 저장 이전은 지원하지 않는다.

## 파일

`Application.persistentDataPath/saves` 아래:

- `playthroughs-v3/{id}.json`: `PlaythroughFile` envelope.
- `active.json`: active ID와 사용자 선택 세대.
- `conflict-transfer.json`: 미완료 409 이동. 완료 후 제거.
- `restore-progress.json`: 복구 시작/완료 및 자동 active 선택 허용 여부.
- `bookmarks.json`: 북마크, 서버 삭제 대기, 로컬 삭제 tombstone.

로컬 envelope는 서버에 업로드하지 않는다. 서버 `snapshot`은 기존 `LocalSaveFile` 모양이다.
`PlaythroughSyncState.PlaythroughId`는 서버의 숫자 ID이고, session의 `Id`는 로컬 문자열 ID이다.

## 소유권과 저장

`SaveCoordinator`는 진행 보고를 `LocalSaveFile + PendingChoice[] + PendingEvent[]`로 바꾼다.
`PlaythroughSession.Commit`은 사본을 만들어 seq 발급과 이력 추가를 수행하고 한 번에 저장한다.
쓰기 성공 이후에만 메모리를 바꾼다. 실패 시 caller에 예외가 전달되며 완료 처리하지 않는다.

Repository 및 worker는 Unity 메인 스레드에서 사용한다. 같은 저장 루트에 repository 두 개를
동시에 열어 수정하는 다중 프로세스/다중 인스턴스 접근은 지원하지 않는다.
Session의 gate는 같은 객체에 대한 캡처와 쓰기를 보호한다.

현재 `AtomicFile`의 temp + replace/move 방식을 사용한다. 테스트의 보장은 프로세스 중단 및
주입한 파일 쓰기 실패 경계이다. 디스크 고장·파일 손상·전원 차단의 모든 경우를 보장하지 않는다.

## 전송

1. `CaptureSyncWork`가 deep copy를 만들어 envelope의 `InFlight`에 기록한다.
2. 서버 회차 ID를 확보해 같은 session에 기록한다.
3. 콘텐츠 버전을 확보해 해당 work에 기록한다.
4. 고정된 work를 PUT한다.
5. 성공하면 그 work ID를 검증한 뒤, 전송한 pending 접두부만 제거한다.
6. `SyncedCommitVersion`은 전송한 번호까지만 올린다.

`LocalCommitVersion > SyncedCommitVersion`이면 snapshot-only 변경도 미동기화로 분류된다.
전송 중 커밋이 추가되면 ACK 이후 worker가 같은 회차를 다시 큐에 등록한다.
응답을 못 받으면 InFlight가 남아 재시작 후 같은 요청을 먼저 재시도한다.
기존 서버의 client GUID 멱등 생성 및 PUT 재전송 흡수 계약을 사용한다.

실패 ID는 같은 drain에서 즉시 반복하지 않는다. 다른 ID는 계속 시도한다.
재시도 기회는 앱 시작, 해당 회차의 후속 커밋, 명시적 `TrySyncAsync()`이다.
주기적 backoff/reconnect 트리거는 추후 추가할 수 있다.

## 전환

입력 경로는 `ProgressionLauncher.TransitionAsync`에서 Stop → 로컬 prepare → Launch를 수행한다.
동일 전환 중 들어온 요청은 무시한다. 네트워크 응답은 전환 경계에 포함하지 않는다.
준비 중 새 회차 ID가 메모리에만 있을 때는 이전 active 파일이 그대로 남는다.
첫 Scene 진입에서 초기 snapshot 저장에 성공한 뒤 active를 교체한다.

409 응답은 요청의 source ID로 처리한다. 현재 active가 source인 경우에만 active를 옮긴다.
새 게임을 예약해 선택 세대가 바뀌었거나 다른 회차가 active이면 기존 진행을 건드리지 않는다.
현재 Scene을 실행 중인 coordinator는 체크포인트를 유지하고 소유 session만 새 fork로 바꾼다.

## 409 이동의 중단 복구

1. source와 destination ID, destination 전체 내용, 선택 세대를 journal에 기록.
2. destination 파일을 먼저 작성. 이미 있으면 덮어쓰지 않음.
3. source에 `ReleasedTo`를 기록하고 이동한 pending을 제거.
4. 원래 active와 선택 세대가 여전히 같을 때만 active 교체.
5. journal 제거.

초기화와 후속 충돌 처리 전에 journal을 재실행한다. 같은 source는 같은 destination으로 수렴한다.
이동 기록 작성 이후 I/O 오류가 발생하면 source의 추가 Commit을 금지한다.
앱 재시작 시 복구를 먼저 수행한다. UI에서 I/O 오류를 사용자에게 안내하는 기능은 별도다.
baseRevision 0인 신규 슬롯까지 CONFLICT를 반환하면 자동 fork를 반복하지 않고 충돌 표시를 남긴다.

## 복구

초기 빈 저장소에서만 새 복구를 시작한다. 시작 기록이 남아 있으면 일부 로컬이 있어도 재개한다.
최신 회차부터 복구하고 처음 사용 가능한 회차를 active로 선택한다. active가 이미 있거나 사용자가
새 게임/fork를 명시적으로 선택했으면 뒤늦게 active를 바꾸지 않는다.
각 회차는 snapshot과 서버 sync 상태를 한 파일로 저장한다. 기존 로컬 파일은 덮지 않는다.
북마크도 개별 성공마다 저장하고, 로컬 변경·삭제를 보존한다.

복구 전체가 성공했을 때만 Completed를 기록한다. 네트워크/항목 실패는 다음 앱 시작에서 재시도한다.
이어하기는 필요한 초기 복구를 기다리지만, 명시적 새 게임은 기다리지 않는다.
옛 회차 전송과 북마크 재전송은 이어하기 startup 장벽 밖에서 실행한다.
회차 전체 목록/상세를 순차 복구하는 비용과 서버 pagination은 후속 과제로 남는다.

## 검증

명령:

```sh
dotnet run --project tests/SaveLifecycle/SaveLifecycle.csproj --configuration Release
```

Harness는 실제 Save 전체 소스, GuestSession, Launcher, 진행 데이터 모델을 컴파일한다.
Unity/Yarn 재생 실행과 HTTP 경계만 대역으로 교체한다. 실패하면 프로세스가 비정상 종료한다.
`server_DB_Test` push와 관련 PR에서 GitHub Actions가 같은 harness를 실행한다.

로컬 검증: .NET 8 Roslyn 직접 컴파일 및 .NET 런타임 실행으로 아래 17개 시나리오 통과.
현재 실행 환경의 dotnet CLI가 프로세스 정보를 읽지 못해 일반 CLI 대신 같은 SDK의 컴파일러를 사용했다.

- 읽기/목록 무쓰기 및 회차별 단일 session.
- Commit 단일 쓰기와 실패 시 디스크·메모리 보존.
- 캡처 deep copy 및 늦은 ACK가 후속 커밋 보존.
- snapshot-only dirty 처리.
- 응답 유실·재시작 후 동일 work 재전송 및 후속 커밋 처리.
- A 응답이 B를 ACK하지 않음.
- A 실패가 B를 막거나 반복하지 않음.
- 409 이동의 각 쓰기 경계 실패 후 복구.
- 비활성 409가 현재 active를 변경하지 않으며 이동 재실행이 destination을 덮지 않음.
- A 전송 중 새 게임 준비 즉시 완료와 초기 Scene 저장.
- 부모 파일 없는 북마크의 독립 fork.
- Launcher 중복 전환 및 오래된 이어하기 요청 무효화.
- 활성 409 후 현재 Scene 유지와 이후 커밋.
- 신규 슬롯의 반복 충돌에서 무한 fork 방지.
- 완료된 재개 지점의 회차 ID 분리.
- 부분 복구 재개와 기존 로컬 진행 보존.
- 늦은 복구가 명시적 새 게임 active를 덮지 않음.

Unity Editor 및 실제 Spring 서버 연동은 이 환경에서 실행하지 않았다. 다음 실물 확인이 필요하다.

- [ ] 서버를 끈 상태로 새 게임/북마크/백로그 fork 실행.
- [ ] 전송 지연 중 회차를 바꾸고 이전 응답이 도착한 뒤 새 회차 진행 확인.
- [ ] 두 기기로 같은 서버 revision을 변경해 실제 409 fork 및 재전송 확인.
- [ ] PUT 성공 응답을 유실시켜 서버의 재전송 흡수 확인(선택 없는 snapshot 포함).
- [ ] 새 기기 복구 도중 새 게임을 시작하고 active 유지 확인.
- [ ] 초기 Scene과 장면 커밋, 북마크 seek를 Unity에서 재생 확인.

콘텐츠 버전 호환성/마이그레이션, 북마크 PUT 간 경합 정책, 손상 파일 복구와 저장 오류 UI는
이번 회차 소유권 개선과 별개의 후속 작업이다. 상용 출시 검증 완료를 의미하지 않는다.
