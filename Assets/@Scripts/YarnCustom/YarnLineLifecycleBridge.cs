using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

[Serializable]
public struct YarnLineMeta
{
    public int lineSerial;
    public int frame;
    public string rawText;
    public string nodeName;
    public string lineId;

    public YarnLineMeta(int lineSerial, int frame, string rawText, string nodeName, string lineId)
    {
        this.lineSerial = lineSerial;
        this.frame = frame;
        this.rawText = rawText;
        this.nodeName = nodeName;
        this.lineId = lineId;
    }
}

/// <summary>
/// Bridge that exposes Yarn line lifecycle events.
///
/// Important lifecycle split:
/// - LineEntered: logical Yarn line reached. This is emitted by YarnLineIdPresenter before visual presenters run.
/// - LinePrepared: text target and TMP content prepared.
/// - LineDisplayBegin: typewriter/display begins.
/// - LineFinishDisplaying: line fully displayed.
/// - LineWillDismiss: line is about to be dismissed.
///
/// Rollback should prefer LineEntered, not LineDisplayBegin.
/// </summary>
[RequireComponent(typeof(YarnLineIdPresenter))]
public sealed class YarnLineLifecycleBridge : ActionMarkupHandler
{
    private DialogueRunner _dialogueRunner;
    private YarnLineIdPresenter _lineIdPresenter;

    [Header("Setup")]
    [Tooltip("DialogueRunner의 Dialogue Presenters 리스트에 LineIdPresenter가 자동으로 가장 앞에 추가됨")]
    [SerializeField] private bool autoRegisterPresenter = true;

    public event Action<string> OnNodeStarted;
    public event Action<string> NodeCompleted;

    public event Action<YarnLineMeta> LineEntered;
    public event Action<YarnLineMeta> LinePrepared;

    /// <summary>
    /// Legacy name. 실제 의미는 LineDisplayBegin.
    /// 기존 RollbackController 마이그레이션 전까지 호환용으로 유지.
    /// 새 코드는 LineEntered 또는 LineDisplayBegin을 직접 구독하는 것을 권장.
    /// </summary>

    public event Action<YarnLineMeta> LineDisplayBegin;
    public event Action<YarnLineMeta> LineFinishDisplaying;
    public event Action<YarnLineMeta> LineWillDismiss;

    // ---- Runtime State ----
    private int _lineSerial;
    private MarkupParseResult _currentLine;
    private TMP_Text _currentText;
    private LocalizedLine _currentLocalizedLine;

    private string _currentNodeName = "";
    private string _currentLineId = "";
    private string _currentCharacterKey = "";

    public string CurrentCharacterKey => _currentCharacterKey;
    public int CurrentLineSerial => _lineSerial;

    public bool IsLineEntered { get; private set; }
    public bool IsLinePrepared { get; private set; }
    public bool IsLineFullyShown { get; private set; }
    public bool IsLineDismissing { get; private set; }

    public YarnLineMeta CurrentMeta { get; private set; }

    private bool _initialized;

    #region Lifecycle & Bindings

    public void Initialize(DialogueRunner dialogueRunner)
    {
        _dialogueRunner = dialogueRunner;

        if (_initialized)
            return;

        _initialized = true;

        _lineIdPresenter = GetComponent<YarnLineIdPresenter>();

        _lineIdPresenter.OnLineIdReceived -= OnLineIdReceived;
        _lineIdPresenter.OnLineIdReceived += OnLineIdReceived;

        _lineIdPresenter.OnCharacterKeyReceived -= OnCharacterKeyReceived;
        _lineIdPresenter.OnCharacterKeyReceived += OnCharacterKeyReceived;

        _lineIdPresenter.LineEntered -= OnLineEntered;
        _lineIdPresenter.LineEntered += OnLineEntered;

        if (autoRegisterPresenter && dialogueRunner != null)
        {
            List<DialoguePresenterBase> presenters = new(dialogueRunner.DialoguePresenters);

            // Must be first.
            // Rollback uses YarnLineIdPresenter as the earliest logical line-enter hook,
            // before visual presenters, dialogue box transitions, typewriter, or input wait.
            presenters.Remove(_lineIdPresenter);
            presenters.Insert(0, _lineIdPresenter);

            dialogueRunner.DialoguePresenters = presenters;
        }

        RegisterToYarn();
    }

    private void RegisterToYarn()
    {
        if (_dialogueRunner == null)
            return;

        _dialogueRunner.onNodeStart?.RemoveListener(OnNodeStart);
        _dialogueRunner.onNodeStart?.AddListener(OnNodeStart);

        _dialogueRunner.onNodeComplete?.RemoveListener(OnNodeComplete);
        _dialogueRunner.onNodeComplete?.AddListener(OnNodeComplete);
    }

