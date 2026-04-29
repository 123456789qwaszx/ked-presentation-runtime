using System;
using System.Collections;

[Serializable]
[CommandMenuHint(
    "Presentation", "@Setup Presentation View", Order = -995)]
public sealed class SetupPresentationViewCommandSpec : CommandSpecBase
{
    public bool strict = true;
}

public sealed class SetupPresentationViewCommand : CommandBase
{
    private readonly PresentationViewAccess _access;
    private readonly SetupPresentationViewCommandSpec _spec;

    public override bool WaitForCompletion => true;

    public SetupPresentationViewCommand(
        PresentationViewAccess access,
        SetupPresentationViewCommandSpec spec)
    {
        _access = access;
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
            if (_spec.strict)
                throw new System.InvalidOperationException("[SetupPresentationView] PresentationUIRoot not found.");
            return;
        }

        scope.Presentation = _access.BuildRefs(root, _spec.strict);
    }
}