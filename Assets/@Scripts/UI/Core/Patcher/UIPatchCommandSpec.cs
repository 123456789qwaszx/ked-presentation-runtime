using System;
using System.Collections;
using UnityEngine;

public enum UIPatchTargetKind
{
    CurrentRoot = 0,
}

[Serializable]
[CommandMenuHint(
    "UI",
    "Patch UI",
    Order = 500)]
public sealed class UIPatchCommandSpec : CommandSpecBase
{
    [Header("Target")]
    public UIPatchTargetKind targetKind = UIPatchTargetKind.CurrentRoot;

    [Header("Context")]
    public string themeId = "default";
    public string localeId = "ko-KR";
}


public sealed class UIPatchCommand : CommandBase
{
    private readonly UIPatchService _uiPatchService;
    private readonly UIPatchCommandSpec _spec;

    public UIPatchCommand(UIPatchService uiPatchService, UIPatchCommandSpec spec)
    {
        _uiPatchService = uiPatchService;
        _spec = spec;
    }

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (_uiPatchService == null)
            yield break;

        Component targetRoot = ResolveTargetRoot();
        if (targetRoot == null)
        {
            Debug.LogWarning($"[UIPatchCommand] Target UI not found. targetKind={_spec.targetKind}");
            yield break;
        }

        UIContext context = new UIContext(_spec.themeId, _spec.localeId);

        yield return _uiPatchService.PatchUIInHierarchy(targetRoot, context);
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        OnCommandCompleted(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
    }

    private Component ResolveTargetRoot()
    {
        switch (_spec.targetKind)
        {
            case UIPatchTargetKind.CurrentRoot:
                return ResolveCurrentRoot();
        }

        return null;
    }

    private static Component ResolveCurrentRoot()
    {
        return UIManager.Instance.CurSceneRoot;
    }
}