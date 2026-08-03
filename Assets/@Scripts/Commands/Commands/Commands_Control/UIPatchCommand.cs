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
    private readonly UIManager _uiManager;
    private readonly UIPatchCommandSpec _spec;

    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;
    
    public UIPatchCommand(UIPatchService uiPatchService, UIManager uiManager, UIPatchCommandSpec spec)
    {
        _uiPatchService = uiPatchService;
        _uiManager = uiManager;
        _spec = spec;
    }


    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        var targetRoot = _uiManager.CurSceneRoot;
        UIContext context = new UIContext(_spec.themeId, _spec.localeId);

        yield return _uiPatchService.PatchUIInHierarchy(targetRoot, context);
    }
}