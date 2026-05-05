using System;
using System.Collections;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Dialogue Box",
    "Hide Dialogue Box",
    Order = -780)]
public sealed class HideDialogueBoxCommandSpec : CommandSpecBase
{
    [Header("Target")]
    public DialogueBoxKind targetKind = DialogueBoxKind.Speaker;

    [Tooltip("true면 특정 kind와 무관하게 모든 DialogueBox를 숨깁니다.")]
    public bool hideAll = true;

    [Header("Fade")]
    public float duration = 0.18f;

    public bool snapOnSkip = true;
}

public sealed class HideDialogueBoxCommand : CommandBase
{
    private readonly HideDialogueBoxCommandSpec _spec;
    private readonly IDialogueBoxViewResolver _resolver;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public HideDialogueBoxCommand(
        HideDialogueBoxCommandSpec spec,
        IDialogueBoxViewResolver resolver)
    {
        _spec = spec;
        _resolver = resolver;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Apply();
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        Apply();
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        Apply();
    }

    private void Apply()
    {
        if (_resolver == null)
            return;

        if (_spec.hideAll)
        {
            _resolver.HideAll();
            return;
        }

        IDialogueTextTarget target = _resolver.ResolveTarget(_spec.targetKind);
        HideImmediate(target);
    }

    private static void HideImmediate(IDialogueTextTarget target)
    {
        if (target == null)
            return;

        IPresentationDialogueBoxView view = target as IPresentationDialogueBoxView;
        if (view != null)
        {
            view.SetVisible(false);
            return;
        }

        CanvasGroup cg = target.CanvasGroup;
        if (cg == null)
            return;

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }
}