// using System;
// using System.Collections.Generic;
// using UnityEngine;
//
// public sealed class EpisodeGraphViewData
// {
//     public List<EpisodeNodeViewData> Nodes = new();
//     public Vector2 ContentSize;
//     
//     public bool TryGetNode(string episodeId, out EpisodeNodeViewData node)
//     {
//         node = null;
//
//         if (string.IsNullOrEmpty(episodeId) || Nodes == null)
//             return false;
//
//         for (int i = 0; i < Nodes.Count; i++)
//         {
//             EpisodeNodeViewData candidate = Nodes[i];
//
//             if (candidate == null)
//                 continue;
//
//             if (string.Equals(candidate.EpisodeId, episodeId, StringComparison.Ordinal))
//             {
//                 node = candidate;
//                 return true;
//             }
//         }
//
//         return false;
//     }
// }