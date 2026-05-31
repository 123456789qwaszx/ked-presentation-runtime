using System;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Yarn.Unity;

public sealed class VNRuntimeStateInspectorWindow : EditorWindow
{
    private Vector2 _scroll;
    private string _dump = "";
    private bool _autoRefresh;
    private double _nextRefreshTime;

    [MenuItem("Tools/VN/Runtime State Inspector")]
    public static void Open()
    {
        GetWindow<VNRuntimeStateInspectorWindow>("VN Runtime State");
    }

    private void OnEnable()
    {
        Refresh();
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        if (!_autoRefresh)
            return;

        if (EditorApplication.timeSinceStartup < _nextRefreshTime)
            return;

        _nextRefreshTime = EditorApplication.timeSinceStartup + 0.5d;
        Refresh();
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Refresh", GUILayout.Width(120)))
                Refresh();

            if (GUILayout.Button("Dump To Console", GUILayout.Width(140)))
                Debug.Log(_dump);

            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", GUILayout.Width(120));
        }

        EditorGUILayout.Space();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to inspect runtime VN state.", MessageType.Info);
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.TextArea(_dump, GUILayout.ExpandHeight(true));

        EditorGUILayout.EndScrollView();
    }

    private void Refresh()
    {
        StringBuilder sb = new StringBuilder(8192);

        AppendHeader(sb);

        VnAppBootstrap bootstrap = FindFirstObjectByTypeSafe<VnAppBootstrap>();

        if (bootstrap == null)
        {
            sb.AppendLine("VnAppBootstrap: NOT FOUND");
            _dump = sb.ToString();
            return;
        }

        sb.AppendLine("VnAppBootstrap: FOUND");
        sb.AppendLine($"GameObject: {bootstrap.gameObject.name}");
        sb.AppendLine();

        AppendBootstrapState(sb, bootstrap);
        AppendDialogueRunnerState(sb, bootstrap, "dialogueRunner");
        AppendDialogueRunnerState(sb, bootstrap, "subPresentationRunner");
        AppendCustomLinePresenterState(sb, bootstrap);
        AppendLineStateMachineState(sb, bootstrap);
        AppendSideRunnerSyncState(sb, bootstrap);

        _dump = sb.ToString();
    }

    private static void AppendHeader(StringBuilder sb)
    {
        sb.AppendLine("===== VN RUNTIME STATE INSPECTOR =====");
        sb.AppendLine($"PlayMode: {Application.isPlaying}");
        sb.AppendLine($"Frame: {Time.frameCount}");
        sb.AppendLine($"Time: {DateTime.Now:HH:mm:ss.fff}");
        sb.AppendLine();
    }

    private static void AppendBootstrapState(StringBuilder sb, VnAppBootstrap bootstrap)
    {
        sb.AppendLine("----- AppBootstrap Runtime Fields -----");

        object rollbackHistory = GetFieldValue(bootstrap, "_rollbackHistory");
        AppendRollbackHistory(sb, rollbackHistory);

        object lineState = GetFieldValue(bootstrap, "_linePresentationAdvanceState");
        AppendLinePresentationState(sb, lineState);

        object backlogRecorder = GetFieldValue(bootstrap, "_backlogRecorder");
        AppendObjectSummary(sb, "BacklogRecorder", backlogRecorder);

        object loadSeekDriver = GetFieldValue(bootstrap, "_vnLoadSeekDriver");
        AppendLoadSeekDriver(sb, loadSeekDriver);

        object runtimeStateProvider = GetFieldValue(bootstrap, "_vnRuntimeStateProvider");
        AppendObjectSummary(sb, "VNRuntimeStateProvider", runtimeStateProvider);

        object presentationContext = GetFieldValue(bootstrap, "_presentationSessionContext");
        AppendObjectSummary(sb, "PresentationSessionContext", presentationContext);

        sb.AppendLine();
    }

    private static void AppendRollbackHistory(StringBuilder sb, object history)
    {
        sb.AppendLine("[RollbackHistory]");

        if (history == null)
        {
            sb.AppendLine("  null");
            return;
        }

        object pointsObj = GetPropertyValue(history, "Points");
        int count = TryGetCollectionCount(pointsObj);

        sb.AppendLine($"  Type: {history.GetType().Name}");
        sb.AppendLine($"  Points.Count: {count}");
        sb.AppendLine($"  CanRollbackOneStep: {GetPropertyValue(history, "CanRollbackOneStep")}");
        sb.AppendLine($"  _nextHistoryIndex: {GetFieldValue(history, "_nextHistoryIndex")}");

        IEnumerable points = pointsObj as IEnumerable;
        if (points == null)
            return;

        int index = 0;
        foreach (object point in points)
        {
            sb.AppendLine($"  [{index}] {FormatRollbackPoint(point)}");
            index++;

            if (index >= 12)
            {
                sb.AppendLine("  ... truncated");
                break;
            }
        }
    }

    private static string FormatRollbackPoint(object point)
    {
        if (point == null)
            return "null";

        Type type = point.GetType();

        object historyIndex = GetFieldValueByType(type, point, "historyIndex");
        object nodeName = GetFieldValueByType(type, point, "nodeName");
        object lineId = GetFieldValueByType(type, point, "lineId");
        object rawText = GetFieldValueByType(type, point, "rawText");

        string text = rawText != null ? rawText.ToString() : "";
        if (text.Length > 60)
            text = text.Substring(0, 60) + "...";

        return $"historyIndex={historyIndex}, node={nodeName}, line={lineId}, text='{text}'";
    }

    private static void AppendLinePresentationState(StringBuilder sb, object lineState)
    {
        sb.AppendLine("[VNLinePresentationState]");

        if (lineState == null)
        {
            sb.AppendLine("  null");
            return;
        }

        sb.AppendLine($"  Type: {lineState.GetType().Name}");
        sb.AppendLine($"  Snapshot: {InvokeStringMethod(lineState, "Snapshot")}");
        sb.AppendLine($"  SeekKind: {GetPropertyValue(lineState, "SeekKind")}");
        sb.AppendLine($"  SeekPhase: {GetPropertyValue(lineState, "SeekPhase")}");
        sb.AppendLine($"  IsSeekingActive: {GetPropertyValue(lineState, "IsSeekingActive")}");
        sb.AppendLine($"  IsSeekPassingThrough: {GetPropertyValue(lineState, "IsSeekPassingThrough")}");
        sb.AppendLine($"  IsLineFullyShown: {GetPropertyValue(lineState, "IsLineFullyShown")}");
        sb.AppendLine($"  CanRecordRollbackPoint: {GetPropertyValue(lineState, "CanRecordRollbackPoint")}");
        sb.AppendLine($"  SeekTargetNodeName: {FormatNull(GetPropertyValue(lineState, "SeekTargetNodeName"))}");
        sb.AppendLine($"  SeekTargetLineId: {FormatNull(GetPropertyValue(lineState, "SeekTargetLineId"))}");

        object seekState = GetFieldValue(lineState, "_seekState");
        AppendSeekState(sb, seekState);
    }

    private static void AppendSeekState(StringBuilder sb, object seekState)
    {
        sb.AppendLine("  [VNSeekState]");

        if (seekState == null)
        {
            sb.AppendLine("    null");
            return;
        }

        sb.AppendLine($"    Snapshot: {InvokeStringMethod(seekState, "Snapshot")}");
        sb.AppendLine($"    Kind: {GetPropertyValue(seekState, "Kind")}");
        sb.AppendLine($"    Phase: {GetPropertyValue(seekState, "Phase")}");
        sb.AppendLine($"    TargetNodeName: {FormatNull(GetPropertyValue(seekState, "TargetNodeName"))}");
        sb.AppendLine($"    TargetLineId: {FormatNull(GetPropertyValue(seekState, "TargetLineId"))}");
        sb.AppendLine($"    PendingNodeName: {FormatNull(GetPropertyValue(seekState, "PendingNodeName"))}");
        sb.AppendLine($"    PendingLineId: {FormatNull(GetPropertyValue(seekState, "PendingLineId"))}");
        sb.AppendLine($"    IsActive: {GetPropertyValue(seekState, "IsActive")}");
        sb.AppendLine($"    IsSeeking: {GetPropertyValue(seekState, "IsSeeking")}");
    }

    private static void AppendLoadSeekDriver(StringBuilder sb, object driver)
    {
        sb.AppendLine("[VNLoadSeekDriver]");

        if (driver == null)
        {
            sb.AppendLine("  null");
            return;
        }

        sb.AppendLine($"  Type: {driver.GetType().Name}");
        sb.AppendLine($"  IsActive: {GetPropertyValue(driver, "IsActive")}");

        object target = GetPropertyValue(driver, "Target");
        sb.AppendLine($"  Target: {FormatSaveData(target)}");

        sb.AppendLine($"  _onComplete: {FormatDelegate(GetFieldValue(driver, "_onComplete"))}");
        sb.AppendLine($"  _onFail: {FormatDelegate(GetFieldValue(driver, "_onFail"))}");
    }

    private static string FormatSaveData(object saveData)
    {
        if (saveData == null)
            return "null";

        Type type = saveData.GetType();

        object nodeName = GetFieldValueByType(type, saveData, "nodeName");
        object lineId = GetFieldValueByType(type, saveData, "lineId");
        object playtimeSeconds = GetFieldValueByType(type, saveData, "playtimeSeconds");

        return $"node={nodeName}, line={lineId}, playtime={playtimeSeconds}";
    }

    private static void AppendDialogueRunnerState(StringBuilder sb, VnAppBootstrap bootstrap, string fieldName)
    {
        sb.AppendLine($"----- DialogueRunner: {fieldName} -----");

        DialogueRunner runner = GetFieldValue(bootstrap, fieldName) as DialogueRunner;
        if (runner == null)
        {
            sb.AppendLine("  null");
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"  Object: {runner.name}");
        sb.AppendLine($"  IsDialogueRunning: {runner.IsDialogueRunning}");
        sb.AppendLine($"  DialogueTask completed: {runner.DialogueTask.IsCompletedSuccessfully()}");

        object dialogue = GetPropertyValue(runner, "Dialogue");
        sb.AppendLine($"  Dialogue object: {FormatObject(dialogue)}");

        sb.AppendLine($"  dialogueCancellationSource: {FormatCancellationSource(GetFieldValue(runner, "dialogueCancellationSource"))}");
        sb.AppendLine($"  currentLineCancellationSource: {FormatCancellationSource(GetFieldValue(runner, "currentLineCancellationSource"))}");
        sb.AppendLine($"  currentLineHurryUpSource: {FormatCancellationSource(GetFieldValue(runner, "currentLineHurryUpSource"))}");
        sb.AppendLine($"  currentOptionsCancellationSource: {FormatCancellationSource(GetFieldValue(runner, "currentOptionsCancellationSource"))}");
        sb.AppendLine($"  currentOptionsHurryUpSource: {FormatCancellationSource(GetFieldValue(runner, "currentOptionsHurryUpSource"))}");
        sb.AppendLine($"  dialogueCompletionSource: {FormatObject(GetFieldValue(runner, "dialogueCompletionSource"))}");
        sb.AppendLine($"  dialogueCancellationCompletion: {FormatObject(GetFieldValue(runner, "dialogueCancellationCompletion"))}");

        object presenters = GetPropertyValue(runner, "DialoguePresenters");
        AppendPresenters(sb, presenters);

        sb.AppendLine();
    }

    private static void AppendPresenters(StringBuilder sb, object presentersObj)
    {
        sb.AppendLine("  Presenters:");

        IEnumerable presenters = presentersObj as IEnumerable;
        if (presenters == null)
        {
            sb.AppendLine("    null");
            return;
        }

        int index = 0;
        foreach (object presenter in presenters)
        {
            MonoBehaviour behaviour = presenter as MonoBehaviour;
            string name = behaviour != null ? behaviour.name : FormatObject(presenter);
            bool enabled = behaviour != null && behaviour.enabled;

            sb.AppendLine($"    [{index}] {name}, type={presenter.GetType().Name}, enabled={enabled}");
            index++;
        }
    }

    private static void AppendCustomLinePresenterState(StringBuilder sb, VnAppBootstrap bootstrap)
    {
        sb.AppendLine("----- CustomLinePresenter -----");

        object presenter = GetFieldValue(bootstrap, "customLinePresenter");
        if (presenter == null)
        {
            sb.AppendLine("  null");
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"  Object: {FormatObject(presenter)}");
        sb.AppendLine($"  _currentNodeName: {FormatNull(GetFieldValue(presenter, "_currentNodeName"))}");
        sb.AppendLine($"  _presenterGeneration: {GetFieldValue(presenter, "_presenterGeneration")}");
        sb.AppendLine($"  _presenterLifetimeCts: {FormatCancellationSource(GetFieldValue(presenter, "_presenterLifetimeCts"))}");
        sb.AppendLine($"  _lineVisualCts: {FormatCancellationSource(GetFieldValue(presenter, "_lineVisualCts"))}");

        object lineState = GetFieldValue(presenter, "_lineAdvanceState");
        sb.AppendLine($"  _lineAdvanceState: {InvokeStringMethod(lineState, "Snapshot")}");

        object context = GetFieldValue(presenter, "_presentationSessionContext");
        AppendObjectSummary(sb, "  _presentationSessionContext", context);

        object typewriter = GetFieldValue(presenter, "_typewriter");
        AppendObjectSummary(sb, "  _typewriter", typewriter);

        object boxPresentation = GetFieldValue(presenter, "_boxPresentation");
        AppendDialogueBoxPresentation(sb, boxPresentation);

        sb.AppendLine();
    }

    private static void AppendDialogueBoxPresentation(StringBuilder sb, object boxPresentation)
    {
        sb.AppendLine("  [DialogueBoxPresentationController]");

        if (boxPresentation == null)
        {
            sb.AppendLine("    null");
            return;
        }

        sb.AppendLine($"    CurrentPhase: {GetPropertyValue(boxPresentation, "CurrentPhase")}");

        object boxState = GetFieldValue(boxPresentation, "_boxState");
        if (boxState == null)
        {
            sb.AppendLine("    _boxState: null");
            return;
        }

        sb.AppendLine($"    _boxState.Type: {boxState.GetType().Name}");
        sb.AppendLine($"    BoxKind: {GetPropertyValue(boxState, "BoxKind")}");
        sb.AppendLine($"    IsVisible: {GetPropertyValue(boxState, "IsVisible")}");
        sb.AppendLine($"    Box: {FormatObject(GetPropertyValue(boxState, "Box"))}");
    }

    private static void AppendLineStateMachineState(StringBuilder sb, VnAppBootstrap bootstrap)
    {
        sb.AppendLine("----- VNLinePresentationStateMachine -----");

        object presenter = GetFieldValue(bootstrap, "customLinePresenter");
        object machine = presenter != null
            ? GetFieldValue(presenter, "_vnLinePresentationStateMachine")
            : null;

        if (machine == null)
        {
            sb.AppendLine("  null");
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"  CurrentPhase: {GetPropertyValue(machine, "CurrentPhase")}");

        object committer = GetFieldValue(machine, "_committer");
        object currentMeta = committer != null ? GetFieldValue(committer, "_currentMeta") : null;

        sb.AppendLine($"  Committer._currentMeta: {FormatYarnLineMeta(currentMeta)}");
        sb.AppendLine();
    }

    private static string FormatYarnLineMeta(object meta)
    {
        if (meta == null)
            return "null";

        Type type = meta.GetType();

        object nodeName = GetFieldValueByType(type, meta, "nodeName");
        object lineId = GetFieldValueByType(type, meta, "lineId");
        object charName = GetFieldValueByType(type, meta, "charName");
        object rawText = GetFieldValueByType(type, meta, "rawText");

        string text = rawText != null ? rawText.ToString() : "";
        if (text.Length > 60)
            text = text.Substring(0, 60) + "...";

        return $"node={nodeName}, line={lineId}, char={charName}, text='{text}'";
    }

    private static void AppendSideRunnerSyncState(StringBuilder sb, VnAppBootstrap bootstrap)
    {
        sb.AppendLine("----- VNSideRunnerSyncHub -----");

        object hub = GetFieldValue(bootstrap, "_vnSideRunnerSyncHub");
        if (hub == null)
        {
            sb.AppendLine("  null");
            sb.AppendLine();
            return;
        }

        object lanes = GetFieldValue(hub, "_lanes");
        IDictionary dictionary = lanes as IDictionary;

        if (dictionary == null)
        {
            sb.AppendLine($"  _lanes: {FormatObject(lanes)}");
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"  Lane count: {dictionary.Count}");

        foreach (DictionaryEntry entry in dictionary)
        {
            object lane = entry.Value;
            sb.AppendLine($"  Lane key: {entry.Key}");

            if (lane == null)
            {
                sb.AppendLine("    null");
                continue;
            }

            sb.AppendLine($"    Snapshot: {InvokeStringMethod(lane, "Snapshot")}");
            sb.AppendLine($"    PendingAdvanceCount: {GetFieldValue(lane, "PendingAdvanceCount")}");
            sb.AppendLine($"    IsReadyForAdvance: {GetFieldValue(lane, "IsReadyForAdvance")}");
            sb.AppendLine($"    Generation: {GetFieldValue(lane, "Generation")}");
            sb.AppendLine($"    Runner: {FormatObject(GetFieldValue(lane, "Runner"))}");
        }

        sb.AppendLine();
    }

    private static void AppendObjectSummary(StringBuilder sb, string label, object obj)
    {
        sb.AppendLine($"[{label}]");

        if (obj == null)
        {
            sb.AppendLine("  null");
            return;
        }

        sb.AppendLine($"  Type: {obj.GetType().Name}");
        sb.AppendLine($"  ToString: {obj}");
    }

    private static T FindFirstObjectByTypeSafe<T>() where T : UnityEngine.Object
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindFirstObjectByType<T>();
#else
        return UnityEngine.Object.FindObjectOfType<T>();
