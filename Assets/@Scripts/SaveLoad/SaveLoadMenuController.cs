using UnityEngine;

public sealed class SaveLoadMenuController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject _root;

    [Header("Slot UI")]
    [SerializeField] private Transform _contentRoot;
    [SerializeField] private VNSaveSlotButton _slotButtonPrefab;

    private bool _isSaveMode;
    private VNServiceContainer _svc;

    private void Awake()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    public void OpenAsSaveMenu()
    {
        _isSaveMode = true;
        Open();
    }

    public void OpenAsLoadMenu()
    {
        _isSaveMode = false;
        Open();
    }

    public void Close()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    private void Open()
    {
        _svc = VNServiceContainer.Instance;

        if (_svc == null || !_svc.IsPersistentInitialized)
        {
            Debug.LogWarning("[SaveLoadMenuController] VNServiceContainer is not ready.");
            return;
        }

        if (_root != null)
            _root.SetActive(true);

        Rebuild();
    }

    private void Rebuild()
    {
        if (_contentRoot == null || _slotButtonPrefab == null)
        {
            Debug.LogError("[SaveLoadMenuController] Missing contentRoot or slotButtonPrefab.", this);
            return;
        }

        for (int i = _contentRoot.childCount - 1; i >= 0; i--)
            Destroy(_contentRoot.GetChild(i).gameObject);

        VNSaveSlotMeta[] metas = _svc.SaveRepository.GetAllMetas();

        for (int i = 0; i < metas.Length; i++)
        {
            int slotIndex = i + 1;
            VNSaveSlotMeta meta = metas[i];

            VNSaveSlotButton button = Instantiate(_slotButtonPrefab, _contentRoot);
            button.Bind(slotIndex, meta, _isSaveMode, OnSlotClicked);
        }
    }

    private void OnSlotClicked(int slotIndex)
    {
        if (_svc == null)
            return;

        if (_isSaveMode)
        {
            if (!_svc.IsRuntimeBound || _svc.SaveService == null)
            {
                Debug.LogWarning("[SaveLoadMenuController] Runtime is not bound. Cannot save.");
                return;
            }

            if (_svc.SaveService.SaveManual(slotIndex))
                Rebuild();

            return;
        }

        if (!_svc.IsRuntimeBound || _svc.LoadService == null)
        {
            Debug.LogWarning("[SaveLoadMenuController] Runtime is not bound. Cannot load.");
            return;
        }

        _svc.LoadService.Load(slotIndex);
    }
}