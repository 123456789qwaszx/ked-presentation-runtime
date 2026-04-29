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
// - Handles only point tags: [pause], [signal], [move], [sfx]
// - Styling (color/size/etc) must be handled by ReplacementMarkupHandler on LineProvider
public sealed class InlineEventMarkupHandler : ActionMarkupHandler
{
    private YarnLineLifecycleBridge _lineLifecycleBridge;
    
    private IInlineSignalHost _inlineSignalHost;
    private IInlineAudioHost _inlineAudioHost;
    private IInlineEmojiHost _inlineEmojiHost;

    public void Initialize(YarnLineLifecycleBridge lineLifecycleBridge, 
        IInlineSignalHost inlineSignalHost, 
        IInlineAudioHost inlineAudioHost, 
        IInlineEmojiHost inlineEmojiHost)
    {
        _lineLifecycleBridge = lineLifecycleBridge;
        _inlineSignalHost = inlineSignalHost;
        _inlineAudioHost = inlineAudioHost;
        _inlineEmojiHost = inlineEmojiHost;
    }

    public interface IInlineSignalHost
    {
        void RaiseSignal(string key);
    }

    public interface IInlineAudioHost
    {
        void PlaySfxCue(string cue, float gain = 1f);
    }
    
    public interface IInlineEmojiHost
    {
        void PlayEmojiCue(string characterKey, string cue);
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
        Sfx = 3,
        Emoji = 4
    }

    private struct InlineAction
    {
        public InlineActionType type;
        public int pauseMs;
        public string signalKey;
        public string moveName;

        public string sfxCue;
        public float sfxGain;

        public string emojiCue;
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

    [Header("Callbacks")]
    public StringEvent onMoveRequested = new();

    private readonly Dictionary<int, ActionBucket> _actionsByIndex = new(64);

    private string _plainText = "";
    private int _lastProcessedCharIndex = 0;

    private bool _ignorePause;
    private bool _suppressSignals;
    private bool _suppressMoves;
    private bool _suppressSfx;
    private bool _suppressEmoji;
    
    public void SetPauseIgnored(bool ignored)
    {
        _ignorePause = ignored;
    }

    public void SetReplaySuppressed(bool suppressSignals, bool suppressMoves, bool suppressSfx = true, bool suppressEmoji = false)
    {
        _suppressSignals = suppressSignals;
        _suppressMoves = suppressMoves;
        _suppressSfx = suppressSfx;
        _suppressEmoji = suppressEmoji;
    }

    /// <summary>
    /// Force-fires any pending signals that haven't been triggered yet (e.g. on HurryUp/skip).
    /// pause/move/sfx are skipped.
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
                    _inlineSignalHost?.RaiseSignal(bucket.single.signalKey);
            }

