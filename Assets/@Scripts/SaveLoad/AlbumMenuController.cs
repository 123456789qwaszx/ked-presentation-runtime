using UnityEngine;
using UnityEngine.UI;

public sealed class AlbumMenuController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject _root;

    [Header("Grid")]
    [SerializeField] private Transform _contentRoot;
    [SerializeField] private VNAlbumSlotButton _slotButtonPrefab;

    [Header("Preview")]
    [SerializeField] private Image _previewImage;

    private VNServiceContainer _svc;

    private void Awake()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    public void Open()
    {
        _svc = VNServiceContainer.Instance;

        if (_svc == null || !_svc.IsPersistentInitialized || _svc.AlbumService == null)
        {
            Debug.LogWarning("[AlbumMenuController] Album service is not ready.");
            return;
        }

        if (_root != null)
            _root.SetActive(true);

        Rebuild();
    }

    public void Close()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    private void Rebuild()
    {
        if (_contentRoot == null || _slotButtonPrefab == null)
        {
            Debug.LogError("[AlbumMenuController] Missing contentRoot or slotButtonPrefab.", this);
            return;
        }

        for (int i = _contentRoot.childCount - 1; i >= 0; i--)
            Destroy(_contentRoot.GetChild(i).gameObject);

        var items = _svc.AlbumService.GetAllItems();

        for (int i = 0; i < items.Count; i++)
        {
            VNAlbumItemSO item = items[i];

            if (item == null)
                continue;

            bool unlocked = _svc.AlbumService.IsUnlocked(item.key);

            VNAlbumSlotButton button = Instantiate(_slotButtonPrefab, _contentRoot);
            button.Bind(item, unlocked, OnAlbumItemClicked);
        }
    }

    private void OnAlbumItemClicked(VNAlbumItemSO item)
    {
        if (item == null)
            return;

        if (_previewImage != null)
        {
            _previewImage.sprite = item.cgSprite;
            _previewImage.enabled = item.cgSprite != null;
        }
    }
}