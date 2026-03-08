// using System;
// using System.Collections;
// using UnityEngine;
//
// public partial class UIManager
// {
//     private bool HasSpritePatch()
//     {
//         // UIContexts는 "필드 값" 기준으로 유효성 체크
//         if (_spritePatch == null) return false;
//         if (string.IsNullOrEmpty(_uiContext.ThemeId)) return false;
//         return true;
//     }
//
//     private void InvokeAfterPatch(UIBase ui, Action callback)
//     {
//         if (ui == null)
//             return;
//
//         // 패치 시스템이 없다면 그냥 실행
//         if (!HasSpritePatch())
//         {
//             callback?.Invoke();
//             return;
//         }
//
//         int ticket = _showVersion;
//         StartCoroutine(CoPatchThen(ticket, ui, callback));
//     }
//
//     private IEnumerator CoPatchThen(int ticket, UIBase ui, Action callback)
//     {
//         // Patch (root + children)
//         yield return _spritePatch.ApplyInHierarchyIfSupported(ui, _uiContext);
//
//         if (ticket != _showVersion)
//             yield break;
//
//         callback?.Invoke();
//     }
//
//     // ----------------------------
//     // Optional: Theme/Locale 변경 대응
//     // ----------------------------
//     public void RepatchVisible()
//     {
//         if (!HasSpritePatch())
//             return;
//
//         BumpShowVersion();
//         int ticket = _showVersion;
//         StartCoroutine(CoRepatchVisible(ticket));
//     }
//
//     private IEnumerator CoRepatchVisible(int ticket)
//     {
//         // Root
//         if (CurSceneRoot != null && CurSceneRoot.gameObject.activeInHierarchy)
//         {
//             yield return _spritePatch.ApplyInHierarchyIfSupported(CurSceneRoot, _uiContext);
//             if (ticket != _showVersion) yield break;
//         }
//
//         // Panels (keepAliveDepth만)
//         if (_panelStack.Count > 0)
//         {
//             int keep = Mathf.Max(1, _keepAliveDepth);
//             int idx = 0;
//
//             foreach (UIBase panel in _panelStack) // top부터
//             {
//                 if (idx >= keep) break;
//
//                 if (panel != null && panel.gameObject.activeInHierarchy)
//                 {
//                     yield return _spritePatch.ApplyInHierarchyIfSupported(panel, _uiContext);
//                     if (ticket != _showVersion) yield break;
//                 }
//
//                 idx++;
//             }
//
//             ApplyPanelStackState(); // alpha/interactable 정책 재적용
//         }
//
//         // Overlay
//         if (_layerOverlay != null)
//         {
//             for (int i = 0; i < _layerOverlay.childCount; i++)
//             {
//                 var ui = _layerOverlay.GetChild(i).GetComponent<UIBase>();
//                 if (ui != null && ui.gameObject.activeInHierarchy)
//                 {
//                     yield return _spritePatch.ApplyInHierarchyIfSupported(ui, _uiContext);
//                     if (ticket != _showVersion) yield break;
//                 }
//             }
//         }
//
//         // Top
//         if (_layerTop != null)
//         {
//             for (int i = 0; i < _layerTop.childCount; i++)
//             {
//                 var ui = _layerTop.GetChild(i).GetComponent<UIBase>();
//                 if (ui != null && ui.gameObject.activeInHierarchy)
//                 {
//                     yield return _spritePatch.ApplyInHierarchyIfSupported(ui, _uiContext);
//                     if (ticket != _showVersion) yield break;
//                 }
//             }
//         }
//     }
//     
//     // ButtonWidget
//     public Coroutine PatchNow(UIBase ui, Action afterPatched = null)
//     {
//         if (ui == null) return null;
//
//         // 패치 시스템 없으면 바로 콜백
//         if (_spritePatch == null)
//         {
//             afterPatched?.Invoke();
//             return null;
//         }
//
//         return StartCoroutine(CoPatchNow(ui, afterPatched));
//     }
//
//     private IEnumerator CoPatchNow(UIBase ui, Action afterPatched)
//     {
//         yield return _spritePatch.ApplyInHierarchyIfSupported(ui, _uiContext);
//         afterPatched?.Invoke();
//     }
// }
