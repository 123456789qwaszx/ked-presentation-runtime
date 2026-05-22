using System;
using System.Collections;
using UnityEngine;


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

    [Tooltip("BackgroundRig가 shot / pseudo camera intent에 반응하는 방식입니다.")]
    public PresentationResponseProfile responseProfile = PresentationResponseProfile.Background;
}

public sealed class RegisterBackgroundResponseBindingCommand : CommandBase
{
    private readonly PresentationResponseRig _responseRig;
    private readonly RegisterBackgroundResponseBindingCommandSpec _spec;
    private readonly ICameraFocusStageRootProvider _stageRootProvider;

    public override bool WaitForCompletion => true;

    public RegisterBackgroundResponseBindingCommand(
        RegisterBackgroundResponseBindingCommandSpec spec,
        PresentationResponseRig responseRig,
        ICameraFocusStageRootProvider cameraFocusStageRootProvider)
    {
        _responseRig = responseRig;
        _spec = spec;
        _stageRootProvider = cameraFocusStageRootProvider;
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
        if (!scope.backgroundRigs.TryGetRig(_spec.rigKey, out BackgroundRigRefs rigRefs))
            return;

        BackgroundRigResponseTarget target = new (rigRefs);

        _responseRig.RegisterRuntimeBinding(
            _spec.rigKey,
            target,
            _spec.responseProfile,
            _stageRootProvider.StageRoot);
    }
}