using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private const int FramePauseAliasMaxFrame = StageCommandParser.FramePauseAliasMaxFrame;
    
    // Animator-style frame wait aliases.
    // 24fps basis: <<24fr>> = 1 second.
    // Registered up to <<48fr>> = 2 seconds.
    // Longer or precise waits should use <<pause 시간>> (12fr · 0.5s · 0.5).
    private void BindFramePauseAliases(DialogueRunner runner)
    {
        for (int i = 1; i <= FramePauseAliasMaxFrame; i++)
        {
            int frame = i;

            string alias = $"{frame}fr";

            runner.AddCommandHandler(alias, () => Forward(alias));
        }
    }
    
    // 대사창 표시는 줄 태그(#boxhide · #boxfadein …)가 정한다 — 커맨드로 끄고 켜지 않는다.
    private void SetSurfaceLayout(string presetKey) 
        => _dialogueBoxPresentation.SetSurfaceLayout(presetKey);
    
    private void ResetSurfaceLayout() 
        => _dialogueBoxPresentation.ResetSurfaceLayout();
}