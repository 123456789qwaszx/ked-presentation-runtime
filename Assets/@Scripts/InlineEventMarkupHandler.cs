using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Yarn.Markup;
using Yarn.Unity;

// Inline Action Markup Handler:
// - Handles only point tags: [pause], [signal], [move]
// - Styling (color/size/etc) must be handled by ReplacementMarkupHandler on LineProvider
public sealed class InlineEventMarkupHandler : ActionMarkupHandler
{
    private IInlineSignalHost _inlineSignalHost;
    public void Initialize(IInlineSignalHost inlineSignalHost)
    {
        _inlineSignalHost = inlineSignalHost;
    }
    
    public interface IInlineSignalHost
    {
        void RaiseSignal(string key);
    }

    [Serializable]
    public sealed class StringEvent : UnityEvent<string>
    {
    }

    private enum InlineActionType : byte
    {
        Pause = 0,
        Signal = 1,
        Move = 2,
    }

    private struct InlineAction
    {
        public InlineActionType type;
        public int pauseMs;
        public string signalKey;
        public string moveName;
    }

    private struct ActionBucket
    {
        public bool hasSingle;
        public InlineAction single;
        public List<InlineAction> many;

        public void Add(in InlineAction action)
        {
            if (!hasSingle && many == null)
            {
                single = action;
                hasSingle = true;
                return;
            }

            if (many == null)
            {
                many = new List<InlineAction>(capacity: 2);
                if (hasSingle)
                {
                    many.Add(single);
                    hasSingle = false;
                    single = default;
                }
            }

            many.Add(action);
        }
    }

    [Header("Callbacks")] public StringEvent onMoveRequested = new();

    // ---- Action table (index -> actions) ----
    private readonly Dictionary<int, ActionBucket> _actionsByIndex = new(64);

    private string _plainText = "";
    private int _lastProcessedCharIndex = 0;
    
    private bool _ignorePause;

    public void SetPauseIgnored(bool ignored)
    {
        _ignorePause = ignored;
    }
    
    /// <summary>
    /// Force-fires any pending signals that haven't been triggered yet (e.g. on HurryUp/skip)
    /// pause/move are skipped
    /// </summary>
    public void FlushPendingSignals()
    {
        if (string.IsNullOrEmpty(_plainText)) return;

        int textLength = _plainText.Length;

        // Only iterate indices that haven't been processed yet
        foreach (var kvp in _actionsByIndex)
        {
            int index = kvp.Key;

            // Skip already-processed positions
            if (index <= _lastProcessedCharIndex)
                continue;

            ActionBucket bucket = kvp.Value;

            if (bucket.hasSingle)
            {
                if (bucket.single.type == InlineActionType.Signal)
                {
                    _inlineSignalHost?.RaiseSignal(bucket.single.signalKey);
                }
            }

            if (bucket.many != null)
            {
                foreach (InlineAction action in bucket.many)
                {
                    if (action.type == InlineActionType.Signal)
                    {
                        _inlineSignalHost?.RaiseSignal(action.signalKey);
                    }
                }
            }
        }

        _lastProcessedCharIndex = textLength;
    }

    // ------------------------------
    // IActionMarkupHandler
    // ------------------------------
    public override void OnPrepareForLine(MarkupParseResult line, TMP_Text text)
    {
        _actionsByIndex.Clear();
        _lastProcessedCharIndex = 0;

        _plainText = line.Text;

        // Point tags only. Range tags (attr.Length > 0) are for styling and are ignored here.
        foreach (MarkupAttribute attr in line.Attributes)
        {
            if (attr.Length > 0)
                continue;

            switch (attr.Name)
            {
                case "pau": RegisterPause(attr); break;
                case "signal": RegisterSignal(attr); break;
                case "move": RegisterMove(attr); break;
            }
        }
    }

