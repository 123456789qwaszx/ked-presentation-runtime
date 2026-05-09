using UnityEngine;

public sealed class VNServiceContainer : MonoBehaviour
{
    [Header("Repository")]
    [SerializeField] private int _slotCount = 10;

    [Header("Album")]
    [SerializeField] private VNAlbumDatabaseSO _albumDatabase;
    
    private IVNSaveRepository _saveRepository;
    private IVNGlobalProgressRepository _globalRepository;
    private VNGlobalProgressData _globalData;

    public VNSaveService SaveService { get; private set; }
    public VNLoadService LoadService { get; private set; }
    public VNAlbumUnlockService AlbumService { get; private set; }

    private IVNRuntimeStateProvider _stateProvider;
    private IVNLoadSeekDriver _seekDriver;
    private IVNFlagStore _flagStore;
    private IVNSaveSafetyPolicy _safetyPolicy;
    
    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
        _saveRepository = new JsonVNSaveRepository(_slotCount);
        
        _globalRepository = new JsonVNGlobalProgressRepository();
        _globalData = _globalRepository.LoadOrCreate();
        

        IsInitialized = true;
    }

    public void BindRuntime(
        IVNRuntimeStateProvider stateProvider,
        IVNLoadSeekDriver seekDriver,
        IVNFlagStore flagStore,
        IVNSaveSafetyPolicy safetyPolicy)
    {
        _stateProvider = stateProvider;
        _seekDriver = seekDriver;
        _flagStore = flagStore;
        _safetyPolicy = safetyPolicy;
        
        AlbumService = new VNAlbumUnlockService(_globalData, _globalRepository, _albumDatabase);
        LoadService = new VNLoadService(_saveRepository, _seekDriver, _flagStore, _safetyPolicy);
        SaveService = new VNSaveService(_saveRepository, _globalRepository, _globalData, _stateProvider, _flagStore, _safetyPolicy);
    }

    public bool CanContinue()
    {
        if (_globalData == null || string.IsNullOrWhiteSpace(_globalData.continueSlotId))
            return false;
        
        return _saveRepository.Exists(_globalData.continueSlotId);
    }

    public bool TryContinue()
    {
        if (!CanContinue())
        {
            Debug.LogWarning("[VNContinueService] No continue target available.");
            return false;
        }

        Debug.Log($"[VNContinueService] Continue → slot='{_globalData.continueSlotId}'");
        return LoadService.Load(_globalData.continueSlotId);
    }
    
    public VNSaveSlotMeta[] GetAllSaveSlotMetas()
    {
        return _saveRepository.GetAllMetas();
    }
}