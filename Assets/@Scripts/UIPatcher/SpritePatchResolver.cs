using System.Collections.Generic;

public readonly struct SpritePortAssignment
{
    public readonly string portId;
    public readonly string imageAddress;

    public SpritePortAssignment(string portId, string address)
    {
        this.portId = portId;
        imageAddress = address;
    }
}

public sealed class SpriteAssignmentBuilder
{
    public List<SpritePortAssignment> Build(IUISpritePortProvider ui, in UIContext context)
    {
        var patchPlan = new List<SpritePortAssignment>();

        IReadOnlyList<string> targetIds = ui.GetSpritePortIds();
        for (int i = 0; i < targetIds.Count; i++)
        {
            string targetId = targetIds[i];
            string imagePath = MakeImagePath(targetId, context);
            
            var assignment = new SpritePortAssignment(targetId, imagePath);
            patchPlan.Add(assignment);
        }

        return patchPlan;
    }

    private static string MakeImagePath(string portId, in UIContext context)
    {
        return $"ui/{context.ThemeId}/{portId}";
    }
}