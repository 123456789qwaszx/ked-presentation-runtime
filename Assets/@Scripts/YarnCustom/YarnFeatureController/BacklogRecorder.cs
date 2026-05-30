using System.Collections.Generic;

public sealed class BacklogRecorder
{
    private const int MAXLOGCOUNT = 100;
    private readonly List<DialogueLogEntry> _entries = new(MAXLOGCOUNT);
    
    public IReadOnlyList<DialogueLogEntry> Entries => _entries;

    public void Record(YarnLineMeta meta)
    {
        Add(new DialogueLogEntry
        {
            lineId = meta.lineId,
            nodeName = meta.nodeName,
            rawText = meta.rawText,
        });
    }

    public void ClearBacklog()
    {
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