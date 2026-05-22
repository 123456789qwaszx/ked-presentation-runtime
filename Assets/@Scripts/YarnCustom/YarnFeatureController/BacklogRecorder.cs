using System.Collections.Generic;

public sealed class BacklogRecorder
{
    private readonly YarnLineLifecycleBridge _yarnLineLifecycleBridge;
    
    private readonly int _maxCount;
    private readonly List<DialogueLogEntry> _entries;

    public IReadOnlyList<DialogueLogEntry> Entries => _entries;

    public BacklogRecorder(
        YarnLineLifecycleBridge yarnLineLifecycleBridge,
        VnPlaybackSettings vnPlaybackSettings)
    {
        _yarnLineLifecycleBridge = yarnLineLifecycleBridge;
        
        _maxCount = 
            vnPlaybackSettings.maxLogCount <= 0 ?
                100 : vnPlaybackSettings.maxLogCount;
        _entries = new List<DialogueLogEntry>(_maxCount);

        RegisterHandler();
    }
    
    private void RegisterHandler()
    {
        _yarnLineLifecycleBridge.LineEntered -= OnLinePrepare;
        _yarnLineLifecycleBridge.LineEntered += OnLinePrepare;
    }

    private void UnRegisterHandler()
    {
        if (_yarnLineLifecycleBridge == null) return;

        _yarnLineLifecycleBridge.LineEntered -= OnLinePrepare;
    }
    
    private void OnLinePrepare(YarnLineMeta meta)
    {
        Add(new DialogueLogEntry
        {
            lineId = meta.lineId,
            nodeName = meta.nodeName,
            rawText = meta.rawText,
        });
    }
    
    public void ClearBacklog() => _entries.Clear();

    private void Add(in DialogueLogEntry entry)
    {
        _entries.Add(entry);

        // 매우 단순한 트림(필요하면 링버퍼로 교체)
        if (_entries.Count > _maxCount)
            _entries.RemoveAt(0);
    }
}