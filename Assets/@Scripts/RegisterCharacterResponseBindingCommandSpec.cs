using System;
using System.Collections;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig", "Register Character Response Binding", Order = -997,
    Sets = new[]
    {
        CommandMenuSets.BuildChar,
    },
    SetOrder = -979)]
public sealed class RegisterCharacterResponseBindingCommandSpec : CommandSpecBase
{
    [Header("Rig")]
    [Tooltip("CharacterRigRegistry에 등록된 slotKey이거나, CastRegistry를 통해 slotKey로 해석 가능한 characterKey입니다.")]
    public string targetKey;

    [Tooltip("CharacterRig가 shot / pseudo camera intent에 반응하는 방식입니다.")]
    public PresentationResponseProfile responseProfile = PresentationResponseProfile.CharacterSlot;
}

public sealed class RegisterCharacterResponseBindingCommand : CommandBase
{
    private readonly PresentationResponseRig _responseRig;
    private readonly RegisterCharacterResponseBindingCommandSpec _spec;
    private readonly ICameraFocusStageRootProvider _stageRootProvider;

    public override bool WaitForCompletion => true;

    public RegisterCharacterResponseBindingCommand(
        RegisterCharacterResponseBindingCommandSpec spec,
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
        string resolvedRigKey = CharacterRigTargetResolver.ResolveRigKeyByPolicy(scope, _spec.targetKey);

        if (!scope.characterRigs.TryGetRig(resolvedRigKey, out CharacterRigRefs rigRefs))
            return;
        
        CharacterRigResponseTarget target = new CharacterRigResponseTarget(rigRefs);

        _responseRig.RegisterRuntimeBinding(
            _spec.targetKey,
            target,
            _spec.responseProfile,
            _stageRootProvider.StageRoot);
    }
}