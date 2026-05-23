using System;
using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

[Serializable]
public struct YarnLineMeta
{
    public string nodeName;
    public string lineId;
    public string charName;
    public string rawText;

    public YarnLineMeta(string nodeName, string lineId, string rawText, string charName)
    {
        this.nodeName = nodeName;
        this.lineId = lineId;
        this.rawText = rawText;
        this.charName = charName;
    }
}

public sealed class YarnLineLifecycleBridge : ActionMarkupHandler
{
    public event Action<YarnLineMeta> LineEntered;
    public event Action<YarnLineMeta> LinePrepared;
    public event Action<YarnLineMeta> LineDisplayBegin;
    public event Action<YarnLineMeta> LineFinishDisplaying;
    public event Action<YarnLineMeta> LineWillDismiss;
    
    
    private DialogueRunner _dialogueRunner; 
    private CustomLinePresenter _customLinePresenter;

    private YarnLineMeta _currentMeta;
    
    public string CurrentNodeName { get; set; }
    public string CurrentLineId { get; set; }
    public string CurrentCharacterKey { get; set; }
    
    public void Initialize(DialogueRunner dialogueRunner, CustomLinePresenter customLinePresenter)
    {
        _dialogueRunner = dialogueRunner;
        _customLinePresenter = customLinePresenter;

        _dialogueRunner.onNodeStart?.RemoveListener(OnNodeStart);
        _dialogueRunner.onNodeStart?.AddListener(OnNodeStart);
        
        _customLinePresenter.LineEntered -= OnLineEntered;
        _customLinePresenter.LineEntered += OnLineEntered;
    }
    
    private void OnNodeStart(string nodeName) => CurrentNodeName = nodeName;
    
    private void OnLineEntered(LocalizedLine line)
    {
        CurrentLineId = line.TextID;
        CurrentCharacterKey = line.CharacterName ?? string.Empty;
        
        _currentMeta = BuildMeta(line);
        LineEntered?.Invoke(_currentMeta);
    }
    
    public override void OnPrepareForLine(MarkupParseResult line, TMP_Text text) => LinePrepared?.Invoke(_currentMeta);
    
    public override void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text) => LineDisplayBegin?.Invoke(_currentMeta);
    
    public override YarnTask OnCharacterWillAppear(int currentCharacterIndex, MarkupParseResult line, CancellationToken cancellationToken) { return YarnTask.CompletedTask; }
    
    public override void OnLineDisplayComplete() => LineFinishDisplaying?.Invoke(_currentMeta);
    
    public override void OnLineWillDismiss() => LineWillDismiss?.Invoke(_currentMeta);
    
    
    private YarnLineMeta BuildMeta(LocalizedLine line)
    {
        string charName = "";
        string rawText = "";
        
        if (!string.IsNullOrEmpty(line.TextWithoutCharacterName.Text))
            charName = line.TextWithoutCharacterName.Text;

        rawText = line.TextWithoutCharacterName.Text;
        
        return new YarnLineMeta(
            CurrentNodeName,
            CurrentLineId,
            charName,
            rawText
        );
    }
    
    private void OnDestroy()
    {
        if (_dialogueRunner != null)
            _dialogueRunner.onNodeStart?.RemoveListener(OnNodeStart);
        
        if (_customLinePresenter != null)
            _customLinePresenter.LineEntered -= OnLineEntered;
    }
}