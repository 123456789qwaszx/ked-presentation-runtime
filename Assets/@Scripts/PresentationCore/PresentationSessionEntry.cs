using UnityEngine;

public sealed class PresentationSessionEntry : MonoBehaviour, ICommandRunScopeProvider
{
    public PresentationSession PresentationSession { get; private set; }
    private RouteCatalogSO _routeCatalog;

    public void Initialize(PresentationSession presentationSession, RouteCatalogSO routeCatalogSo)
    {
        PresentationSession = presentationSession;
        _routeCatalog = routeCatalogSo;
    }

    public CommandRunScope CurrentScope => PresentationSession?.CurrentScope;
    
    private void Update()
    {
        if (PresentationSession != null)
            PresentationSession.Tick();
    }
    
    public void StartRoute(string routeKey)
    {
        if (!_routeCatalog.TryResolve(routeKey, out Route route, out SequenceSpecSO sequence))
        {
            Debug.LogWarning($"[CpsRouteEntry] Failed to resolve routeKey='{routeKey}'");
            return;
        }

        PresentationSession.Start(route, sequence);
    }

    public void RequestEnd()
    {
        PresentationSession.RequestEnd();
    }
}