using System;
using System.Collections.Generic;

public sealed partial class VNScreenBindings : IPresentationView, IDisposable
{
    private readonly UIManager _ui;
    private UIManager UI => _ui;
    
    // 재생 — 어떤 예제든 있다.
    private VNFeatureController _vnFeatures;
    private DialogueAdvanceDispatcher _dialogueAdvanceDispatcher;
    private IPlaybackHost _playbackHost;

    // 진행·저장 — 진행·저장 예제에만 있다. 없으면 타이틀·저장·불러오기·포크도 없다.
    private ProgressionLauncher _progressionLauncher;
    private SaveSlotService _saveSlots;
    private ManualSaveFlow _manualSaveFlow;

    private bool HasProgression => _progressionLauncher != null;

    public VNScreenBindings(UIManager uiManager)
    {
        _ui = uiManager;
    }
    
    
    public void ConfigurePlayback(
        VNFeatureController vnFeatures,
        DialogueAdvanceDispatcher dialogueAdvanceDispatcher,
        IPlaybackHost playbackHost)
    {
        _vnFeatures = vnFeatures;
        _dialogueAdvanceDispatcher = dialogueAdvanceDispatcher;
        _playbackHost = playbackHost;
    }

    public void ConfigureProgression(
        ProgressionLauncher progressionLauncher,
        SaveSlotService saveSlots,
        ManualSaveFlow manualSaveFlow)
    {
        _progressionLauncher = progressionLauncher;
        _saveSlots = saveSlots;
        _manualSaveFlow = manualSaveFlow;
    }
    
    public void OpenTitleMenu() => GoToTitle();
    
    /// <summary>
    /// Closes the top panel and releases its VNScreenBindings cleanup entries.
    /// </summary>
    private void ClosePanel()
    {
        UI.PopPanel(Unbind);
    }

    private void CloseAllPanels()
    {
        UI.PopAllPanels(Unbind);
    }
    
    #region UIContext
    
    private readonly Dictionary<UIBase, List<Action>> _cleanupByOwner = new();
    
    private void BindView<T>(T owner, Action<T> apply)
        where T : UIBase
    {
        Unbind(owner);
        apply(owner);
    }

    private void AddBinding<T>(T owner, Action<T> attach, Action<T> detach)
        where T : UIBase
    {
        attach(owner);
        AddCleanup(owner, () => detach(owner));
    }

    private void AddCleanup(UIBase owner, Action cleanup)
    {
        if (!_cleanupByOwner.TryGetValue(owner, out List<Action> cleanups))
        {
            cleanups = new List<Action>();
            _cleanupByOwner[owner] = cleanups;
        }

        cleanups.Add(cleanup);
    }

    private void Unbind(UIBase owner)
    {
        if (!_cleanupByOwner.TryGetValue(owner, out List<Action> cleanups))
            return;

        RunCleanups(cleanups);
        _cleanupByOwner.Remove(owner);
    }

    private void UnbindAll()
    {
        foreach (var kv in _cleanupByOwner)
            RunCleanups(kv.Value);

        _cleanupByOwner.Clear();
    }

    private static void RunCleanups(List<Action> cleanups)
    {
        for (int i = cleanups.Count - 1; i >= 0; i--)
            cleanups[i]?.Invoke();
    }

    public void Dispose()
    {
        UnbindAll();
    }
    
    #endregion
}