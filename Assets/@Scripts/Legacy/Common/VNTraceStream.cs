// using System;
// using System.Text;
// using UnityEngine;
//
// [Serializable]
// public sealed class VNTraceStream
// {
//     public bool enableTrace = true;
//
//     [Tooltip("If true, every Trace() call is printed immediately. Usually keep this false and use DumpToConsole().")]
//     public bool logTraceStreaming = false;
//
//     [Tooltip("If true, DumpToConsole() prints the whole trace buffer as one log.")]
//     public bool logDumpToConsole = true;
//
//     [TextArea(8, 30)]
//     public string tracePreview;
//
//     private readonly StringBuilder _trace = new StringBuilder(4096);
//     private const int MaxTraceChars = 20000;
//
//     private int _seq;
//
//     public void Trace(
//         string source,
//         string evt,
//         string state = null,
//         string note = null,
//         UnityEngine.Object context = null)
//     {
//         if (!enableTrace)
//             return;
//
//         TrimIfNeeded();
//
//         _seq++;
//
//         string statePart = string.IsNullOrWhiteSpace(state)
//             ? ""
//             : $" | {state}";
//
//         string notePart = string.IsNullOrWhiteSpace(note)
//             ? ""
//             : $" | {note}";
//
//         string line = $"[{Time.frameCount}] #{_seq:0000} {source}.{evt}{statePart}{notePart}";
//
//         _trace.AppendLine(line);
//         tracePreview = _trace.ToString();
//
//         if (logTraceStreaming)
//             Debug.Log($"[VNTrace] {line}", context);
//     }
//
//     public string GetDump(string title = null)
//     {
//         string headerTitle = string.IsNullOrWhiteSpace(title)
//             ? "VN TRACE DUMP"
//             : title;
//
//         StringBuilder dump = new StringBuilder(_trace.Length + 512);
//
//         dump.AppendLine("============================================================");
//         dump.AppendLine($"[VNTrace] {headerTitle}");
//         dump.AppendLine($"frame={Time.frameCount}, seq={_seq}, chars={_trace.Length}");
//         dump.AppendLine("============================================================");
//
//         if (_trace.Length <= 0)
//             dump.AppendLine("(empty)");
//         else
//             dump.Append(_trace);
//
//         dump.AppendLine("============================================================");
//         dump.AppendLine("[VNTrace] END");
//         dump.AppendLine("============================================================");
//
//         return dump.ToString();
//     }
//
//     public void DumpToConsole(string title = null, UnityEngine.Object context = null)
//     {
//         if (!enableTrace || !logDumpToConsole)
//             return;
//
//         Debug.Log(GetDump(title), context);
//     }
//
//     public void DumpPreviewToConsole(string title = null, UnityEngine.Object context = null)
//     {
//         if (!enableTrace || !logDumpToConsole)
//             return;
//
//         string headerTitle = string.IsNullOrWhiteSpace(title)
//             ? "VN TRACE PREVIEW DUMP"
//             : title;
//
//         string body = string.IsNullOrWhiteSpace(tracePreview)
//             ? "(empty)"
//             : tracePreview;
//
//         Debug.Log(
//             "============================================================\n" +
//             $"[VNTrace] {headerTitle}\n" +
//             $"frame={Time.frameCount}, seq={_seq}, previewChars={(tracePreview == null ? 0 : tracePreview.Length)}\n" +
//             "============================================================\n" +
//             body +
//             "\n============================================================\n" +
//             "[VNTrace] END\n" +
//             "============================================================",
//             context);
//     }
//
//     public void DumpAndClear(string title = null, UnityEngine.Object context = null)
//     {
//         DumpToConsole(title, context);
//         Clear();
//     }
//
//     public void Clear(UnityEngine.Object context = null)
//     {
//         _seq = 0;
//         _trace.Length = 0;
//         tracePreview = "";
//
//         if (logTraceStreaming)
//             Debug.Log("[VNTrace] Cleared", context);
//     }
//
//     private void TrimIfNeeded()
//     {
//         if (_trace.Length <= MaxTraceChars)
//             return;
//
//         _trace.Remove(0, _trace.Length - (MaxTraceChars / 2));
//
//         _trace.Insert(0, "[VNTrace] --- trimmed old trace lines ---\n");
//     }
// }