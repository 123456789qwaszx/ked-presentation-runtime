using UnityEngine;

public sealed class VNAlbumUnlockDebugList : MonoBehaviour
{
    private VNSaveLoadSystem _vnServiceContainer;
    
    public void Initialize (VNSaveLoadSystem vnServiceContainer)
    {
        _vnServiceContainer  = vnServiceContainer;

        _initialized = true;
    }
    
    private bool _initialized;
    
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
        if (!_initialized)
            return;
        
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
        VNAlbumUnlockService album = _vnServiceContainer.AlbumService;

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
        VNAlbumUnlockService album = _vnServiceContainer.AlbumService;

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
        VNAlbumUnlockService album = _vnServiceContainer.AlbumService;
        
        album.ClearAllCgUnlocksForDebug();
        _nextUnlockIndex = 0;

        Debug.Log("[VNAlbumUnlockDebugList] Locked all test CGs.");
    }
}