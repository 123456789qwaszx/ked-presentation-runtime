using System;
using System.Text;
using UnityEngine;

[Serializable]
public sealed class VNTraceStream
{
    [Header("Enable")]
    public bool enableTrace = true;

    [Tooltip("If true, every Trace() call is printed immediately. Usually keep this false and use DumpToConsole().")]
    public bool logTraceStreaming = false;

    [Tooltip("If true, DumpToConsole() prints the whole trace buffer as one log.")]
    public bool logDumpToConsole = true;

    [Header("Buffer")]
    [Tooltip("If false, trace keeps growing until Clear() is called. Useful for focused debugging.")]
    public bool trimOldTrace = true;

    [Tooltip("Maximum trace buffer size before trimming. Increase this for rollback/load seek debugging.")]
    [Min(4096)]
    public int maxTraceChars = 200000;

    [Tooltip("When trimming, keep this ratio of the newest trace buffer.")]
    [Range(0.1f, 0.9f)]
    public float trimKeepRatio = 0.5f;

    [Tooltip("If true, DumpAndClear() clears the trace after dumping.")]
    public bool clearAfterDump = false;

    [Header("Preview")]
    [TextArea(8, 30)]
    public string tracePreview;

    private readonly StringBuilder _trace = new StringBuilder(4096);

    private int _seq;
    private int _trimCount;
    private int _droppedChars;

    public int Sequence => _seq;
    public int Length => _trace.Length;
    public int TrimCount => _trimCount;
    public int DroppedChars => _droppedChars;
    public bool HasTrace => _trace.Length > 0;

    public void Trace(
        string source,
        string evt,
        string state = null,
        string note = null,
        UnityEngine.Object context = null)
    {
        if (!enableTrace)
            return;

        _seq++;

        string statePart = string.IsNullOrWhiteSpace(state)
            ? ""
            : $" | {state}";

        string notePart = string.IsNullOrWhiteSpace(note)
            ? ""
            : $" | {note}";

        string line = $"[{Time.frameCount}] #{_seq:0000} {source}.{evt}{statePart}{notePart}";

        _trace.AppendLine(line);

        TrimIfNeeded();
        RefreshPreview();

        if (logTraceStreaming)
            Debug.Log($"[VNTrace] {line}", context);
    }

    public string GetDump(string title = null)
    {
        string headerTitle = string.IsNullOrWhiteSpace(title)
            ? "VN TRACE DUMP"
            : title;

        StringBuilder dump = new StringBuilder(_trace.Length + 768);

        dump.AppendLine("============================================================");
        dump.AppendLine($"[VNTrace] {headerTitle}");
        dump.AppendLine(
            $"frame={Time.frameCount}, seq={_seq}, chars={_trace.Length}, trims={_trimCount}, droppedChars={_droppedChars}");
        dump.AppendLine("============================================================");

        if (_trace.Length <= 0)
            dump.AppendLine("(empty)");
        else
            dump.Append(_trace);

        dump.AppendLine("============================================================");
        dump.AppendLine("[VNTrace] END");
        dump.AppendLine("============================================================");

        return dump.ToString();
    }

    public void DumpToConsole(string title = null, UnityEngine.Object context = null)
    {
        if (!enableTrace || !logDumpToConsole)
            return;

        Debug.Log(GetDump(title), context);

        if (clearAfterDump)
            Clear(context);
    }

    public void DumpPreviewToConsole(string title = null, UnityEngine.Object context = null)
    {
        if (!enableTrace || !logDumpToConsole)
            return;

        string headerTitle = string.IsNullOrWhiteSpace(title)
            ? "VN TRACE PREVIEW DUMP"
            : title;

        string body = string.IsNullOrWhiteSpace(tracePreview)
            ? "(empty)"
            : tracePreview;

        Debug.Log(
            "============================================================\n" +
            $"[VNTrace] {headerTitle}\n" +
            $"frame={Time.frameCount}, seq={_seq}, previewChars={(tracePreview == null ? 0 : tracePreview.Length)}, trims={_trimCount}, droppedChars={_droppedChars}\n" +
            "============================================================\n" +
            body +
            "\n============================================================\n" +
            "[VNTrace] END\n" +
            "============================================================",
            context);
    }

    public void DumpAndClear(string title = null, UnityEngine.Object context = null)
    {
        if (!enableTrace || !logDumpToConsole)
            return;

        Debug.Log(GetDump(title), context);
        Clear(context);
    }

    public void Clear(UnityEngine.Object context = null)
    {
        _seq = 0;
        _trimCount = 0;
        _droppedChars = 0;

        _trace.Length = 0;
        tracePreview = string.Empty;

        if (logTraceStreaming)
            Debug.Log("[VNTrace] Cleared", context);
    }

    public void Mark(string label, UnityEngine.Object context = null)
    {
        Trace(nameof(VNTraceStream), "Mark", null, label, context);
    }

    private void TrimIfNeeded()
    {
        if (!trimOldTrace)
            return;

        if (maxTraceChars <= 0)
            return;

        if (_trace.Length <= maxTraceChars)
            return;

        int keepChars = Mathf.Clamp(
            Mathf.RoundToInt(maxTraceChars * trimKeepRatio),
            1024,
            maxTraceChars);

        int removeChars = _trace.Length - keepChars;
        if (removeChars <= 0)
            return;

        _trace.Remove(0, removeChars);

        _trimCount++;
        _droppedChars += removeChars;

        string trimHeader =
            "[VNTrace] --- trimmed old trace lines --- " +
            $"trimCount={_trimCount}, droppedChars={_droppedChars}, keptChars={_trace.Length}\n";

        _trace.Insert(0, trimHeader);
    }

    private void RefreshPreview()
    {
        tracePreview = _trace.ToString();
    }
}