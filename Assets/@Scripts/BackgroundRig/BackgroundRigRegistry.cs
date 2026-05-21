using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class BackgroundRigRegistry
{
    private readonly Dictionary<string, BackgroundRigRefs> _rigs = new(StringComparer.Ordinal);

    public void Register(string rigKey, BackgroundRigRefs rigRefs)
    {
        if (_rigs.TryGetValue(rigKey, out BackgroundRigRefs existingRig))
            DestroyRig(existingRig);

        _rigs[rigKey] = rigRefs;
    }

    public bool Unregister(string rigKey)
    {
        if (!_rigs.Remove(rigKey, out BackgroundRigRefs rigRefs))
        {
            Debug.LogWarning($"[BackgroundRigRegistry] Unregister failed. Rig not found. rigKey='{rigKey}'.");
            return false;
        }

        DestroyRig(rigRefs);
        return true;
    }

    public bool HasRig(string rigKey)
    {
        return _rigs.TryGetValue(rigKey, out BackgroundRigRefs rigRefs)
               && rigRefs?.RigRoot != null;
    }

    public bool TryGetRig(string rigKey, out BackgroundRigRefs rigRefs)
    {
        if (!_rigs.TryGetValue(rigKey, out rigRefs))
        {
            Debug.LogWarning($"[BackgroundRigRegistry] Rig not found. rigKey='{rigKey}'.");
            return false;
        }

        if (rigRefs?.RigRoot == null)
        {
            Debug.LogWarning($"[BackgroundRigRegistry] Rig is registered but invalid or destroyed. rigKey='{rigKey}'.");
            rigRefs = null;
            return false;
        }

        return true;
    }

    public void Clear()
    {
        foreach (BackgroundRigRefs rigRefs in _rigs.Values)
            DestroyRig(rigRefs);

        _rigs.Clear();
    }

    private static void DestroyRig(BackgroundRigRefs rigRefs)
    {
        if (rigRefs?.RigRoot == null)
            return;

        KillTweenOnHierarchy(rigRefs.RigRoot);
        Object.Destroy(rigRefs.RigRoot.gameObject);
    }

    private static void KillTweenOnHierarchy(Transform root)
    {
        if (root == null)
            return;

        RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
        {
            if (rects[i] != null)
                rects[i].DOKill(false);
        }

        CanvasGroup[] canvasGroups = root.GetComponentsInChildren<CanvasGroup>(true);
        for (int i = 0; i < canvasGroups.Length; i++)
        {
            if (canvasGroups[i] != null)
                canvasGroups[i].DOKill(false);
        }

        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].DOKill(false);
        }

        DOTween.Kill(root, false);
        DOTween.Kill(root.gameObject, false);
    }
}