    public override void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text)
    {
    }

    public override async YarnTask OnCharacterWillAppear(int currentCharacterIndex, MarkupParseResult line,
        CancellationToken ct)
    {
        _lastProcessedCharIndex = currentCharacterIndex;

        if (_actionsByIndex.TryGetValue(currentCharacterIndex, out ActionBucket bucket))
        {
            if (bucket.hasSingle)
            {
                await RunAction(bucket.single, ct);
                return;
            }

            if (bucket.many != null)
            {
                for (int i = 0; i < bucket.many.Count; i++)
                {
                    await RunAction(bucket.many[i], ct);
                }
            }
        }
    }

    public override void OnLineDisplayComplete()
    {
    }

    public override void OnLineWillDismiss()
    {
    }

    // ------------------------------
    // Action execution
    // ------------------------------
    private async YarnTask RunAction(InlineAction action, CancellationToken ct)
    {
        switch (action.type)
        {
            case InlineActionType.Pause:
                if (_ignorePause)
                    return;
                
                if (action.pauseMs > 0)
                    await YarnTask.Delay(action.pauseMs, ct);
                return;

            case InlineActionType.Move:
                if (!string.IsNullOrWhiteSpace(action.moveName))
                    onMoveRequested?.Invoke(action.moveName);
                return;

            case InlineActionType.Signal:
                if (!string.IsNullOrWhiteSpace(action.signalKey))
                    _inlineSignalHost?.RaiseSignal(action.signalKey);
                return;
        }
    }

    // ------------------------------
    // Action Registration
    // ------------------------------

    private void RegisterPause(MarkupAttribute attr)
    {
        // [pause t=0.25/]
        float seconds = 0.2f;
        if (TryGetFloatSmart(attr, "t", out float pauseTime))
            seconds = Mathf.Max(0f, pauseTime);

        int ms = Mathf.RoundToInt(seconds * 1000f);
        int idx = NormalizeIndexFast(attr.Position);

        InlineAction inlineAction = new InlineAction
        {
            type = InlineActionType.Pause,
            pauseMs = ms,
        };
        AddAction(idx, in inlineAction);
    }

    private void RegisterSignal(MarkupAttribute attr)
    {
        // [signal key="beat"/]
        if (!TryGetString(attr, "key", out string key) || string.IsNullOrWhiteSpace(key))
            return;

        int idx = NormalizeIndexFast(attr.Position);

        InlineAction inlineAction = new InlineAction
        {
            type = InlineActionType.Signal,
            signalKey = key,
        };
        AddAction(idx, in inlineAction);
    }

    private void RegisterMove(MarkupAttribute attr)
    {
        // [move name="far"/]
        if (!TryGetString(attr, "name", out string moveName) || string.IsNullOrWhiteSpace(moveName))
            return;

        int idx = NormalizeIndexFast(attr.Position);

        InlineAction inlineAction = new InlineAction
        {
            type = InlineActionType.Move,
            moveName = moveName,
        };
        AddAction(idx, in inlineAction);
    }

    private void AddAction(int index, in InlineAction action)
    {
        _actionsByIndex.TryGetValue(index, out ActionBucket bucket);
        bucket.Add(action);
        _actionsByIndex[index] = bucket;
    }

    private int NormalizeIndexFast(int index)
    {
        int len = _plainText?.Length ?? 0;

        if (len <= 0)
        {
            // 실패원인: 빈라인에 마크업만 존재
            Debug.LogError($"[InlineEvent] Empty text line. pos={index}");
            return index;
        }

        if (index < 0)
        {
            // 실패원인: 가장 앞자리 마크업
            Debug.LogError($"[InlineEvent] Negative markup position. pos={index}, len={len}");
            return index;
        }

        if (index >= len)
        {
            int clamped = len - 1;
            return clamped; // 끝/초과 시 마지막 문자에 붙도록 보정
        }

        return index;
    }

    private bool TryGetString(MarkupAttribute attr, string key, out string value)
    {
        if (attr.TryGetProperty(key, out string v))
        {
            value = v;
            return true;
        }

        // 실패 원인: (1) 타입이 string이 아님 (2) 키가 없음
        if (attr.TryGetProperty(key, out MarkupValue mv))
            Debug.LogError($"[InlineEvent] [{attr.Name}] '{key}' must be string (got {mv.Type}). pos={attr.Position}");
        else
            Debug.LogError($"[InlineEvent] [{attr.Name}] missing '{key}'. pos={attr.Position}");

        value = null;
        return false;
    }

    private bool TryGetFloatSmart(MarkupAttribute attr, string key, out float value)
    {
        value = 0f;

        if (!attr.TryGetProperty(key, out MarkupValue markupValue))
        {
            // 실패원인: 태그에 존재하지 않는 프로퍼티
            Debug.LogError($"[InlineEvent] reason=missing_prop tag=[{attr.Name}] prop='{key}' pos={attr.Position}");
            return false;
        }

        switch (markupValue.Type)
        {
            case MarkupValueType.Float:
                value = markupValue.FloatValue;
                return true;

            case MarkupValueType.Integer:
                value = markupValue.IntegerValue;
                return true;

            case MarkupValueType.String:
            {
                string s = markupValue.StringValue;
                if (!float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    // 실패원인: string이지만 파싱실패. e.g) "0.2s","abc", "" 
                    Debug.LogError(
                        $"[InlineEvent] reason=parse_failed tag=[{attr.Name}] prop='{key}' raw='{s}' pos={attr.Position}");
                    return false;
                }

                return true;
            }

            default: // 실패원인: 지원하지 않는 타입, bool
                Debug.LogError(
                    $"[InlineEvent] reason=unsupported_type tag=[{attr.Name}] prop='{key}' type={markupValue.Type} pos={attr.Position}");
                return false;
        }
    }
}