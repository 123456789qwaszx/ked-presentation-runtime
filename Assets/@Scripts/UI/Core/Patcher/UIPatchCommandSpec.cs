using System;
using System.Collections;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "UI",
    "Patch UI",
    Order = 500)]
public sealed class UIPatchCommandSpec : CommandSpecBase
{
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

    public override bool WaitForCompletion => true;
    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        var targetRoot = UIManager.Instance.CurSceneRoot;
        UIContext context = new UIContext(_spec.themeId, _spec.localeId);

        yield return _uiPatchService.PatchUIInHierarchy(targetRoot, context);
    }
}