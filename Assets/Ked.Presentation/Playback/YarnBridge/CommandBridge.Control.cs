public sealed partial class YarnCommandBridge
{
    // 줄의 대사창 표시는 줄 태그(#boxhide · #boxfadein …)가 정한다 — 커맨드로 끄고 켜지 않는다.
    private void SetSurfaceLayout(string presetKey) 
        => _dialogueBoxPresentation.SetSurfaceLayout(presetKey);
    
    private void ResetSurfaceLayout() 
        => _dialogueBoxPresentation.ResetSurfaceLayout();
}