#endif
    }

    private static object GetFieldValue(object target, string fieldName)
    {
        if (target == null || string.IsNullOrEmpty(fieldName))
            return null;

        Type type = target.GetType();

        while (type != null)
        {
            FieldInfo field = type.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (field != null)
                return field.GetValue(target);

            type = type.BaseType;
        }

        return null;
    }

    private static object GetFieldValueByType(Type type, object target, string fieldName)
    {
        if (type == null || target == null || string.IsNullOrEmpty(fieldName))
            return null;

        FieldInfo field = type.GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        return field != null ? field.GetValue(target) : null;
    }

    private static object GetPropertyValue(object target, string propertyName)
    {
        if (target == null || string.IsNullOrEmpty(propertyName))
            return null;

        Type type = target.GetType();

        while (type != null)
        {
            PropertyInfo property = type.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (property != null)
            {
                try
                {
                    return property.GetValue(target, null);
                }
                catch
                {
                    return null;
                }
            }

            type = type.BaseType;
        }

        return null;
    }

    private static string InvokeStringMethod(object target, string methodName)
    {
        if (target == null || string.IsNullOrEmpty(methodName))
            return "null";

        Type type = target.GetType();

        while (type != null)
        {
            MethodInfo method = type.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);

            if (method != null)
            {
                try
                {
                    object result = method.Invoke(target, null);
                    return result != null ? result.ToString() : "null";
                }
                catch (Exception e)
                {
                    return $"Invoke failed: {e.GetType().Name}";
                }
            }

            type = type.BaseType;
        }

        return "method not found";
    }

    private static int TryGetCollectionCount(object obj)
    {
        ICollection collection = obj as ICollection;
        if (collection != null)
            return collection.Count;

        IEnumerable enumerable = obj as IEnumerable;
        if (enumerable == null)
            return -1;

        int count = 0;
        foreach (object ignored in enumerable)
            count++;

        return count;
    }

    private static string FormatNull(object value)
    {
        return value == null ? "null" : value.ToString();
    }

    private static string FormatObject(object obj)
    {
        if (obj == null)
            return "null";

        UnityEngine.Object unityObject = obj as UnityEngine.Object;
        if (unityObject != null)
            return $"{unityObject.name} ({obj.GetType().Name})";

        return $"{obj} ({obj.GetType().Name})";
    }

    private static string FormatCancellationSource(object obj)
    {
        CancellationTokenSource cts = obj as CancellationTokenSource;
        if (cts == null)
            return obj == null ? "null" : FormatObject(obj);

        bool canceled = false;

        try
        {
            canceled = cts.IsCancellationRequested;
        }
        catch
        {
            return "disposed or invalid";
        }

        return $"CancellationTokenSource(canceled={canceled})";
    }

    private static string FormatDelegate(object obj)
    {
        Delegate del = obj as Delegate;
        if (del == null)
            return obj == null ? "null" : FormatObject(obj);

        return $"{del.Method.DeclaringType}.{del.Method.Name}";
    }
}