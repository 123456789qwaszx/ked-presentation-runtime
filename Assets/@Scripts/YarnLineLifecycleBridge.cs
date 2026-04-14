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
/// - ActionMarkupHandler: typewriter-related callbacks
/// - DialoguePresenterBase: captures the current line's TextID (line ID)
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
    public event Action<YarnLineMeta> LinePrepared;
    public event Action<YarnLineMeta> LineStart;
    public event Action<YarnLineMeta> LineFinishDisplaying;
    public event Action<YarnLineMeta> LineWillDismiss;

    // ---- Runtime State ----
    private int _lineSerial = 0;
    private MarkupParseResult _currentLine;
    private TMP_Text _currentText;
    private string _currentNodeName = "";
    private string _currentLineId = "";
    
    private string _currentCharacterKey = "";
    public string CurrentCharacterKey => _currentCharacterKey;
    private void OnCharacterKeyReceived(string characterKey) => _currentCharacterKey = characterKey ?? string.Empty;
    

    public int CurrentLineSerial => _lineSerial;
    public bool IsLinePrepared { get; private set; }
    public bool IsLineFullyShown { get; private set; }
    public bool IsLineDismissing { get; private set; }
    public YarnLineMeta CurrentMeta { get; private set; }

    private bool _initialized;
    
    #region Lifecycle & Bindings

    public void Initialize(DialogueRunner dialogueRunner)
    {
        _dialogueRunner = dialogueRunner;
        if (_initialized) return;
        _initialized = true;
        
        _lineIdPresenter = GetComponent<YarnLineIdPresenter>();
        _lineIdPresenter.OnLineIdReceived -= OnLineIdReceived;
        _lineIdPresenter.OnLineIdReceived += OnLineIdReceived;
        
        _lineIdPresenter.OnCharacterKeyReceived -= OnCharacterKeyReceived;
        _lineIdPresenter.OnCharacterKeyReceived += OnCharacterKeyReceived;
        
        if (autoRegisterPresenter && dialogueRunner != null)
        {
            List<DialoguePresenterBase> presenters = new (dialogueRunner.DialoguePresenters);
            presenters.Remove(_lineIdPresenter);
            presenters.Insert(0, _lineIdPresenter);
            dialogueRunner.DialoguePresenters = presenters;
        }
        

        RegisterToYarn();
    }

    private void RegisterToYarn()
    {
        _dialogueRunner.onNodeStart?.AddListener(OnNodeStart);
        _dialogueRunner.onNodeComplete?.AddListener(OnNodeComplete);
    }

    private void OnDisable()
    {
        if (_dialogueRunner == null) return;

        _dialogueRunner.onNodeStart?.RemoveListener(OnNodeStart);
        _dialogueRunner.onNodeComplete?.RemoveListener(OnNodeComplete);
    }
    
    private void OnDestroy()
    {
        if (_lineIdPresenter != null)
            _lineIdPresenter.OnLineIdReceived -= OnLineIdReceived;

        if (_dialogueRunner != null && _lineIdPresenter != null)
        {
            List<DialoguePresenterBase> presenters = new (_dialogueRunner.DialoguePresenters);
            presenters.Remove(_lineIdPresenter);
            _dialogueRunner.DialoguePresenters = presenters;
        }
    }
    
    private void OnNodeStart(string nodeName)
    {
        _currentNodeName = nodeName;
        OnNodeStarted?.Invoke(nodeName);
    }
    private void OnNodeComplete(string completedNodeName)
    {
        NodeCompleted?.Invoke(completedNodeName);
        _currentNodeName = "";
    }
    private void OnLineIdReceived(string lineId) => _currentLineId = lineId
    ;
    #endregion
    
    // =========================================================
    // ActionMarkupHandler overrides
    // =========================================================

    public override void OnPrepareForLine(MarkupParseResult line, TMP_Text text)
    {
        _lineSerial++;
        _currentLine = line;
        _currentText = text;

        IsLinePrepared = true;
        IsLineFullyShown = false;
        IsLineDismissing = false;

        CurrentMeta = BuildMeta(line, text);
        //Debug.Log($"[YarnLifecycle] Prepare serial={_lineSerial} lineId='{CurrentMeta.lineId}' text='{CurrentMeta.rawText}'");
        
        LinePrepared?.Invoke(CurrentMeta);
    }

    public override void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text)
    {
        LineStart?.Invoke(CurrentMeta);
    }
    
    // Per-character hook
    public override YarnTask OnCharacterWillAppear(
        int currentCharacterIndex, // Index of the next character to appear
        MarkupParseResult line,    // Parsed markup result for the current line
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
    private string GetRawText(MarkupParseResult line, TMP_Text text)
    {
        string raw = (text != null)
            ? text.text 
            : string.Empty;
            
        if (string.IsNullOrEmpty(raw))
            raw = line.Text;

        return raw;
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
}
