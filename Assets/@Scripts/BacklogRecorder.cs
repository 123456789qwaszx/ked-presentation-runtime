using System.Collections.Generic;

public sealed class BacklogRecorder
{
    private readonly YarnLineLifecycleBridge _yarnLineLifecycleBridge;
    private readonly LinePresentationAdvanceState _vnLinePresentationState;
    private readonly VNTraceStream _vnTraceStream;

    private readonly int _maxCount;
    private readonly List<DialogueLogEntry> _entries;

    public IReadOnlyList<DialogueLogEntry> Entries => _entries;

    public BacklogRecorder(
        YarnLineLifecycleBridge yarnLineLifecycleBridge,
        VnPlaybackSettings vnPlaybackSettings,
        LinePresentationAdvanceState vnLinePresentationState,
        VNTraceStream trace)
    {
        _yarnLineLifecycleBridge = yarnLineLifecycleBridge;

        _maxCount =
            vnPlaybackSettings.maxLogCount <= 0
                ? 100
                : vnPlaybackSettings.maxLogCount;

        _entries = new List<DialogueLogEntry>(_maxCount);

        _vnLinePresentationState = vnLinePresentationState;
        //***
        //_vnTraceStream = trace;

        RegisterHandler();
    }

    private void RegisterHandler()
    {
        _yarnLineLifecycleBridge.LineEntered -= OnLinePrepare;
        _yarnLineLifecycleBridge.LineEntered += OnLinePrepare;

        Trace("Registered");
    }

    private void UnRegisterHandler()
    {
        if (_yarnLineLifecycleBridge == null)
            return;

        _yarnLineLifecycleBridge.LineEntered -= OnLinePrepare;

        Trace("Unregistered");
    }

    private void OnLinePrepare(YarnLineMeta meta)
    {
        Trace("LineEntered", FormatMeta(meta));

        if (_vnLinePresentationState != null && _vnLinePresentationState.IsSeekingActive)
        {
            Trace("RecordSkippedBySeek", FormatMeta(meta));
            return;
        }

        Add(new DialogueLogEntry
        {
            lineId = meta.lineId,
            nodeName = meta.nodeName,
            rawText = meta.rawText,
        });

        Trace("RecordAccepted", $"{FormatMeta(meta)}, count={_entries.Count}");
    }

    public void ClearBacklog()
    {
        int before = _entries.Count;

        _entries.Clear();

        Trace("ClearBacklog", $"before={before}, after={_entries.Count}");
    }

    private void Add(in DialogueLogEntry entry)
    {
        _entries.Add(entry);

        if (_entries.Count > _maxCount)
        {
            _entries.RemoveAt(0);
            Trace("TrimOldest", $"max={_maxCount}, count={_entries.Count}");
        }
    }

    private void Trace(string evt, string note = null)
    {
        if (_vnTraceStream == null)
            return;

        string state =
            $"count={_entries.Count}, max={_maxCount}, " +
            $"seekActive={(_vnLinePresentationState != null && _vnLinePresentationState.IsSeekingActive)}, " +
            $"isSeeking={(_vnLinePresentationState != null && _vnLinePresentationState.IsSeeking)}";

        _vnTraceStream.Trace(nameof(BacklogRecorder), evt, state, note);
    }

    private static string FormatMeta(YarnLineMeta meta)
    {
        return $"meta={meta.nodeName}/{meta.lineId}, char='{meta.charName}'";
    }
}

// using System.Collections.Generic;
//
// public sealed class BacklogRecorder
// {
//     private readonly YarnLineLifecycleBridge _yarnLineLifecycleBridge;
//     private readonly LinePresentationAdvanceState _vnLinePresentationState;
//     private readonly VNTraceStream _vnTraceStream;
//     
//     private readonly int _maxCount;
//     private readonly List<DialogueLogEntry> _entries;
//
//     public IReadOnlyList<DialogueLogEntry> Entries => _entries;
//
//     public BacklogRecorder(
//         YarnLineLifecycleBridge yarnLineLifecycleBridge,
//         VnPlaybackSettings vnPlaybackSettings,
//         LinePresentationAdvanceState vnLinePresentationState,
//         VNTraceStream trace = null)
//     {
//         _yarnLineLifecycleBridge = yarnLineLifecycleBridge;
//         
//         _maxCount = 
//             vnPlaybackSettings.maxLogCount <= 0 ?
//                 100 : vnPlaybackSettings.maxLogCount;
//         _entries = new List<DialogueLogEntry>(_maxCount);
//         
//         _vnLinePresentationState = vnLinePresentationState;
//         _vnTraceStream = trace;
//
//         RegisterHandler();
//     }
//     
//     private void RegisterHandler()
//     {
//         _yarnLineLifecycleBridge.LineEntered -= OnLinePrepare;
//         _yarnLineLifecycleBridge.LineEntered += OnLinePrepare;
//     }
//
//     private void UnRegisterHandler()
//     {
//         if (_yarnLineLifecycleBridge == null) return;
//
//         _yarnLineLifecycleBridge.LineEntered -= OnLinePrepare;
//     }
//     
//     private void OnLinePrepare(YarnLineMeta meta)
//     {
//         Add(new DialogueLogEntry
//         {
//             lineId = meta.lineId,
//             nodeName = meta.nodeName,
//             rawText = meta.rawText,
//         });
//     }
//     
//     public void ClearBacklog() => _entries.Clear();
//
//     private void Add(in DialogueLogEntry entry)
//     {
//         if (_vnLinePresentationState != null && _vnLinePresentationState.IsSeekingActive)
//         {
//             return;
//         }
//         _entries.Add(entry);
//
//         // 매우 단순한 트림(필요하면 링버퍼로 교체)
//         if (_entries.Count > _maxCount)
//             _entries.RemoveAt(0);
//     }
// }