using System;

public static class BackgroundRigTargetResolver
{
    public static BackgroundRigRefs ResolveBackgroundRigFromTargetKey(CommandRunScope scope, string rigKey)
    {
        if (!scope.backgroundRigs.TryGetRig(rigKey, out BackgroundRigRefs rig))
        {
            throw new InvalidOperationException(
                $"[BackgroundRigTargetResolver] Failed to resolve BackgroundRigRefs. " +
                $"rigKey='{rigKey}'.");
        }

        return rig;
    }
}