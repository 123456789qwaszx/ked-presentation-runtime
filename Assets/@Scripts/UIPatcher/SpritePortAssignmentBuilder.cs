using System.Collections.Generic;

public readonly struct SpritePortAssignment
{
    public readonly string portId;
    public readonly string spriteAddress;

    public SpritePortAssignment(string portId, string address)
    {
        this.portId = portId;
        spriteAddress = address;
    }
}

public sealed class SpritePortAssignmentBuilder
{
    public List<SpritePortAssignment> Build(IUISpritePortProvider ui, in UIContext context)
    {
        var patchPlan = new List<SpritePortAssignment>();

        IReadOnlyList<string> targetIds = ui.GetSpritePortIds();
        for (int i = 0; i < targetIds.Count; i++)
        {
            string targetId = targetIds[i];
            string imageAddress = MakeImagePath(targetId, context);
            
            var entry = new SpritePortAssignment(targetId, imageAddress);
            patchPlan.Add(entry);
        }

        return patchPlan;
    }

    private static string MakeImagePath(string portId, in UIContext context)
    {
        return $"ui/{context.ThemeId}/{portId}";
    }
}