using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    // 대기는 <<pause 시간>>(무대 파서 표)이다. 옛 프레임 별칭 <<1fr>>~<<48fr>>과 debug_log는
    // 2026-11-03에 걷었다 — 대본은 저작 툴이 내고, 툴은 정본만 낸다.

    private YarnTask HideDialogueBox() 
        => _dialogueBoxPresentation.HideCurrentAsync();
    
    private YarnTask ShowDialogueBox() 
        => _dialogueBoxPresentation.ShowCurrentAsync();
    
    // 대사창을 닫고 현재 대사창 상태도 버리는 것.
    // 이후 box_show를 하더라도 켤 대상이 없기에 무시됨.
    // 따라서 다음 대사가 나오면 "현재 열려있는 대사창이 없음"이므로 새로 FadeIn을 함.
    private void CloseDialogueBox() 
        => _dialogueBoxPresentation.CloseAll();
    
    private void SetSurfaceLayout(string presetKey) 
        => _dialogueBoxPresentation.SetSurfaceLayout(presetKey);
    
    private void ResetSurfaceLayout() 
        => _dialogueBoxPresentation.ResetSurfaceLayout();
}