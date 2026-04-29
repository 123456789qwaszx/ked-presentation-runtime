using System;
using System.Collections;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Dialogue", "Set Dialogue Text", Order = -690)]
public sealed class SetDialogueBoxTextCommandSpec : CommandSpecBase
{
    [Header("Identity")]
    public string dialogueKey = "main";

    [Header("Text")]
    public string bodyText = "";
    public bool setNameText = false;
    public string nameText = "";

    [Header("Options")]
    public bool strict = true;
}

public sealed class SetDialogueBoxTextCommand : CommandBase
{
    private readonly SetDialogueBoxTextCommandSpec _spec;

    private PresentationDialogueBoxView _view;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => true;

    public SetDialogueBoxTextCommand(SetDialogueBoxTextCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Apply(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        Apply(scope);
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        Apply(scope);
    }

    private void Apply(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        _view.LineText.text = _spec.bodyText;

        if (_spec.setNameText)
        {
            if (_view.NameText == null)
            {
                if (_spec.strict)
                    throw new InvalidOperationException($"[SetDialogueBoxTextCommand] NameText missing. dialogueKey={_spec.dialogueKey}");
                return;
            }

            _view.NameText.text = _spec.nameText;
        }
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetDialogueBoxView(_spec.dialogueKey, out PresentationDialogueBoxView view))
        {
            if (_spec.strict)
                throw new InvalidOperationException($"[SetDialogueBoxTextCommand] DialogueBox view not found. dialogueKey={_spec.dialogueKey}");
            return;
        }

        _view = view;
        _view.Validate();
    }
}