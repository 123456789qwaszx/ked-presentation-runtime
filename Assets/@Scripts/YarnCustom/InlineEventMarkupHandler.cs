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
// - Handles only point tags: [pause], [signal], [move], [sfx], [emoji], [advance]
// - Styling (color/size/etc) must be handled by ReplacementMarkupHandler on LineProvider
public sealed class InlineEventMarkupHandler : ActionMarkupHandler
{
    private IInlineSignalHost _inlineSignalHost;
    private IInlineAudioHost _inlineAudioHost;
    private IInlineEmojiHost _inlineEmojiHost;
    private IInlinePresentationHost _inlinePresentationHost;

    public void Initialize(
        IInlineSignalHost inlineSignalHost, 
        IInlineAudioHost inlineAudioHost, 
        IInlineEmojiHost inlineEmojiHost,
        IInlinePresentationHost inlinePresentationHost)
    {
        _inlineSignalHost = inlineSignalHost;
        _inlineAudioHost = inlineAudioHost;
        _inlineEmojiHost = inlineEmojiHost;
        _inlinePresentationHost = inlinePresentationHost;
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
        void PlayEmojiCue(string cue);
    }

    // 대사 진행 중 subPresentationLane을 추가로 진행시킨다.
    // 커맨드 "pres_advance"(= VNSideRunnerSyncHub.StepPresentationOnce)와 동일한 효과.
    // 구현체는 _sideRunnerSyncHub.StepPresentationOnce(steps)를 호출하면 된다.
    public interface IInlinePresentationHost
    {
        void AdvanceSubPresentation(int steps = 1);
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
        Emoji = 4,
        Advance = 5
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

        public int advanceSteps;
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
    private bool _suppressAdvance;
    
    public void SetPauseIgnored(bool ignored)
    {
        _ignorePause = ignored;
    }

    // suppressAdvance 기본값은 false다.
    // seek/rollback의 pass-through 라인은 타이프라이터 자체가 돌지 않아 인라인 advance가
    // 발화되지 않으므로(= OnCharacterWillAppear 미호출), 정방향/스킵에서는 항상 발화시켜
    // 서브 레인 동기를 유지하는 것이 안전하다. 굳이 억제가 필요한 호출부에서만 명시적으로 true.
    public void SetReplaySuppressed(
        bool suppressSignals,
        bool suppressMoves,
        bool suppressSfx = true,
        bool suppressEmoji = false,
        bool suppressAdvance = false)
    {
        _suppressSignals = suppressSignals;
        _suppressMoves = suppressMoves;
        _suppressSfx = suppressSfx;
        _suppressEmoji = suppressEmoji;
        _suppressAdvance = suppressAdvance;
    }

    /// <summary>
    /// Force-fires any pending point-actions that must still take effect on HurryUp/skip:
    /// signals(always) and sub-presentation advances(unless suppressed).
    /// pause/move/sfx/emoji are skipped.
    /// advance를 함께 flush하는 이유: 스킵 경로가 정상 재생과 동일한 서브 레인 위치로 수렴해야
    /// 하기 때문(라인 중간 [advance]를 건너뛰고 hurry-up하면 서브 레인이 한 칸 뒤처져 desync).
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
                FlushActionOnSkip(bucket.single);

            if (bucket.many != null)
            {
                foreach (InlineAction action in bucket.many)
                    FlushActionOnSkip(action);
            }
        }

        _lastProcessedCharIndex = textLength;
    }

    // hurry-up/skip 시 "반드시 일어나야 하는" point action만 즉시 발화.
    private void FlushActionOnSkip(in InlineAction action)
    {
        switch (action.type)
        {
            case InlineActionType.Signal:
                // 기존 동작 유지: signal은 억제 여부와 무관하게 flush.
                _inlineSignalHost?.RaiseSignal(action.signalKey);
                return;

            case InlineActionType.Advance:
                if (_suppressAdvance)
                    return;

                _inlinePresentationHost?.AdvanceSubPresentation(action.advanceSteps);
                return;
        }
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
                case "advance":
                case "pres_advance":
                    RegisterAdvance(attr);
                    break;
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
                    _inlineEmojiHost?.PlayEmojiCue(action.emojiCue);
                return;

            case InlineActionType.Advance:
                if (_suppressAdvance)
                    return;

                // 라인 커밋 이후(타이프라이터 단계)에만 호출되므로 서브 레인을 직접 진행시켜도 안전.
                // 커맨드 "pres_advance"와 동일 경로(StepPresentationOnce).
                _inlinePresentationHost?.AdvanceSubPresentation(action.advanceSteps);
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

    private void RegisterAdvance(MarkupAttribute attr)
    {
        // [advance/]            -> 1 step
        // [advance n=2/]        -> 2 steps
        // [advance =2/]         -> 2 steps (shorthand)
        // [pres_advance =2/]    -> 동일 (커맨드 vocabulary와 맞춤)
        int steps = 1;

        if (TryGetFloatSmart(attr, "n", out float n) ||
            TryGetFloatSmart(attr, "steps", out n) ||
            TryGetFloatSmart(attr, "advance", out n) ||       // [advance =2/] shorthand
            TryGetFloatSmart(attr, "pres_advance", out n))    // [pres_advance =2/] shorthand
        {
            steps = Mathf.Max(1, Mathf.RoundToInt(n));
        }

        int idx = NormalizeIndexFast(attr.Position);

        AddAction(idx, new InlineAction
        {
            type = InlineActionType.Advance,
            advanceSteps = steps,
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
            // 태그에 존재하지 않는 프로퍼티
            // fallback 탐색시작 :
            // e.g) [pau =0.85/] 처리 시 먼저 "t"를 찾고, 없으면 "pau"를 찾는다.
            // 만약 잘못된 값을 입력 시, Default값이 들어감 "[pau error= 0.85/] 이경우 default 0.2f"
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