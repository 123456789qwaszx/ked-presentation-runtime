using System.Collections.Generic;
using Ked.Progression;

// 로드 복원의 Presentation 쪽 절반.
//
// 진행 런타임에 들어가는 것은 ScenePathStep[]뿐이고,
// SavedLoadPlan의 YarnChoices와 라인 표적은 여기 staging된다.
//
// ⚠ 순서가 계약이다.
//     Host staging → Driver.Start(restorePath) → SceneProgress.TryRestorePath 성공 → BeginLoadReplay()
//   진행 경로 검증이 실패하면 BeginLoadReplay()가 불리지 않는다 = Yarn 선택도 시크도 복원하지 않는다.
public sealed class ProgressionReplayState : ISceneReplayState
{
    private readonly VNLinePresentationState _lineState;
    private readonly ChoiceHistory _choiceHistory;

    private IReadOnlyList<VNChoiceRecord> _stagedChoices;
    private SaveLineTarget _stagedTarget;

    public ProgressionReplayState(
        VNLinePresentationState lineState,
        ChoiceHistory choiceHistory)
    {
        _lineState = lineState;
        _choiceHistory = choiceHistory;
    }

    // 실행 시작 전에 Host가 준비한다. 로드가 아니면 둘 다 null이다.
    public void Stage(IReadOnlyList<VNChoiceRecord> yarnChoices, SaveLineTarget target)
    {
        _stagedChoices = yarnChoices;
        _stagedTarget = target;
    }

    public bool IsSeekingActive => _lineState.IsSeekingActive;

    public void BeginLoadReplay()
    {
        // 표적이 없으면 복원할 라인이 없다. Launcher가 restorePath를 null로 넘기므로
        // 정상 흐름에서는 여기 오지 않지만, 표적 없이 재생을 시작하지는 않는다.
        if (_stagedTarget == null)
            return;

        if (_stagedChoices != null)
            _choiceHistory.RestoreChoices(_stagedChoices);

        _lineState.BeginLoadSeek(
            _stagedTarget.NodeName,
            _stagedTarget.LineId,
            _stagedTarget.Occurrence);

        // 복원 입력은 새 실행의 첫 Scene에서 한 번만 쓴다.
        _stagedChoices = null;
        _stagedTarget = null;
    }

    public void ClearSeek() => _lineState.ClearSeek();
}
