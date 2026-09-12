# U1 — 서버 콘텐츠 연결 확인

브랜치: learning/vn-play-analytics

## 실행

1. Spring 서버를 실행한다. Postman에서 GET http://localhost:8080/chapters?chapterKey=qwer_scene 응답을 확인한다.
2. Unity Play를 종료한 상태에서 브랜치를 Pull하고 컴파일 완료를 기다린다. Console의 컴파일 오류를 먼저 확인한다.
3. Assets/Scenes/PresentationSample.unity의 VNAppBootstrap에서 Learning Mode와 Learning Chapter Json을 확인한다.
4. Analytics Base Url은 학습 서버 주소다. 기본값은 http://localhost:8080이며 기존 Server Base Url과 별개다.
5. Play를 누르면 타이틀에서도 [U1 서버 콘텐츠] 연결 초기화 로그와 첫 장면 진입 대기 표시가 나온다.
6. 새 게임 또는 이어하기 후 조회 시작 GET 로그와 결과가 나온다.

성공 결과: HTTP 200, local chapterKey: qwer_scene, server chapterId와 title.

## 실패 구분

- 연결 초기화 로그 없음: 새 코드 컴파일 여부, 실행 중인 씬과 학습 모드, 초기화 도중 예외, Console 검색·로그 필터를 확인한다.
- 초기화는 있으나 조회 시작 없음: 새 게임/이어하기로 SceneEntered에 진입했는지 확인한다.
- 통신 실패: 주소·포트·서버 실행 상태를 확인한다.
- HTTP 오류: 상태 코드와 errorCode/message를 확인한다.
- HTTP 200 / 서버에 등록되지 않음: 해당 키의 콘텐츠 등록 여부를 확인한다.
- 화면 표시 실패: 오류는 Console에 남고 HTTP 요청은 계속된다.

Play 중 VNAppBootstrap 컨텍스트 메뉴의 Learning → Retry chapter lookup으로 다시 조회한다.
진행 중인 요청은 중복 실행하지 않는다. 주소 변경은 Play 종료 후 적용한다.
Spring의 일반 로그 설정에 따라 요청마다 접근 로그가 출력되지 않을 수 있으므로 Unity의 HTTP 결과와 Postman 응답으로 확인한다.

## 변경 이유와 검증 범위

기존 코드에서는 첫 화면 생성이 조회 메서드의 try 밖에 있었고 요청 시작 로그가 없었다.
표시 예외가 발생하면 HTTP 이전에 Task가 실패해 결과 로그도 나오지 않을 수 있었다.
이 경로가 사용자 환경의 실제 원인이었는지는 아직 확인되지 않았다.

Bootstrap에서 학습 연결을 명시적으로 생성하고 초기화 상태를 출력한다.
LearningProgressionReporter는 로컬 저장 후 콜백만 호출하며, HTTP와 표시는 LearningAnalyticsConnection이 맡는다.
화면 표시 오류는 별도로 처리해서 요청과 Console 진단을 유지한다.

SaveLifecycle harness는 실제 저장 후 콜백 실행, 저장 실패 시 미호출, 연결 실패 시 로컬 진행 보존을 검증한다.
Unity의 실제 화면·UnityWebRequest와 사용자 로컬 Spring 접속은 직접 플레이로 확인해야 한다.
