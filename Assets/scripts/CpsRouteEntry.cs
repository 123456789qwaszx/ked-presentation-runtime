using UnityEngine;

public interface IRouteLauncher
{
    void StartRoute(string entryKey);
}

public sealed class CpsRouteEntry : MonoBehaviour, IRouteLauncher
{
    [SerializeField] private RouteCatalogSO routeCatalog = null!;
    [SerializeField] private CpsSessionBootstrap cps = null!;

    public void StartRoute(string routeKey)
    {
        //Debug.Log($"Starting route {routeKey}");
        if (routeCatalog == null)
        {
            Debug.LogError("[CpsRouteEntry] RouteCatalog is not assigned.");
            return;
        }

        if (cps == null)
        {
            Debug.LogError("[CpsRouteEntry] CpsSessionBootstrap is not assigned.");
            return;
        }

        cps.Initialize();

        if (!routeCatalog.TryResolve(routeKey, out Route route, out SequenceSpecSO sequence))
        {
            Debug.LogWarning($"[CpsRouteEntry] Failed to resolve routeKey='{routeKey}'");
            return;
        }

        cps.Session.Start(route, sequence);
    }

    public void RequestEnd()
    {
        if (cps?.Session == null) return;
        cps.Session.RequestEnd();
    }
}