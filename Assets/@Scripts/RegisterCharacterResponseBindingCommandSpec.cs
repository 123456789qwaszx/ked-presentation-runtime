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

    [Header("Stage Root")]
    [Tooltip("basePositionInRigSpace와 focusPoint 계산 기준이 되는 StageRoot입니다.")]
    public PresentationResponseStage stage = PresentationResponseStage.Stage00;

    [Tooltip("CharacterRig가 shot / pseudo camera intent에 반응하는 방식입니다.")]
    public PresentationResponseProfile responseProfile = PresentationResponseProfile.CharacterSlot;
}

public sealed class RegisterCharacterResponseBindingCommand : CommandBase
{
    private readonly PresentationResponseRig _responseRig;
    private readonly RegisterCharacterResponseBindingCommandSpec _spec;

    public override bool WaitForCompletion => true;

    public RegisterCharacterResponseBindingCommand(
        PresentationResponseRig responseRig,
        RegisterCharacterResponseBindingCommandSpec spec)
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
        string targetKey = _spec.targetKey;

        string resolvedRigKey = CharacterRigTargetResolver.ResolveRigKeyByPolicy(scope, targetKey);

        if (!scope.characterRigs.TryGetRig(resolvedRigKey, out CharacterRigRefs rigRefs))
            return;

        RectTransform stageRoot = ResolveStageRoot(_spec.stage);


        CharacterRigResponseTarget target = new CharacterRigResponseTarget(rigRefs);

        _responseRig.RegisterRuntimeBinding(
            _spec.targetKey,
            target,
            _spec.responseProfile,
            stageRoot);
    }

    private static RectTransform ResolveStageRoot(PresentationResponseStage stage)
    {
        ICameraFocusStageRootProvider provider = UIManager.Instance.GetUI<PresentationUIRoot>();

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