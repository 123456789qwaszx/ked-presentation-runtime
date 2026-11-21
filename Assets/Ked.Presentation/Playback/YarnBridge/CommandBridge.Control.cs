public sealed partial class YarnCommandBridge
{
    // <<step>>(옛 표기) — 여기까지 쌓인 명령을 다음 대사 줄의 앞 앵커 하나로 닫는다. 실행하지 않는다 — Yarn은 그대로 이어 간다.
    // 그 줄이 도착한 뒤에 제 것으로 실행한다(StageStepFlow.RunLinePreludeAsync). 붙을 줄이 없으면 노드 끝의 암묵 스텝이 실행한다.
    private void CloseStageStep()
        => _playbackDriver.CloseStep();

    // 줄의 대사창 표시는 줄 태그(#boxhide · #boxfadein …)가 정한다 — 커맨드로 끄고 켜지 않는다.
    private void SetSurfaceLayout(string presetKey) 
        => _dialogueBoxPresentation.SetSurfaceLayout(presetKey);
    
    private void ResetSurfaceLayout() 
        => _dialogueBoxPresentation.ResetSurfaceLayout();
}