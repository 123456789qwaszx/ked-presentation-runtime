# Offline Local Save 리팩터링

기준: `learning/vn-play-analytics` @ `7d17779d89f17f9182250cb4efd7e7ea996f8cb1`
작업: `refactor/offline-local-save`

## 확정한 범위

- 인터넷 없이 새 게임, 이어하기, 수동 저장/덮어쓰기/로드, 현재 장면 rollback, 완료 장면 fork가 동작한다.
- 로컬 파일이 진행 상태의 유일한 원본이다.
- 자체 서버 저장, 인증, revision, ACK/outbox, 충돌 처리, HTTP 복원 및 선택 통계를 런타임에서 제거한다.
- Steam/Android 클라우드는 로컬 저장 디렉터리를 플랫폼 저장소에 연결하는 발매 단계 작업으로 남긴다.
- `useLearningSaveData`는 콘텐츠와 저장 디렉터리만 선택한다.

## 최종 데이터 원칙

- 자동 저장은 `PlaythroughFile -> LocalSaveFile` 한 건이다.
- 수동 저장은 작은 `SaveSlotIndexFile`과 독립된 `SaveSlotFile -> SaveSlotData`로 나눈다.
- 수동 슬롯 본문은 원본 자동 저장 회차가 없어도 복원할 수 있다.
- 파일 형식 버전과 콘텐츠 호환 버전을 구분한다.
- `FormatVersion`은 JSON 구조를, `ContentVersion`은 대본과 선택 경로의 호환성을 판별한다.
- 복원에 영향을 주는 대본 변경 시 Bootstrap의 `saveContentVersion`을 올린다.
- 서버 동기화에서 쓰던 revision과 사용하지 않는 로컬 갱신 버전은 저장하지 않는다.
- 플레이 시간은 `PlaySeconds` 하나를 원본으로 저장한다.
- 모든 쓰기는 임시 파일을 완성한 뒤 교체한다. 슬롯은 본문을 먼저 쓰고 목록을 교체한다.
- 읽기 경계에서 필수 ID, 시간, 컬렉션, 체크포인트, 재생 경로와 버전을 검증한다.

## 호환성

- 기존 `playthroughs-v3` 회차 파일은 읽는다. 서버 metadata는 무시하고 다음 쓰기에서 제거한다.
- `ContentVersion` 도입 전 개발용 회차는 현재 콘텐츠 버전으로 한 번 승계한다.
- 기존 `bookmarks.json`과 `bookmark-snapshots`의 완전한 로컬 슬롯은 읽기 호환한다.
- 서버에만 본문이 있던 슬롯 요약은 로컬에서 복원할 수 없어 가져오지 않는다.
- 새 수동 저장은 `save-slots.json`과 `save-slot-data`에 기록한다.

## 자동 검증

- 원자적 commit 실패 시 디스크와 메모리 보존
- 손상되거나 지원하지 않는 회차 데이터 거부
- 콘텐츠 버전 불일치 자동 저장 거부
- 수동 슬롯 목록/본문 분리 및 원자적 덮어쓰기
- 슬롯 로드 후 원본 슬롯 보존
- 새 게임 첫 저장 및 active 포인터 실패 시 기존 이어하기 보존
- 완료 장면 fork의 선택 경로와 플레이 시간 보존
- 독립 슬롯을 유지하면서 비활성 자동 저장 정리
- 과거 서버 metadata 제거
- 중복 재생 전환 방지

## Unity 수동 확인

1. `saveContentVersion`에 현재 작품 버전을 지정한다.
2. 새 게임을 시작하고 장면을 완료한 뒤 종료/재실행하여 이어하기를 확인한다.
3. 장면 중 수동 저장, 덮어쓰기, 다른 진행 후 슬롯 불러오기를 확인한다.
4. 완료된 과거 장면으로 이동하고 새 선택이 별도 회차로 저장되는지 확인한다.
5. `saveContentVersion`을 바꾼 뒤 이전 자동 저장과 슬롯이 실행되지 않는지 확인한다.
6. `saves`와 `saves-learning` 디렉터리를 각각 확인한다.

Unity 에디터와 Steam/Android 실기기 검증은 별도로 수행한다.
