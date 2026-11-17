using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    // 무대 스텝 — 여기까지 쌓인 명령을 대사 없이 실행한다. 대사창은 닫히고, 전경(pause)이 끝나면 Yarn이 이어 간다.
    // 큐를 앞지르지 않는다: 앞에 쌓인 명령을 이 스텝이 실행한다.
    private YarnTask RunStageStep()
        => _stageStep.RunStageStepAsync();

    // 줄의 대사창 표시는 줄 태그(#boxhide · #boxfadein …)가 정한다 — 커맨드로 끄고 켜지 않는다.
    private void SetSurfaceLayout(string presetKey) 
        => _dialogueBoxPresentation.SetSurfaceLayout(presetKey);
    
    private void ResetSurfaceLayout() 
        => _dialogueBoxPresentation.ResetSurfaceLayout();
}