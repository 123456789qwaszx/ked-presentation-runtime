// using System;
// using UnityEngine;
//
// public sealed class EpisodeGraphView : MonoBehaviour
// {
//     [Header("Root")]
//     [SerializeField] private RectTransform content;
//
//     [Header("Optional Prefab")]
//     [SerializeField] private RectTransform nodeRigPrefab;
//
//     [Header("Sizing")]
//     [SerializeField] private HorizontalScrollContentFitter sizer;
//
//     private EpisodeGraphRenderer _renderer;
//
//     private Action<string> _onMainClicked;
//     private Action<string, EpisodeNodeLinkSlot, EpisodeNodeLinkViewData> _onLinkClicked;
//
//     private void Awake()
//     {
//         EnsureRenderer();
//     }
//
//     public void SetHandlers(Action<string> onMainClicked, Action<string, EpisodeNodeLinkSlot, EpisodeNodeLinkViewData> onLinkClicked)
//     {
//         _onMainClicked = onMainClicked;
//         _onLinkClicked = onLinkClicked;
//
//         _renderer.SetHandlers(HandleMainClicked, HandleLinkClicked);
//     }
//
//     public void Render(EpisodeGraphViewData viewData)
//     {
//         EnsureRenderer();
//         _renderer.Render(viewData);
//     }
//
//     public void ClearAll()
//     {
//         _renderer.ClearAll();
//     }
//     
//     private void EnsureRenderer()
//     {
//         if (_renderer != null)
//             return;
//
//         _renderer = new EpisodeGraphRenderer(content, nodeRigPrefab, sizer);
//
//         _renderer.SetHandlers(
//             HandleMainClicked,
//             HandleLinkClicked);
//     }
//
//     private void HandleMainClicked(string episodeId)
//     {
//         _onMainClicked?.Invoke(episodeId);
//     }
//
//     private void HandleLinkClicked(string ownerEpisodeId, EpisodeNodeLinkSlot slot, EpisodeNodeLinkViewData link)
//     {
//         _onLinkClicked?.Invoke(ownerEpisodeId, slot, link);
//     }
// }