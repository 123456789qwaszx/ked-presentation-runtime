using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class UIPatchService
{
    private readonly SpritePortAssignmentBuilder _assignmentBuilder;
    private readonly UISpritePatcher _spritePatcher;

    public UIPatchService(SpritePortAssignmentBuilder assignmentBuilder, UISpritePatcher uiSpritePatcher)
    {
        _assignmentBuilder = assignmentBuilder;
        _spritePatcher = uiSpritePatcher;
    }

    public IEnumerator PatchUIInHierarchy(Component root, UIContext context)
    {
        MonoBehaviour[] children = root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not IUISpritePortProvider targetUI)
                continue;
            
            List<SpritePortAssignment> assignments = _assignmentBuilder.Build(targetUI, context);
            if (assignments == null || assignments.Count == 0)
                continue;

            yield return ApplySpritePatch(targetUI, assignments);
        }
    }

    private IEnumerator ApplySpritePatch(IUISpritePortProvider targetUI, List<SpritePortAssignment> patches)
    {
        yield return _spritePatcher.Apply(targetUI, patches);
    }
}