            if (bucket.many != null)
            {
                foreach (InlineAction action in bucket.many)
                {
                    if (action.type == InlineActionType.Signal)
                        _inlineSignalHost?.RaiseSignal(action.signalKey);
                }
            }
        }

        _lastProcessedCharIndex = textLength;
    }

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
                case "sfx": RegisterSfx(attr); break;
                case "emoji": RegisterEmoji(attr); break;
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
                    await RunAction(bucket.many[i], ct);
            }
        }
    }

    public override void OnLineDisplayComplete()
    {
    }

    public override void OnLineWillDismiss()
    {
    }

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
                if (_suppressMoves)
                    return;

                if (!string.IsNullOrWhiteSpace(action.moveName))
                    onMoveRequested?.Invoke(action.moveName);
                return;

            case InlineActionType.Signal:
                if (_suppressSignals)
                    return;

                if (!string.IsNullOrWhiteSpace(action.signalKey))
                    _inlineSignalHost?.RaiseSignal(action.signalKey);
                return;

            case InlineActionType.Sfx:
                if (_suppressSfx)
                    return;

                if (!string.IsNullOrWhiteSpace(action.sfxCue))
                    _inlineAudioHost?.PlaySfxCue(action.sfxCue, action.sfxGain);
                return;
            
            case InlineActionType.Emoji:
                if (_suppressEmoji)
                    return;

                if (!string.IsNullOrWhiteSpace(action.emojiCue))
                {
                    string characterKey = _lineLifecycleBridge != null
                        ? _lineLifecycleBridge.CurrentCharacterKey
                        : string.Empty;

                    _inlineEmojiHost?.PlayEmojiCue(characterKey, action.emojiCue);
                }
                return;
        }
    }

    private void RegisterPause(MarkupAttribute attr)
    {
        // [pau t=0.25/] [pau =0.25/]
        float seconds = 0.2f;

        if (TryGetFloatSmart(attr, "t", out float pauseTime) ||
            TryGetFloatSmart(attr, "pau", out pauseTime)) // shorthand fallback
        {
            seconds = Mathf.Max(0f, pauseTime);
        }

        int ms = Mathf.RoundToInt(seconds * 1000f);
        int idx = NormalizeIndexFast(attr.Position);

        AddAction(idx, new InlineAction
        {
            type = InlineActionType.Pause,
            pauseMs = ms,
        });
    }

    private void RegisterSignal(MarkupAttribute attr)
    {
        // [signal key="beat"/] [signal =beat/]
        if (!TryGetString(attr, "key", out string key) &&
            !TryGetString(attr, "signal", out key)) // shorthand fallback
            return;

        if (string.IsNullOrWhiteSpace(key)) return;

        int idx = NormalizeIndexFast(attr.Position);

        AddAction(idx, new InlineAction
        {
            type = InlineActionType.Signal,
            signalKey = key,
        });
    }

    private void RegisterMove(MarkupAttribute attr)
    {
        // [move name="far"/] [move =far/]
        if (!TryGetString(attr, "name", out string moveName) &&
            !TryGetString(attr, "move", out moveName)) // shorthand fallback
            return;

        if (string.IsNullOrWhiteSpace(moveName)) return;

        int idx = NormalizeIndexFast(attr.Position);

        AddAction(idx, new InlineAction
        {
            type = InlineActionType.Move,
            moveName = moveName,
        });
    }

    private void RegisterSfx(MarkupAttribute attr)
    {
        // [sfx =c3/] [sfx cue=c3] [sfx cue=c3 gain=0.1/]  [sfx =c3 gain=0.1/]
        if (!TryGetString(attr, "cue", out string cue) &&
            !TryGetString(attr, "sfx", out cue)) // shorthand fallback
            return;

        if (string.IsNullOrWhiteSpace(cue)) return;

        float gain = 1f;
        if (attr.TryGetProperty("gain", out MarkupValue _))
            TryGetFloatSmart(attr, "gain", out gain);

        int idx = NormalizeIndexFast(attr.Position);

        AddAction(idx, new InlineAction
        {
            type = InlineActionType.Sfx,
            sfxCue = cue,
            sfxGain = Mathf.Max(0f, gain),
        });
    }
    
    private void RegisterEmoji(MarkupAttribute attr)
    {
        // [emoji =heart/]
        // [emoji key=heart/]

        if (!TryGetString(attr, "key", out string cue) &&
            !TryGetString(attr, "emoji", out cue)) // shorthand fallback
            return;

        if (string.IsNullOrWhiteSpace(cue))
            return;

        int idx = NormalizeIndexFast(attr.Position);

        AddAction(idx, new InlineAction
        {
            type = InlineActionType.Emoji,
            emojiCue = cue,
        });
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
            return len - 1; // 끝, 초과 시 마지막 문자에 붙도록 보정

        return index;
    }

    private bool TryGetString(MarkupAttribute attr, string key, out string value)
    {
        if (attr.TryGetProperty(key, out string v))
        {
            value = v;
            return true;
        }

        // 실패 원인: (1) 타입이 string이 아님
        if (attr.TryGetProperty(key, out MarkupValue mv))
            Debug.LogError($"[InlineEvent] [{attr.Name}] '{key}' must be string (got {mv.Type}). pos={attr.Position}");
        // else(2) 키가 없음
        //     Debug.LogError($"[InlineEvent] [{attr.Name}] missing '{key}'. pos={attr.Position}");

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

            default:
                Debug.LogError(
                    $"[InlineEvent] reason=unsupported_type tag=[{attr.Name}] prop='{key}' type={markupValue.Type} pos={attr.Position}");
                return false;
        }
    }
}