using System.Collections;
using UnityEngine;


public interface ITransitionTargetPlayer
{
    void SetInstant(TransitionTargetHandle target, float alpha, bool blockRaycasts);

    IEnumerator FadeTo(TransitionTargetHandle target, float targetAlpha, float duration, bool blockRaycasts, AnimationCurve ease);
}


public sealed class TransitionCommandFactory : INodeCommandFactory
{
    private readonly TransitionTargetRouter _transitionTargetRouter;
    private readonly ITransitionTargetPlayer _transitionTargetPlayer;
    private readonly UIPatchService _uiPatchService;

    public TransitionCommandFactory(
        TransitionTargetRouter transitionTargetRouter,
        ITransitionTargetPlayer transitionTargetPlayer,
        UIPatchService uiPatchService)
    {
        _transitionTargetRouter = transitionTargetRouter;
        _transitionTargetPlayer = transitionTargetPlayer;
        _uiPatchService = uiPatchService;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            TransitionCommandSpec s => new TransitionCommand(_transitionTargetRouter, _transitionTargetPlayer, s),
            UIPatchCommandSpec s => new UIPatchCommand(_uiPatchService, s),

            _ => null
        };

        return command != null;
    }
}