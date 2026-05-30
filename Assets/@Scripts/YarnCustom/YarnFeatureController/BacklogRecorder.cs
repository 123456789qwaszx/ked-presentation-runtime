using System.Collections.Generic;

public sealed class BacklogRecorder
{
    private const int MAXLOGCOUNT = 100;
    
    private readonly YarnLineLifecycleBridge _yarnLineLifecycleBridge;
    private readonly LinePresentationAdvanceState _vnLinePresentationState;
    private readonly VNTraceStream _vnTraceStream;

    private readonly List<DialogueLogEntry> _entries;

    public IReadOnlyList<DialogueLogEntry> Entries => _entries;

    public BacklogRecorder(
        YarnLineLifecycleBridge yarnLineLifecycleBridge,
        LinePresentationAdvanceState vnLinePresentationState)
    {
        _yarnLineLifecycleBridge = yarnLineLifecycleBridge;

        _entries = new List<DialogueLogEntry>(MAXLOGCOUNT);

        _vnLinePresentationState = vnLinePresentationState;

        RegisterHandler();
    }

    private void RegisterHandler()
    {
        _yarnLineLifecycleBridge.LineEntered -= OnLinePrepare;
        _yarnLineLifecycleBridge.LineEntered += OnLinePrepare;
    }

    private void UnRegisterHandler()
    {
        if (_yarnLineLifecycleBridge == null)
            return;

        _yarnLineLifecycleBridge.LineEntered -= OnLinePrepare;
    }

    private void OnLinePrepare(YarnLineMeta meta)
    {
        if (_vnLinePresentationState.IsSeekingActive)
            return;

        Add(new DialogueLogEntry
        {
            lineId = meta.lineId,
            nodeName = meta.nodeName,
            rawText = meta.rawText,
        });
    }

    public void ClearBacklog()
    {
        int before = _entries.Count;

        _entries.Clear();
    }

    private void Add(in DialogueLogEntry entry)
    {
        _entries.Add(entry);

        if (_entries.Count > MAXLOGCOUNT)
        {
            _entries.RemoveAt(0);
        }
    }
}