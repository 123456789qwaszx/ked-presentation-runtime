using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class UISpritePatchService
{
    private readonly SpriteAssignmentBuilder _assignmentBuilder;
    private readonly UISpritePatcher _patcher;

    public UISpritePatchService(SpriteAssignmentBuilder assignmentBuilder, UISpritePatcher uiSpritePatcher)
    {
        _assignmentBuilder = assignmentBuilder;
        _patcher = uiSpritePatcher;
    }

    public IEnumerator ApplyInHierarchyIfSupported(Component root, UIContext context)
    {
        MonoBehaviour[] children = root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not IUISpritePortProvider targetUI)
                continue;

            List<SpritePortAssignment> assignments = _assignmentBuilder.Build(targetUI, context);

            yield return _patcher.Apply(targetUI, assignments);
        }
    }
}