using System;
using System.Collections;
using UnityEngine;
using Yarn.Unity;

[Serializable]
[CommandMenuHint(
    "Sub Presentation", "Start Sub Presentation", Order = -910,
    Sets = new[]
    {
        CommandMenuSets.Presentation,
    },
    SetOrder = -910)]
public sealed class SubPresentationStartCommandSpec : CommandSpecBase
{
    [Header("Sub Yarn")]
    [Tooltip("Sub Presentation DialogueRunner에서 시작할 Yarn node name.")]
    public string nodeName;

    [Tooltip("true면 sub runner가 이미 실행 중일 때 Stop 후 다시 시작합니다.")]
    public bool restartIfRunning = true;
}

public sealed class SubPresentationStartCommand : CommandBase
{
    private readonly DialogueRunner _subPresentationRunner;
    private readonly SubPresentationStartCommandSpec _spec;

    public override bool WaitForCompletion => false;

    public SubPresentationStartCommand(
        DialogueRunner subPresentationRunner,
        SubPresentationStartCommandSpec spec)
    {
        _subPresentationRunner = subPresentationRunner;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Apply();
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope) => Apply();
    protected override void OnRollbackSeek(CommandRunScope scope) => Apply();

    private void Apply()
    {
        if (_subPresentationRunner == null)
        {
            Debug.LogWarning("[SubPresentationStartCommand] subPresentationRunner is null.");
            return;
        }

        if (_spec == null)
        {
            Debug.LogWarning("[SubPresentationStartCommand] spec is null.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_spec.nodeName))
        {
            Debug.LogWarning("[SubPresentationStartCommand] nodeName is null or empty.");
            return;
        }

        if (_subPresentationRunner.IsDialogueRunning)
        {
            if (!_spec.restartIfRunning)
                return;

            _subPresentationRunner.Stop();
        }

        _subPresentationRunner.StartDialogue(_spec.nodeName);
    }
}