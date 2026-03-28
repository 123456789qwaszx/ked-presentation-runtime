using UnityEngine;

public interface IRouteLauncher
{
    void StartRoute(string entryKey);
}

public sealed class PresentationRouteEntry : MonoBehaviour, IRouteLauncher
{
    [SerializeField] private RouteCatalogSO routeCatalog;
    [SerializeField] private VnAppBootstrap vnAppBootstrap;

    public void StartRoute(string routeKey)
    {
        if (routeCatalog == null)
        {
            Debug.LogError("[CpsRouteEntry] RouteCatalog is not assigned.");
            return;
        }

        if (vnAppBootstrap == null)
        {
            Debug.LogError("[CpsRouteEntry] CpsSessionBootstrap is not assigned.");
            return;
        }

        if (!routeCatalog.TryResolve(routeKey, out Route route, out SequenceSpecSO sequence))
        {
            Debug.LogWarning($"[CpsRouteEntry] Failed to resolve routeKey='{routeKey}'");
            return;
        }

        vnAppBootstrap.Session.Start(route, sequence);
    }

    public void RequestEnd()
    {
        if (vnAppBootstrap?.Session == null) return;
        vnAppBootstrap.Session.RequestEnd();
    }
}