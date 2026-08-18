using System;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private const float PauseFramesPerSecond = 24f;
    private const int FramePauseAliasMaxFrame = 48;
    
    // Animator-style frame wait aliases.
    // 24fps basis: <<24fr>> = 1 second.
    // Registered up to <<48fr>> = 2 seconds.
    // Longer or precise waits should use <<pause seconds>>.
    private void BindFramePauseAliases(DialogueRunner runner)
    {
        for (int i = 1; i <= FramePauseAliasMaxFrame; i++)
        {
            int frame = i;
            float seconds = frame / PauseFramesPerSecond;

            runner.AddCommandHandler(
                $"{frame}fr",
                () => EnqueueWaitSpec(seconds));
        }
    }
    
    private void EnqueueWaitSpec(float duration = 0.18f)
        => Collect(new WaitCommandSpec() { duration = duration });

    private void EnqueueUIPatchSpec(string themeId = "default")
        => Collect(new UIPatchCommandSpec { themeId = themeId });


    
    private void LogImmediate(string message)
    {
        Debug.Log($"[YarnCommandBridge] {message}");
    }

    private void EnqueuePresentationActorAliasSpec(
        string aliasSymbol,
        string targetKey)
        => Collect(new SetPresentationActorAliasCommandSpec()
        {
            aliasSymbol = aliasSymbol,
            targetKey = targetKey
        });
    
    
    private BackgroundRigTarget ParseBackgroundRigTargetOrDefault(string parentTarget)
    {
        if (Enum.TryParse(parentTarget, out BackgroundRigTarget parsedTarget))
            return parsedTarget;

        Debug.LogWarning(
            $"[YarnCommandBridge] Invalid BackgroundRigTarget '{parentTarget}'. " +
            "Fallback to Background_ObjectSlotRoot.");

        return BackgroundRigTarget.Background_ObjectSlotRoot;
    }
    
    private YarnTask HideDialogueBox()
    {
        return _dialogueBoxPresentation.HideCurrentAsync();
    }

    private YarnTask ShowDialogueBox()
    {
        return _dialogueBoxPresentation.ShowCurrentAsync();
    }
    
    // 대사창을 닫고 현재 대사창 상태도 버리는 것.
    // 이후 box_show를 하더라도 켤 대상이 없기에 무시됨.
    // 따라서 다음 대사가 나오면 "현재 열려있는 대사창이 없음"이므로 새로 FadeIn을 함.
    private void CloseDialogueBox()
    {
        _dialogueBoxPresentation.CloseAll();
    }
    
    private void SetSurfaceLayout(string presetKey)
    {
        _dialogueBoxPresentation.SetSurfaceLayout(presetKey);
    }

    private void ResetSurfaceLayout()
    {
        _dialogueBoxPresentation.ResetSurfaceLayout();
    }
}