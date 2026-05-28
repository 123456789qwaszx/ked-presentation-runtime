using System;
using UnityEngine;

[Serializable]
public sealed class EpisodeGraphLayoutOptions
{
    [Header("Node")]
    public Vector2 NodeSize = new Vector2(360f, 160f);

    [Header("Main Path")]
    public float MainGapX = 460f;

    [Header("Branch")]
    public float BranchOffsetY = 220f;

    [Header("Content Padding")]
    public Vector2 Padding = new Vector2(240f, 260f);

    public static EpisodeGraphLayoutOptions Compact()
    {
        return new EpisodeGraphLayoutOptions
        {
            NodeSize = new Vector2(300f, 140f),
            MainGapX = 380f,
            BranchOffsetY = 180f,
            Padding = new Vector2(180f, 220f)
        };
    }

    public static EpisodeGraphLayoutOptions Wide()
    {
        return new EpisodeGraphLayoutOptions
        {
            NodeSize = new Vector2(360f, 160f),
            MainGapX = 520f,
            BranchOffsetY = 240f,
            Padding = new Vector2(280f, 300f)
        };
    }
}