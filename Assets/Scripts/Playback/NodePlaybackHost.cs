using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 연출 단독 예제의 재생 호스트. 재생 단위는 Yarn 노드 하나다.
///
/// 진행·저장을 모른다. 노드 하나를 처음부터 끝까지 돌리고, 리플레이 요청이 오면
/// 무대를 비우고 같은 노드를 다시 시작한다. 표적 라인까지는 시크가 데려간다.
/// 노드 안의 jump·detour는 Yarn이 처리한다 — 이 호스트는 시작 노드 이름만 안다.
///
/// 리플레이 순서는 진행 층의 SceneRunner와 같다.
/// 멈춤 요청 → 노드 종료 → 멈춤 완료 대기 → 무대 준비 → 같은 노드 재시작.
///
/// 예전 디버그 러너(ScenePlaybackDebugRunner, 719e7b8에서 제거)와 달리 진행 런처와 공존하지 않는다.
/// 한 실행에 호스트는 하나다(PresentationRuntime.AttachHost).
/// </summary>
public sealed class NodePlaybackHost : IPlaybackHost
{
    private readonly ScenePresentation _presentation;
    private readonly BacklogRecorder _backlog;
    private readonly RollbackHistory _rollbackHistory;

    // 리플레이 요청과 재생 루프가 같은 멈춤 완료를 기다린다.
    private Task _stopTask = Task.CompletedTask;

    // 재생 구간(시작 또는 리플레이 재시작부터)마다 멈춤은 한 번만 건다.
    // 끝내기 요청 뒤 finally가 다시 멈추면 스코프 종료가 두 번 불린다.
    private bool _stopIssued;

    private bool _replayRequested;
    private bool _exitRequested;

    public NodePlaybackHost(
        ScenePresentation presentation,
        BacklogRecorder backlog,
        RollbackHistory rollbackHistory)
    {
        _presentation = presentation;
        _backlog = backlog;
        _rollbackHistory = rollbackHistory;
    }

    public bool IsRunning { get; private set; }

    public async Task PlayAsync(string nodeName)
    {
        if (string.IsNullOrWhiteSpace(nodeName))
        {
            Debug.LogError("[노드 재생] 재생할 노드 이름이 비어 있다.");
            return;
        }

        if (IsRunning)
        {
            Debug.LogWarning($"[노드 재생] 이미 재생 중이라 '{nodeName}' 요청을 거부한다.");
            return;
        }

        IsRunning = true;
        _replayRequested = false;
        _exitRequested = false;

        bool finished = false;

        try
        {
            // 이전 실행이 남긴 무대를 먼저 정리한다 — SceneRunner.EnterSceneAsync와 같은 순서.
            _stopIssued = false;
            await StopPlaybackAsync();

            // 정리하는 사이 끝내기 요청이 왔다면 시작하지 않는다.
            if (_exitRequested)
                return;

            _stopIssued = false;

            _presentation.BeginScene();
            _backlog.MarkSceneBoundary();

            while (true)
            {
                _replayRequested = false;

                await _presentation.PlayEpisodeAsync(nodeName);

                if (_exitRequested || !_replayRequested)
                    break;

                // RequestReplayAsync가 시작한 멈춤이 끝날 때까지 기다린다.
                await _stopTask;

                _presentation.PrepareReplay();
                _stopIssued = false;

                // 표적 라인은 시크가 이미 들고 있다. 노드 하나에는 되돌릴 진행 색인이 없으므로
                // 표적 표시만 소비한다(SceneRunner는 여기서 진행을 표적 색인으로 되돌린다).
                _rollbackHistory.TakeRollbackTarget(out _);
            }

            finished = true;

            if (!_exitRequested)
                Debug.Log($"[노드 재생] '{nodeName}' 끝.");
        }
        finally
        {
            // 끝까지 간 노드는 마지막 화면을 그대로 둔다 — 진행 층이 챕터 끝에서 하는 것과 같다.
            // 끝내기 요청이나 예외로 끊긴 경우만 멈춤을 마무리한다.
            if (!finished || _exitRequested)
                await StopPlaybackAsync();

            IsRunning = false;
        }
    }

    public Task RequestReplayAsync()
    {
        if (!IsRunning || _replayRequested || _exitRequested)
            return Task.CompletedTask;

        _replayRequested = true;

        return StopPlaybackAsync();
    }

    public Task ExitAsync()
    {
        if (!IsRunning)
            return Task.CompletedTask;

        _exitRequested = true;

        return StopPlaybackAsync();
    }

    private Task StopPlaybackAsync()
    {
        if (_stopIssued)
            return _stopTask;

        _stopIssued = true;
        _stopTask = _presentation.StopAsync();
        return _stopTask;
    }
}
