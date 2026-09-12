# Offline Local Save 리팩터링 PLAN

기준: learning/vn-play-analytics @ 7d17779d89f17f9182250cb4efd7e7ea996f8cb1
작업: refactor/offline-local-save

## 목표와 결정
- 인터넷 및 Spring 서버 없이 새 게임, 이어하기, 수동 슬롯 생성/덮어쓰기/로드, 현재 장면 rollback, 완료 장면 fork가 동작한다.
- 로컬 저장 성공이 게임 진행의 유일한 저장 조건이다. 통계 실패는 진행/저장/로드를 막지 않는다.
- 자체 서버 저장 revision, ACK/outbox, 충돌 fork, resume/bookmark HTTP 동기화를 제거한다.
- 사용자 fork와 자체 복원 가능한 수동 슬롯은 유지한다.
- Steam/Android 클라우드는 발매 후속 작업이다. 이번에는 SDK, 계정, 클라우드 포트를 구현하지 않는다.
- vn-play-analytics 서버는 변경하지 않는다. 클라이언트에는 선택적인 회차/선택 통계 연결만 남긴다.
- 기존 saves와 saves-learning 경로 및 v3 snapshot/슬롯을 보존한다. 서버 metadata는 읽을 때 무시하며 새 쓰기에서 제거한다. format version은 보존한다.
- 계정이나 설치 ID는 인증 수단이 아니다. 이번 통계 연결은 기존 개발 서버 계약이며 공개 서비스 인증 구현은 범위 밖이다.

## 순차 작업
1. R1: Bootstrap에서 콘텐츠/저장 경로 선택과 네트워크 사용을 분리. 수동 슬롯 및 완료 장면 이동 제한 해제. 기본 실행은 네트워크 없음.
2. R2: SaveCoordinator/Persistence/Model에서 서버 전용 상태와 동작 제거. 원자적 파일 쓰기, active, 사용자 fork, 슬롯 요약/본문 보존, 로컬 retention 유지.
3. R3: 학습 checkpoint 백업/복원/overlay 제거. 통계 연결을 인스턴스 기반 선택 기능으로 분리. 회차별 최신 snapshot, 전환 중 응답 구분, 지연 재시도. 영속 통계 outbox는 만들지 않는다.
4. R4: 기존 로컬 회귀 시나리오를 유지하고 v3 호환성 및 통계 실패/전환 시나리오 검증. 문서와 CI를 현재 범위에 맞춘다.

## 검증 기준
- 생성/commit 쓰기 실패 시 기존 디스크와 메모리 보존.
- 새 게임 첫 저장 전 기존 active 유지.
- 슬롯 덮어쓰기 실패 시 기존 본문 유지; 슬롯 로드 후 원본 유지.
- 부모 회차 없이 슬롯 복원; 완료 장면 fork의 경로/변수/시간 보존.
- 과거 v3의 sync metadata가 있어도 snapshot을 읽고 로컬 저장 가능.
- 통계 비활성 시 HTTP 객체 생성 없음. 활성 중 실패해도 로컬 기능 독립.
- A 전송 중 B로 이동해도 A 응답이 B를 바꾸지 않음. 실패한 이전 상태가 최신 대기 상태를 덮지 않음.
- Unity 에디터 실행은 이 환경에서 불가능하므로 자동 검증과 사용자 실행 체크를 구분하여 보고.

## Unity 수동 확인
서버를 종료한 상태에서 기존 학습 씬 실행 → 새 게임 → 장면 진행 → 수동 슬롯 저장/덮어쓰기 → 다른 진행 → 슬롯 로드 → 이전 완료 장면 이동 → 종료/재실행 → 이어하기. 기존 saves와 saves-learning 각각 확인. 통계 활성/서버 중단 중에도 같은 동작 확인.

## 진행 기록
- PLAN 작성 완료. 구현 전.
