using UnityEngine;

public static class PresentationResponseRigResolver
{
    public static PresentationResponseRig Resolve(PresentationViewRefs presentation)
    {
        if (presentation == null || presentation.Stage_Root == null)
            return null;

        return presentation.Stage_Root.GetComponentInParent<PresentationResponseRig>(true);
    }
}