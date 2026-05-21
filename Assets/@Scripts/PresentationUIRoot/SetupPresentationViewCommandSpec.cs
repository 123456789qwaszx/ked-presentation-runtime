using System;
using System.Collections;

[Serializable]
[CommandMenuHint(
    "Presentation", "@Setup Presentation View", Order = -995)]
public sealed class SetupPresentationViewCommandSpec : CommandSpecBase
{ }

public sealed class SetupPresentationViewCommand : CommandBase
{
    private readonly PresentationResponseRig _responseRig;
    private readonly SetupPresentationViewCommandSpec _spec;

    public override bool WaitForCompletion => true;

    public SetupPresentationViewCommand(PresentationResponseRig responseRig, SetupPresentationViewCommandSpec spec)
    {
        _responseRig = responseRig;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Bind(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope) => Bind(scope);
    protected override void OnRollbackSeek(CommandRunScope scope) => Bind(scope);

    private void Bind(CommandRunScope scope)
    {
        PresentationUIRoot root = UIManager.Instance.GetUI<PresentationUIRoot>();
        if (root == null)
        {
            return;
        }
        //***
        //_responseRig.BindCameraRoots(scope.Presentation.StagePan_Root, scope.Presentation.StageZoom_Root);
        
        ResetSlantedMasks(root);
    }
    
    #region slantedMaskGroup
    
    private void ResetSlantedMasks(PresentationUIRoot root)
    {
        SlantedMaskResetGroup resetGroup = root.GetComponentInChildren<SlantedMaskResetGroup>(true);
        resetGroup?.ResetAllToHiddenOffset();
    }
    
    #endregion
}