    private void OnDisable()
    {
        UnregisterFromYarn();
    }

    private void OnDestroy()
    {
        if (_lineIdPresenter != null)
        {
            _lineIdPresenter.OnLineIdReceived -= OnLineIdReceived;
            _lineIdPresenter.OnCharacterKeyReceived -= OnCharacterKeyReceived;
            _lineIdPresenter.LineEntered -= OnLineEntered;
        }

        UnregisterFromYarn();

        if (_dialogueRunner != null && _lineIdPresenter != null)
        {
            List<DialoguePresenterBase> presenters = new(_dialogueRunner.DialoguePresenters);
            presenters.Remove(_lineIdPresenter);
            _dialogueRunner.DialoguePresenters = presenters;
        }
    }

    private void UnregisterFromYarn()
    {
        if (_dialogueRunner == null)
            return;

        _dialogueRunner.onNodeStart?.RemoveListener(OnNodeStart);
        _dialogueRunner.onNodeComplete?.RemoveListener(OnNodeComplete);
    }

    private void OnNodeStart(string nodeName)
    {
        _currentNodeName = nodeName ?? string.Empty;
        OnNodeStarted?.Invoke(_currentNodeName);
    }

    private void OnNodeComplete(string completedNodeName)
    {
        NodeCompleted?.Invoke(completedNodeName);
        _currentNodeName = "";
    }

    private void OnLineIdReceived(string lineId)
    {
        _currentLineId = lineId ?? string.Empty;
    }

    private void OnCharacterKeyReceived(string characterKey)
    {
        _currentCharacterKey = characterKey ?? string.Empty;
    }

    private void OnLineEntered(LocalizedLine line)
    {
        _lineSerial++;

        _currentLocalizedLine = line;
        _currentLine = default;
        _currentText = null;

        IsLineEntered = true;
        IsLinePrepared = false;
        IsLineFullyShown = false;
        IsLineDismissing = false;

        CurrentMeta = BuildMeta(line);

        LineEntered?.Invoke(CurrentMeta);
    }

    #endregion

    // =========================================================
    // ActionMarkupHandler overrides
    // =========================================================

    public override void OnPrepareForLine(MarkupParseResult line, TMP_Text text)
    {
        _currentLine = line;
        _currentText = text;

        IsLinePrepared = true;
        IsLineFullyShown = false;
        IsLineDismissing = false;

        CurrentMeta = BuildMeta(line, text);

        LinePrepared?.Invoke(CurrentMeta);
    }

    public override void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text)
    {
        _currentLine = line;
        _currentText = text;

        CurrentMeta = BuildMeta(line, text);

        LineDisplayBegin?.Invoke(CurrentMeta);
    }

    public override YarnTask OnCharacterWillAppear(
        int currentCharacterIndex,
        MarkupParseResult line,
        CancellationToken cancellationToken)
    {
        return YarnTask.CompletedTask;
    }

    public override void OnLineDisplayComplete()
    {
        IsLineFullyShown = true;
        CurrentMeta = BuildMeta(_currentLine, _currentText);

        LineFinishDisplaying?.Invoke(CurrentMeta);
    }

    public override void OnLineWillDismiss()
    {
        IsLineDismissing = true;
        CurrentMeta = BuildMeta(_currentLine, _currentText);

        LineWillDismiss?.Invoke(CurrentMeta);
    }

    // ---- Helpers ----

    private YarnLineMeta BuildMeta(LocalizedLine line)
    {
        return new YarnLineMeta(
            _lineSerial,
            Time.frameCount,
            GetRawText(line),
            _currentNodeName,
            _currentLineId
        );
    }

    private YarnLineMeta BuildMeta(MarkupParseResult line, TMP_Text text)
    {
        return new YarnLineMeta(
            _lineSerial,
            Time.frameCount,
            GetRawText(line, text),
            _currentNodeName,
            _currentLineId
        );
    }

    private string GetRawText(LocalizedLine line)
    {
        if (line == null)
            return string.Empty;

        if (string.IsNullOrEmpty(line.TextWithoutCharacterName.Text) == false)
            return line.TextWithoutCharacterName.Text;

        if (string.IsNullOrEmpty(line.Text.Text) == false)
            return line.Text.Text;

        return string.Empty;
    }

    private string GetRawText(MarkupParseResult line, TMP_Text text)
    {
        string raw = text != null
            ? text.text
            : string.Empty;

        if (string.IsNullOrEmpty(raw) == false)
            return raw;

        if (line.Text != null)
            return line.Text;

        return GetRawText(_currentLocalizedLine);
    }
}