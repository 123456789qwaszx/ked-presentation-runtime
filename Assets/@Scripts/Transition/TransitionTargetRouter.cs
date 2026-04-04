using UnityEngine;

public sealed class TransitionTargetRouter : MonoBehaviour
{
    [SerializeField] private TransitionTargetBinding[] transitionTargets;

    public bool TryResolve(TransitionTargetKind kind, string customTargetKey, out TransitionTargetHandle handle)
    {
        handle = null;

        if (transitionTargets == null || transitionTargets.Length == 0)
            return false;

        foreach (var binding in transitionTargets)
        {
            if (binding == null || binding.canvasGroup == null)
                continue;

            if (binding.kind != kind)
                continue;

            if (kind == TransitionTargetKind.Custom)
            {
                if (binding.customTargetKey != customTargetKey)
                    continue;
            }

            handle = new TransitionTargetHandle
            {
                kind = kind,
                routeKey = BuildRouteKey(kind, customTargetKey),
                canvasGroup = binding.canvasGroup,
            };
            return true;
        }

        Debug.Log($"[TransitionTargetRouter] Resolve failed: kind={kind}, key={customTargetKey}");
        return false;
    }

    private string BuildRouteKey(TransitionTargetKind kind, string customTargetKey)
    {
        if (kind == TransitionTargetKind.Custom)
            return customTargetKey;
            
        return kind.ToString();
    }
}