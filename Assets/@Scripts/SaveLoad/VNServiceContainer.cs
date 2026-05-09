using System;
using UnityEngine;

public sealed class VNServiceContainer : MonoBehaviour
{
    [Header("Repository")]
    [SerializeField] private int _slotCount = 10;

    [Header("Album")]
    [SerializeField] private VNAlbumDatabaseSO _albumDatabase;

    public bool IsInitialized { get; private set; }

    public IVNSaveRepository SaveRepository { get; private set; }
    
    public IVNGlobalProgressRepository GlobalRepository { get; private set; }
    public VNGlobalProgressData GlobalData { get; private set; }

    public VNSaveService SaveService { get; private set; }
    public VNLoadService LoadService { get; private set; }
    public VNContinueService ContinueService { get; private set; }
    public VNAutoSaveService AutoSaveService { get; private set; }
    public VNAlbumUnlockService AlbumService { get; private set; }

    private IVNRuntimeStateProvider _stateProvider;
    private IVNLoadSeekDriver _seekDriver;
    private IVNFlagStore _flagStore;
    private IVNSaveSafetyPolicy _safetyPolicy;

    public void Initialize()
    {
        if (IsInitialized)
        {
            Debug.LogWarning("[VNServiceContainer] Already initialized. Skipping.");
            return;
        }

        SaveRepository = new JsonVNSaveRepository(_slotCount);
        GlobalRepository = new JsonVNGlobalProgressRepository();

        GlobalData = GlobalRepository.LoadOrCreate();

        if (GlobalData == null)
            GlobalData = new VNGlobalProgressData();

        GlobalData.Normalize();

        AlbumService = new VNAlbumUnlockService(GlobalData, GlobalRepository, _albumDatabase);

        IsInitialized = true;
    }

    public void BindRuntime(
        IVNRuntimeStateProvider stateProvider,
        IVNLoadSeekDriver seekDriver,
        IVNFlagStore flagStore,
        IVNSaveSafetyPolicy safetyPolicy)
    {
        if (!IsInitialized)
        {
            Debug.LogError("[VNServiceContainer] BindRuntime called before Initialize(). Runtime bind aborted.");
            return;
        }

        _stateProvider = stateProvider;
        _seekDriver = seekDriver;
        _flagStore = flagStore;
        _safetyPolicy = safetyPolicy;
        
        LoadService = new VNLoadService(SaveRepository, _seekDriver, _flagStore, _safetyPolicy);
        ContinueService = new VNContinueService(GlobalData, LoadService, SaveRepository);
        SaveService = new VNSaveService(SaveRepository, GlobalRepository, GlobalData, _stateProvider, _flagStore, _safetyPolicy);
        AutoSaveService = new VNAutoSaveService(SaveService);
    }

    public bool CanContinue()
    {
        if (!IsInitialized || GlobalData == null || SaveRepository == null)
            return false;

        GlobalData.Normalize();

        if (string.IsNullOrWhiteSpace(GlobalData.continueSlotId))
            return false;

        return SaveRepository.Exists(GlobalData.continueSlotId);
    }

    public bool TryContinue()
    {
        if (!IsInitialized || ContinueService == null)
        {
            Debug.LogWarning("[VNServiceContainer] Runtime is not bound. Cannot continue yet.");
            return false;
        }

        return ContinueService.Continue();
    }
}