public sealed class VNSaveLoadSystem
{
    private readonly IVNSaveRepository _saveRepository;
    private readonly IVNGlobalProgressRepository _globalRepository;
    private readonly VNGlobalProgressData _globalData;

    private IVNRuntimeStateProvider _stateProvider;
    private IVNLoadSeekDriver _seekDriver;
    private IVNFlagStore _flagStore;
    private IVNSaveSafetyPolicy _safetyPolicy;
    
    public VNSaveService SaveService { get; private set; }
    public VNLoadService LoadService { get; private set; }
    public VNAlbumUnlockService AlbumService { get; private set; }
    
    public bool IsInitialized { get; private set; }

    public VNSaveLoadSystem(int saveSlotCount)
    {
        _saveRepository = new JsonVNSaveRepository(saveSlotCount);
        _globalRepository = new JsonVNGlobalProgressRepository();
        _globalData = _globalRepository.LoadOrCreate();
    }

    public void AttachRuntime(
        IVNRuntimeStateProvider stateProvider,
        IVNLoadSeekDriver seekDriver,
        IVNFlagStore flagStore,
        IVNSaveSafetyPolicy safetyPolicy,
        VNAlbumDatabaseSO albumDatabase)
    {
        _stateProvider = stateProvider;
        _seekDriver = seekDriver;
        _flagStore = flagStore;
        _safetyPolicy = safetyPolicy;
        
        AlbumService = new VNAlbumUnlockService(_globalData, _globalRepository, albumDatabase);
        LoadService = new VNLoadService(_saveRepository, _seekDriver, _flagStore, _safetyPolicy);
        SaveService = new VNSaveService(_saveRepository, _globalRepository, _globalData, _stateProvider, _flagStore, _safetyPolicy);
        
        IsInitialized = true;
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
            return false;
        }

        return LoadService.Load(_globalData.continueSlotId);
    }
    
    public VNSaveSlotMeta[] GetAllSaveSlotMetas()
    {
        return _saveRepository.GetAllMetas();
    }
}