// using System.Collections.Generic;
// using UnityEngine;
//
// public enum LinkKind
// {
//     BranchUpper,
//     BranchLower,
//     BranchMiddle,
//     AttachmentLower
// }
//
// public readonly struct EpisodeGraphModel
// {
//     public readonly IReadOnlyList<EpisodeNodeModel> Nodes;
//
//     public EpisodeGraphModel(IReadOnlyList<EpisodeNodeModel> nodes)
//     {
//         Nodes = nodes;;
//     }
// }
//
// public readonly struct EpisodeNodeModel
// {
//     public readonly string EpisodeId;
//     public readonly string IndexText;
//     public readonly string Title;
//     public readonly Vector2 AnchoredPos;
//
//     public readonly bool Locked;
//     public readonly bool Interactable;
//     public readonly bool Selected;
//     public readonly bool IsCurrent;
//     public readonly bool Completed;
//
//     public readonly EpisodeAttachmentModel? UpperAttachment;
//     public readonly EpisodeAttachmentModel? LowerAttachment;
//
//     public EpisodeNodeModel(
//         string episodeId,
//         string indexText,
//         string title,
//         Vector2 anchoredPos,
//         bool locked = false,
//         bool interactable = true,
//         bool selected = false,
//         bool isCurrent = false,
//         bool completed = false,
//         EpisodeAttachmentModel? upperAttachment = null,
//         EpisodeAttachmentModel? lowerAttachment = null)
//     {
//         EpisodeId = episodeId;
//         IndexText = indexText;
//         Title = title;
//         AnchoredPos = anchoredPos;
//
//         Locked = locked;
//         Interactable = interactable;
//         Selected = selected;
//         IsCurrent = isCurrent;
//         Completed = completed;
//
//         UpperAttachment = upperAttachment;
//         LowerAttachment = lowerAttachment;
//     }
// }
//
// public readonly struct EpisodeAttachmentModel
// {
//     public readonly string HostEpisodeId;
//     public readonly string DisplayTitle;
//     public readonly bool IsInteractable;
//
//     public EpisodeAttachmentModel(string targetEpisodeId, string title, bool interactable = true)
//     {
//         HostEpisodeId = targetEpisodeId;
//         DisplayTitle = title;
//         IsInteractable = interactable;
//     }
// }