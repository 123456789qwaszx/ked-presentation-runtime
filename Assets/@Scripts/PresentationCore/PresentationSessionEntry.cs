using UnityEngine;

public sealed class PresentationSessionEntry : MonoBehaviour, ICommandRunScopeProvider
{
    public PresentationLaneScopeSession LaneScopes { get; private set; }

    public CommandRunScope CurrentScope => LaneScopes?.CurrentScope;
    public CommandRunScope SubScope => LaneScopes?.SubScope;

    public void Initialize(PresentationLaneScopeSession laneScopes)
    {
        LaneScopes = laneScopes;
    }

    // routeKey는 더 이상 쓰이지 않는다(Route/SequenceSpecSO 기반 메인 흐름의 잔재).
    // 호출부(EpisodePlayer) 시그니처를 안 건드리려고 파라미터만 유지.
    public void RestartRoute()
    {
        if (LaneScopes == null)
        {
            Debug.LogWarning("[PresentationSessionEntry] LaneScopes is null.", this);
            return;
        }

        LaneScopes.ClearStage();
        LaneScopes.Start();
    }

    public void EndRouteNow() => LaneScopes?.EndImmediately();
}