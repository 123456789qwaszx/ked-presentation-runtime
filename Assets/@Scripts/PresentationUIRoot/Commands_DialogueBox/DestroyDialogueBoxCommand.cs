using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
[CommandMenuHint("Presentation Dialogue", "Destroy Dialogue Box", Order = -685)]
public sealed class DestroyDialogueBoxCommandSpec : CommandSpecBase
{
    [Header("Identity")]
    public string dialogueKey = "main";

    [Header("Options")]
    public bool killTween = true;
    public bool removeRefEntry = true;
    public bool strict = true;
}

public sealed class DestroyDialogueBoxCommand : CommandBase
{
    private readonly DestroyDialogueBoxCommandSpec _spec;

    private PresentationDialogueBoxView _view;
    private string _refKey;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => true;

    public DestroyDialogueBoxCommand(DestroyDialogueBoxCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        DestroyView(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        DestroyView(scope);
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        DestroyView(scope);
    }

    private void DestroyView(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_view == null)
            return;

        RectTransform rect = _view.Root != null ? _view.Root : _view.transform as RectTransform;
        if (rect != null && _spec.killTween)
            rect.DOKill(true); // Finish previous motion so this command starts from a committed state.

        if (_view.CanvasGroup != null)
            _view.CanvasGroup.DOKill(_spec.killTween);

        if (_spec.removeRefEntry && scope != null && scope.Refs != null && !string.IsNullOrEmpty(_refKey))
            scope.Refs.Remove(_refKey);

#if UNITY_EDITOR
        if (!Application.isPlaying)
            Object.DestroyImmediate(_view.gameObject);
        else
#endif
            Object.Destroy(_view.gameObject);

        _view = null;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;
        _refKey = PresentationDialogueBoxRegistryExt.MakeDialogueBoxRefKey(_spec.dialogueKey);

        if (!scope.Refs.TryGetDialogueBoxView(_spec.dialogueKey, out PresentationDialogueBoxView view))
        {
            if (_spec.strict)
                throw new InvalidOperationException($"[DestroyDialogueBoxCommand] DialogueBox view not found. dialogueKey={_spec.dialogueKey}");
            return;
        }

        _view = view;

        if (_view == null && _spec.strict)
            throw new InvalidOperationException($"[DestroyDialogueBoxCommand] DialogueBox view is null. dialogueKey={_spec.dialogueKey}");
    }
}