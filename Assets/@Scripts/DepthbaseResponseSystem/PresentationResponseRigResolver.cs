using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 네 현재 구조에서 PresentationResponseRig를 찾는 공용 resolver.
/// PresentationViewAccess에는 의존하지 않고, 실제 runtime root인 PresentationUIRoot를 우선한다.
/// </summary>
public static class PresentationResponseRigResolver
{
    private static readonly Dictionary<int, PresentationResponseRig> Cache =
        new Dictionary<int, PresentationResponseRig>();

    public static PresentationResponseRig Resolve(object presentation)
    {
        if (presentation == null)
            return FallbackSearch();

        if (presentation is Object unityObject)
        {
            int key = unityObject.GetInstanceID();
            if (Cache.TryGetValue(key, out PresentationResponseRig cached) && cached != null)
                return cached;

            PresentationResponseRig resolved = ResolveFromUnityObject(unityObject);
            if (resolved != null)
                Cache[key] = resolved;

            return resolved;
        }

        return FallbackSearch();
    }

    public static void Clear()
    {
        Cache.Clear();
    }

    private static PresentationResponseRig ResolveFromUnityObject(Object unityObject)
    {
        if (unityObject is PresentationUIRoot root)
            return ResolveFromPresentationRoot(root);

        if (unityObject is MonoBehaviour mono)
        {
            PresentationResponseRig direct = mono.GetComponentInChildren<PresentationResponseRig>(true);
            if (direct != null)
                return direct;

            PresentationUIRoot parentRoot = mono.GetComponentInParent<PresentationUIRoot>(true);
            if (parentRoot != null)
                return ResolveFromPresentationRoot(parentRoot);
        }

        return FallbackSearch();
    }

    private static PresentationResponseRig ResolveFromPresentationRoot(PresentationUIRoot root)
    {
        if (root == null)
            return null;

        PresentationResponseRig rig = root.GetComponentInChildren<PresentationResponseRig>(true);
        if (rig == null)
        {
            Debug.LogWarning(
                "[PresentationResponseRigResolver] PresentationUIRoot 하위에서 Rig를 찾지 못했습니다. " +
                "PresentationResponseRig를 배치하거나 Auto Wire를 수행하세요.");
        }

        return rig;
    }

    private static PresentationResponseRig FallbackSearch()
    {
        PresentationResponseRig rig = Object.FindAnyObjectByType<PresentationResponseRig>();
        if (rig == null)
        {
            Debug.LogWarning(
                "[PresentationResponseRigResolver] Scene에서 Rig를 찾지 못했습니다. " +
                "PresentationUIRoot 하위에 PresentationResponseRig를 배치하세요.");
        }
        return rig;
    }
}
