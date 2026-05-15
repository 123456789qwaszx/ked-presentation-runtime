using System.Collections.Generic;
using UnityEngine;

public enum UIPlacementMode
{
    ZeroLocal,
}

public readonly struct UIPlacementRequest
{
    public readonly UIPlacementMode Mode;

    private UIPlacementRequest(UIPlacementMode mode)
    {
        Mode = mode;
    }

    public static UIPlacementRequest ZeroLocal()
    {
        return new UIPlacementRequest(UIPlacementMode.ZeroLocal);
    }
}

public partial class UIManager
{
    private static readonly UIPlacementRequest DefaultMountPlacement =
        UIPlacementRequest.ZeroLocal();

#if UNITY_EDITOR
    private readonly Dictionary<UIBase, Vector3> _editorInitialLocalPositions = new();
#endif

    private void ApplyMountPlacement(UIBase ui)
    {
        ApplyPlacement(ui, DefaultMountPlacement);
    }

    private void ApplyPlacement(UIBase ui, UIPlacementRequest request)
    {
        if (ui == null)
            return;

        switch (request.Mode)
        {
            case UIPlacementMode.ZeroLocal:
                ApplyZeroLocalPlacement(ui.transform);
                break;

            default:
                ApplyZeroLocalPlacement(ui.transform);
                break;
        }
    }

    private static void ApplyZeroLocalPlacement(Transform tr)
    {
        if (tr == null)
            return;

        tr.localPosition = Vector3.zero;
        tr.localRotation = Quaternion.identity;
        tr.localScale = Vector3.one;
    }

    private void ClearPlacementCacheForEditor()
    {
#if UNITY_EDITOR
        _editorInitialLocalPositions.Clear();
#endif
    }

    private void CaptureInitialPositionForEditor(UIBase ui)
    {
#if UNITY_EDITOR
        if (ui == null)
            return;

        if (_editorInitialLocalPositions.ContainsKey(ui))
            return;

        _editorInitialLocalPositions.Add(ui, ui.transform.localPosition);
#endif
    }

    private void RestoreInitialPositionForEditor(UIBase ui)
    {
#if UNITY_EDITOR
        if (ui == null)
            return;

        if (!_editorInitialLocalPositions.TryGetValue(ui, out Vector3 position))
            return;

        ui.transform.localPosition = position;
#endif
    }
}