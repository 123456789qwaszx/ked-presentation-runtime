using System.Collections;
using UnityEngine;




public sealed class TransitionCommandFactory : INodeCommandFactory
{
    private readonly TransitionTargetRouter _transitionTargetRouter;
    private readonly UIPatchService _uiPatchService;

    public TransitionCommandFactory(
        TransitionTargetRouter transitionTargetRouter,
        UIPatchService uiPatchService)
    {
        _transitionTargetRouter = transitionTargetRouter;
        _uiPatchService = uiPatchService;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            TransitionCommandSpec s => new TransitionCommand(_transitionTargetRouter, s),
            UIPatchCommandSpec s => new UIPatchCommand(_uiPatchService, s),

            _ => null
        };

        return command != null;
    }
}