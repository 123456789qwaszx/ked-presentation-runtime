using UnityEngine;

public sealed class VNAlbumUnlockDebugList : MonoBehaviour
{
    [SerializeField] private string[] _cgKeys =
    {
        "cg_test_01",
        "cg_test_02",
    };

    [Header("Keys")]
    [SerializeField] private KeyCode _unlockNextKey = KeyCode.Alpha7;
    [SerializeField] private KeyCode _lockAllKey = KeyCode.Alpha8;
    [SerializeField] private KeyCode _unlockAllKey = KeyCode.Alpha9;

    private int _nextUnlockIndex;

    private void Update()
    {
        if (Input.GetKeyDown(_unlockNextKey))
        {
            UnlockNext();
            return;
        }

        if (Input.GetKeyDown(_lockAllKey))
        {
            LockAll();
            return;
        }

        if (Input.GetKeyDown(_unlockAllKey))
        {
            UnlockAll();
        }
    }

    private void UnlockNext()
    {
        VNAlbumUnlockService album = ResolveAlbumService();

        if (album == null)
            return;

        while (_nextUnlockIndex < _cgKeys.Length)
        {
            string key = _cgKeys[_nextUnlockIndex];
            _nextUnlockIndex++;

            if (string.IsNullOrWhiteSpace(key))
                continue;

            bool unlocked = album.Unlock(key);

            Debug.Log(unlocked
                ? $"[VNAlbumUnlockDebugList] Unlock next CG: {key}"
                : $"[VNAlbumUnlockDebugList] CG already unlocked or failed: {key}");

            return;
        }

        Debug.Log("[VNAlbumUnlockDebugList] No more CG keys to unlock.");
    }

    private void UnlockAll()
    {
        VNAlbumUnlockService album = ResolveAlbumService();

        if (album == null)
            return;

        for (int i = 0; i < _cgKeys.Length; i++)
        {
            string key = _cgKeys[i];

            if (string.IsNullOrWhiteSpace(key))
                continue;

            album.Unlock(key);
        }

        _nextUnlockIndex = _cgKeys.Length;

        Debug.Log("[VNAlbumUnlockDebugList] Unlock all test CGs requested.");
    }

    private void LockAll()
    {
        VNAlbumUnlockService album = ResolveAlbumService();

        if (album == null)
            return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        album.ClearAllCgUnlocksForDebug();
        _nextUnlockIndex = 0;

        Debug.Log("[VNAlbumUnlockDebugList] Locked all test CGs.");
#else
        Debug.LogWarning("[VNAlbumUnlockDebugList] LockAll is only available in Editor or Development Build.");
#endif
    }

    private VNAlbumUnlockService ResolveAlbumService()
    {
        VNServiceContainer svc = VNServiceContainer.Instance;

        if (svc == null || !svc.IsPersistentInitialized || svc.AlbumService == null)
        {
            Debug.LogWarning("[VNAlbumUnlockDebugList] Album service is not ready.");
            return null;
        }

        return svc.AlbumService;
    }
}