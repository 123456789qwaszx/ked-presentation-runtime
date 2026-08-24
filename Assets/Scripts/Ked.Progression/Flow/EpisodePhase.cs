namespace Ked.Progression
{
    /// <summary>
    /// 한 에피소드 트랜잭션이 <b>멈추는</b> 자리들. 전부 호스트가 무언가 해야 해서 흐름이
    /// 선 자리이고, 그때마다 <see cref="FlowRequest"/>가 채워진다.
    ///
    /// <b>한 호출 안에서 덮어써지는 중간 자리는 두지 않는다.</b> 호스트는 호출과 호출
    /// 사이에만 이 값을 볼 수 있어서, 그런 자리는 크래시 로그 한 줄 말고는 쓸 데가 없었다.
    ///
    /// 번호는 주 흐름이 10단위, 곁가지가 +1, 종료가 900대다. 사이에 자리를 넣을 수 있게
    /// 비워 둔 것이고, 값 자체에 뜻은 없다.
    /// </summary>
    internal enum EpisodePhase
    {
        None = 0,

        /// <summary>
        /// 상태가 이 에피소드를 가리킨다. 아직 아무것도 그리지 않았다.
        /// <b>멈춘다</b> — 대사를 재생하라고 부탁한다.
        /// </summary>
        EpisodeEntered = 10,

        /// <summary>고를 수 있는 것이 있다. <b>멈춘다</b> — 그리고 기다린다.</summary>
        AwaitingChoice = 40,

        /// <summary>
        /// 고른 간선에 <c>ViaNodeId</c>가 있다. <b>멈춘다</b> — 지나며 거쳐 갈 연출이다.
        /// 비어 있으면 이 자리를 건너뛴다.
        /// </summary>
        ViaPlaying = 50,

        /// <summary>
        /// <see cref="ProgressionState.Commit"/>이 끝났다. 스탯 반영과 이동이 한 번에 일어났다.
        ///
        /// <b>트랜잭션 경계이자 저장 경계다.</b> 여기서 굽기 때문에 "스탯만 오르고 안 옮겨 간
        /// 세이브"가 존재할 수 없다. <b>멈춘다</b> — 파일로 쓰라고 부탁한다.
        /// </summary>
        Committed = 60,

        /// <summary>
        /// 챕터 경계를 넘었다. 다음 챕터의 시작 에피소드에 서 있고 <b>스탯은 그 챕터의
        /// 초기값에서 다시 섰다</b>. <b>멈춘다</b> — 경계도 저장 자리다.
        /// </summary>
        ChapterBoundaryCommitted = 81,

        /// <summary>엔딩에 도달했고 그 길이 종착이다. <b>의도한 끝이다.</b></summary>
        ScenarioFinished = 900,

        /// <summary>
        /// 엔딩키가 없는 노드에서 멈췄다 — 나가는 길도 없고 엔딩도 아니다.
        /// <b>미완성이지 끝이 아니다.</b> 섞으면 화면에서 구별되지 않는다.
        /// </summary>
        DeadEnd = 901,
    }
}
