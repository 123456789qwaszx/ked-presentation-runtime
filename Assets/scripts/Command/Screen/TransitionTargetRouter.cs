using System;
using UnityEngine;

[Serializable]
public sealed class TransitionTargetBinding
{
    public TransitionTargetKind kind = TransitionTargetKind.Blackout;
    public string customTargetKey = "";
    public CanvasGroup canvasGroup;
}

public sealed class TransitionTargetRouter : MonoBehaviour, ITransitionTargetRouter
{
    [SerializeField] private TransitionTargetBinding[] _bindings;

    public bool TryResolve(
        TransitionTargetKind kind,
        string customTargetKey,
        out TransitionTargetHandle handle)
    {
        handle = null;

        if (_bindings == null || _bindings.Length == 0)
            return false;

        string normalizedCustomKey = Normalize(customTargetKey);

        foreach (var binding in _bindings)
        {
            if (binding == null || binding.canvasGroup == null)
                continue;

            if (binding.kind != kind)
                continue;

            if (kind == TransitionTargetKind.Custom)
            {
                if (Normalize(binding.customTargetKey) != normalizedCustomKey)
                    continue;
            }

            handle = new TransitionTargetHandle
            {
                kind     = kind,
                routeKey = BuildRouteKey(kind, customTargetKey),
                canvasGroup = binding.canvasGroup,
            };
            return true;
        }

        return false;
    }

    private static string Normalize(string value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();

    private static string BuildRouteKey(TransitionTargetKind kind, string customTargetKey)
        => kind != TransitionTargetKind.Custom
            ? kind.ToString()
            : "Custom:" + Normalize(customTargetKey);
}