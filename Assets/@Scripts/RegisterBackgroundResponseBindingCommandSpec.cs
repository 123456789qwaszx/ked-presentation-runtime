using System;
using System.Collections;
using UnityEngine;

public enum PresentationResponseStage
{
    Stage00 = 0,
    Stage01 = 1,
    Stage02 = 2
}

[Serializable]
[CommandMenuHint(
    "Background Rig", "Register Background Response Binding", Order = -997,
    Sets = new[]
    {
        CommandMenuSets.SetupBackground,
    },
    SetOrder = -979)]
public sealed class RegisterBackgroundResponseBindingCommandSpec : CommandSpecBase
{
    [Header("Rig")]
    [Tooltip("BackgroundRigRegistry에 등록된 rigKey입니다.")]
    public string rigKey;

    [Header("Stage Root")]
    [Tooltip("basePositionInRigSpace와 focusPoint 계산 기준이 되는 StageRoot입니다.")]
    public PresentationResponseStage stage = PresentationResponseStage.Stage00;

    [Tooltip("BackgroundRig가 shot / pseudo camera intent에 반응하는 방식입니다.")]
    public PresentationResponseProfile responseProfile = PresentationResponseProfile.Background;
}

public sealed class RegisterBackgroundResponseBindingCommand : CommandBase
{
    private readonly PresentationResponseRig _responseRig;
    private readonly RegisterBackgroundResponseBindingCommandSpec _spec;

    public override bool WaitForCompletion => true;

    public RegisterBackgroundResponseBindingCommand(
        PresentationResponseRig responseRig,
        RegisterBackgroundResponseBindingCommandSpec spec)
    {
        _responseRig = responseRig;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Apply(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope) => Apply(scope);
    protected override void OnRollbackSeek(CommandRunScope scope) => Apply(scope);

    private void Apply(CommandRunScope scope)
    {
        string rigKey = _spec.rigKey;
        
        if (!scope.backgroundRigs.TryGetRig(rigKey, out BackgroundRigRefs rigRefs))
            return;

        RectTransform stageRoot = ResolveStageRoot(_spec.stage);

        BackgroundRigResponseTarget target = new BackgroundRigResponseTarget(rigRefs);

        _responseRig.RegisterRuntimeBinding(
            _spec.rigKey,
            target,
            _spec.responseProfile,
            stageRoot);
    }

    private static RectTransform ResolveStageRoot(PresentationResponseStage stage)
    {
        ICameraFocusStageRootProvider provider =
            UIManager.Instance.GetUI<PresentationUIRoot>();

        if (provider == null)
            return null;

        switch (stage)
        {
            case PresentationResponseStage.Stage00:
                return provider.Stage00Root;

            case PresentationResponseStage.Stage01:
                return provider.Stage01Root;

            case PresentationResponseStage.Stage02:
                return provider.Stage02Root;

            default:
                return provider.Stage00Root;
        }
    }
}