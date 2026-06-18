public sealed class VNSaveLoadSystem
{
    private const int SaveSlotCount = 120;
    
    private readonly JsonVNSaveRepository _saveRepository;
    private readonly JsonVNGlobalProgressRepository _globalRepository;
    private readonly VNGlobalProgressData _globalData;

    private IVNRuntimeStateProvider _stateProvider;
    private VNLoadSeekDriver _seekDriver;
    private IVNFlagStore _flagStore;
    
    public VNSaveService SaveService { get; private set; }
    public VNLoadService LoadService { get; private set; }
    public VNAlbumUnlockService AlbumService { get; private set; }
    
    public bool IsInitialized { get; private set; }

    public VNSaveLoadSystem()
    {
        _saveRepository = new JsonVNSaveRepository(SaveSlotCount);
        _globalRepository = new JsonVNGlobalProgressRepository();
        _globalData = _globalRepository.LoadOrCreate();
    }

    public void AttachRuntime(
        IVNRuntimeStateProvider stateProvider,
        VNLoadSeekDriver seekDriver,
        IVNFlagStore flagStore,
        VNAlbumDatabaseSO albumDatabase,
        VNTraceStream traceStream)
    {
        _stateProvider = stateProvider;
        _seekDriver = seekDriver;
        _flagStore = flagStore;
        
        AlbumService = new VNAlbumUnlockService(_globalData, _globalRepository, albumDatabase);
        LoadService = new VNLoadService(_saveRepository, _seekDriver, _flagStore, traceStream);
        SaveService = new VNSaveService(_saveRepository, _globalRepository, _globalData, _stateProvider, _flagStore);
        
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