public enum PresentationLanePhase
{
    Stopped,
    Running,
    Completed,
}

public enum PresentationLaneGate
{
    /// <summary>
    /// Side lane이 현재 line 내부에 있거나,
    /// 아직 다음 line을 받을 준비가 되지 않은 상태.
    /// </summary>
    Blocked,

    /// <summary>
    /// Side lane이 자연스럽게 RequestNextLine을 받을 수 있는 상태.
    /// </summary>
    Ready,

    /// <summary>
    /// 현재 side line이 entry completion 전에 rollback/stop/request-next 등으로 뜯겨나간 상태.
    /// Main wait은 풀 수 있지만, advance dispatch를 소비하면 안 된다.
    /// </summary>
    Released,
}