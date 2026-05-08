using System;
using UnityEngine;

public sealed class VNServiceContainer : MonoBehaviour
{
    [Header("Repository")]
    [SerializeField] private int _slotCount = 10;

    [Header("Album")]
    [SerializeField] private VNAlbumDatabaseSO _albumDatabase;

    [Header("Continue Policy")]
    [SerializeField] private bool _updateContinueOnAutoSave = true;

    public static VNServiceContainer Instance { get; private set; }

    public bool IsPersistentInitialized { get; private set; }
    public bool IsRuntimeBound { get; private set; }

    public event Action PersistentInitialized;
    public event Action RuntimeBound;

    public IVNSaveRepository SaveRepository { get; private set; }
    public IVNGlobalProgressRepository GlobalRepository { get; private set; }
    public VNGlobalProgressData GlobalData { get; private set; }

    public VNSaveService SaveService { get; private set; }
    public VNLoadService LoadService { get; private set; }
    public VNContinueService ContinueService { get; private set; }
    public VNAutoSaveService AutoSaveService { get; private set; }
    public VNAlbumUnlockService AlbumService { get; private set; }
    public VNReadLineService ReadLineService { get; private set; }

    private IVNRuntimeStateProvider _stateProvider;
    private IVNLoadSeekDriver _seekDriver;
    private IVNFlagStore _flagStore;
    private IVNSaveSafetyPolicy _safetyPolicy;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializePersistentServices();
    }

    private void OnDestroy()
    {
        ReadLineService?.Flush();

        if (Instance == this)
            Instance = null;
    }

    public void InitializePersistentServices()
    {
        if (IsPersistentInitialized)
            return;

        SaveRepository = new JsonVNSaveRepository(_slotCount);
        GlobalRepository = new JsonVNGlobalProgressRepository();
        GlobalData = GlobalRepository.LoadOrCreate();
        GlobalData.Normalize();

        AlbumService = new VNAlbumUnlockService(GlobalData, GlobalRepository, _albumDatabase);
        ReadLineService = new VNReadLineService(GlobalData, GlobalRepository);

        IsPersistentInitialized = true;

        Debug.Log("[VNServiceContainer] Persistent services initialized.");
        PersistentInitialized?.Invoke();
    }

    public void BindRuntime(
        IVNRuntimeStateProvider stateProvider,
        IVNLoadSeekDriver seekDriver,
        IVNFlagStore flagStore,
        IVNSaveSafetyPolicy safetyPolicy)
    {
        if (!IsPersistentInitialized)
            InitializePersistentServices();

        if (stateProvider == null)
        {
            Debug.LogError("[VNServiceContainer] stateProvider is null. Runtime bind aborted.");
            return;
        }

        if (seekDriver == null)
        {
            Debug.LogError("[VNServiceContainer] seekDriver is null. Runtime bind aborted.");
            return;
        }

        _stateProvider = stateProvider;
        _seekDriver = seekDriver;

        _flagStore = flagStore;
        if (_flagStore == null)
        {
            Debug.LogWarning("[VNServiceContainer] flagStore is null. Using EmptyVNFlagStore.");
            _flagStore = new EmptyVNFlagStore();
        }

        _safetyPolicy = safetyPolicy;
        if (_safetyPolicy == null)
        {
            Debug.LogWarning("[VNServiceContainer] safetyPolicy is null. Using AlwaysAllowVNSaveSafetyPolicy.");
            _safetyPolicy = new AlwaysAllowVNSaveSafetyPolicy();
        }

        SaveService = new VNSaveService(
            SaveRepository,
            GlobalRepository,
            GlobalData,
            _stateProvider,
            _flagStore,
            _safetyPolicy);

        SaveService.UpdateContinueOnAutoSave = _updateContinueOnAutoSave;

        LoadService = new VNLoadService(
            SaveRepository,
            _seekDriver,
            _flagStore,
            _safetyPolicy);

        ContinueService = new VNContinueService(
            GlobalData,
            LoadService,
            SaveRepository);

        AutoSaveService = new VNAutoSaveService(SaveService);

        IsRuntimeBound = true;

        Debug.Log("[VNServiceContainer] Runtime services bound.");
        RuntimeBound?.Invoke();
    }

    public void UnbindRuntime()
    {
        ReadLineService?.Flush();

        SaveService = null;
        LoadService = null;
        ContinueService = null;
        AutoSaveService = null;

        _stateProvider = null;
        _seekDriver = null;
        _flagStore = null;
        _safetyPolicy = null;

        IsRuntimeBound = false;

        Debug.Log("[VNServiceContainer] Runtime services unbound.");
    }

    public bool CanContinue()
    {
        if (!IsPersistentInitialized || GlobalData == null || SaveRepository == null)
            return false;

        GlobalData.Normalize();

        if (string.IsNullOrWhiteSpace(GlobalData.continueSlotId))
            return false;

        return SaveRepository.Exists(GlobalData.continueSlotId);
    }

    public bool TryContinue()
    {
        if (!IsRuntimeBound || ContinueService == null)
        {
            Debug.LogWarning("[VNServiceContainer] Runtime is not bound. Cannot continue yet.");
            return false;
        }

        return ContinueService.Continue();
    }
}