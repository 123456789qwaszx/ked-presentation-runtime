using UnityEngine;

public sealed class PresentationSessionEntry : MonoBehaviour, ICommandRunScopeProvider
{
    public PresentationSession PresentationSession { get; private set; }

    private RouteCatalogSO _routeCatalog;

    public bool IsInitialized => PresentationSession != null && _routeCatalog != null;
    public bool IsRunning => PresentationSession != null && PresentationSession.IsRunning;

    public CommandRunScope CurrentScope
    {
        get
        {
            if (PresentationSession == null)
                return null;

            return PresentationSession.CurrentScope;
        }
    }
    
    public CommandRunScope SubScope => PresentationSession != null ? PresentationSession.SubScope : null;

    public void Initialize(PresentationSession presentationSession, RouteCatalogSO routeCatalogSo)
    {
        PresentationSession = presentationSession;
        _routeCatalog = routeCatalogSo;
    }

    private void Update()
    {
        if (PresentationSession != null)
            PresentationSession.Tick();
    }
    
    public void RestartRoute(string routeKey)
    {
        //EndRouteNow();
        
        if (!TryResolveRoute(routeKey, out Route route, out SequenceSpecSO sequence))
            return;
        PresentationSession.ClearStage();
        PresentationSession.Start(route, sequence);
    }

    public void RequestEnd()
    {
        if (PresentationSession == null)
            return;

        PresentationSession.RequestEnd();
    }

    public void EndRouteNow()
    {
        if (PresentationSession == null)
            return;

        PresentationSession.EndImmediately();
    }

    private bool TryResolveRoute(
        string routeKey,
        out Route route,
        out SequenceSpecSO sequence)
    {
        route = default;
        sequence = null;

        if (PresentationSession == null)
        {
            Debug.LogWarning("[PresentationSessionEntry] Cannot start route. PresentationSession is null.", this);
            return false;
        }

        if (_routeCatalog == null)
        {
            Debug.LogWarning("[PresentationSessionEntry] Cannot start route. RouteCatalog is null.", this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(routeKey))
        {
            Debug.LogWarning("[PresentationSessionEntry] Cannot start route. routeKey is null or empty.", this);
            return false;
        }

        if (!_routeCatalog.TryResolve(routeKey, out route, out sequence))
        {
            Debug.LogWarning($"[PresentationSessionEntry] Failed to resolve routeKey='{routeKey}'", this);
            return false;
        }

        if (sequence == null)
        {
            Debug.LogWarning($"[PresentationSessionEntry] Resolved routeKey='{routeKey}', but sequence is null.", this);
            return false;
        }

        return true;
    }
}