using UnityEngine;

public sealed class EpisodePlayer : MonoBehaviour
{
    [Header("Refs")]
    public CpsRouteEntry cpsEntry;

    public bool TryPlayByEpisodeId(string episodeId)
    {
        //UIManager.Instance.SwitchRoot<DialogueUIRoot>();
        //cpsEntry?.StartRoute(ep.entryKey);
        return true;
    }
}