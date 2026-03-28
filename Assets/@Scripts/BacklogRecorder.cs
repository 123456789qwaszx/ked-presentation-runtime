using System.Collections.Generic;

public sealed class BacklogRecorder
{
    private readonly YarnLineLifecycleBridge _yarnLineLifecycleBridge;
    private readonly UnityTimeSource _unityTimeSource;
    
    private readonly List<DialogueLogEntry> _entries;
    private readonly int _maxCount;

    public IReadOnlyList<DialogueLogEntry> Entries => _entries;

    public BacklogRecorder(
        YarnLineLifecycleBridge yarnLineLifecycleBridge,
        UnityTimeSource unityTimeSource,
        int maxCount)
    {
        _yarnLineLifecycleBridge = yarnLineLifecycleBridge;
        _unityTimeSource = unityTimeSource;
        
        _maxCount = maxCount <= 0 ?
            100 
            : maxCount;
        _entries = new List<DialogueLogEntry>(_maxCount);

        RegisterHandler();
    }
    
    private void RegisterHandler()
    {
        _yarnLineLifecycleBridge.LineStart -= OnLineStart;
        _yarnLineLifecycleBridge.LineStart += OnLineStart;
    }

    private void UnRegisterHandler()
    {
        if (_yarnLineLifecycleBridge == null) return;

        _yarnLineLifecycleBridge.LineStart -= OnLineStart;
    }

    public void Clear() => _entries.Clear();

    public void Add(in DialogueLogEntry entry)
    {
        _entries.Add(entry);

        // 매우 단순한 트림(필요하면 링버퍼로 교체)
        if (_entries.Count > _maxCount)
            _entries.RemoveAt(0);
    }
    
    
    // ---- Yarn Lifecycle handlers ----
    private void OnLineStart(YarnLineMeta meta)
    {
        Add(new DialogueLogEntry
        {
            lineId = meta.lineId,
            lineSerial = meta.lineSerial,
            nodeName = meta.nodeName,
            rawText = meta.rawText,
            timestamp = _unityTimeSource.UnscaledDeltaTime
        });
    }
}