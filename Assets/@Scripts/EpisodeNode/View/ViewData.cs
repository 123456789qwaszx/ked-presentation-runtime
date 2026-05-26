using System.Collections.Generic;
using UnityEngine;

public sealed class EpisodeGraphViewData
{
    public List<EpisodeNodeViewData> Nodes = new();
    public Vector2 ContentSize;
}

public sealed class EpisodeNodeViewData
{
    public string EpisodeId;
    public string Title;
    public string IndexText;

    public Vector2 AnchoredPosition;
    public Vector2 Size;

    public EpisodeNodeVisualState VisualState;
}

public enum EpisodeNodeVisualState
{
    Normal,
    Selected,
    Current,
    Completed,
    Locked
}