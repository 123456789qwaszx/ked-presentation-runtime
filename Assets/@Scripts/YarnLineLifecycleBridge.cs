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

    public YarnLineMeta(string nodeName, string lineId, string charName, string rawText)
    {
        this.nodeName = nodeName;
        this.lineId = lineId;
        this.charName = charName;
        this.rawText = rawText;
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

    public string CurrentNodeName { get; private set; }
    public string CurrentLineId { get; private set; }
    public string CurrentCharacterKey { get; private set; }

    private VNTraceStream _trace;

    public void Initialize(
        DialogueRunner dialogueRunner,
        CustomLinePresenter customLinePresenter,
        VNTraceStream trace = null)
    {
        _dialogueRunner = dialogueRunner;
        _customLinePresenter = customLinePresenter;
        _trace = trace;

        if (_dialogueRunner != null)
        {
            _dialogueRunner.onNodeStart?.RemoveListener(OnNodeStart);
            _dialogueRunner.onNodeStart?.AddListener(OnNodeStart);
        }

        if (_customLinePresenter != null)
        {
            _customLinePresenter.LineEntered -= OnLineEntered;
            _customLinePresenter.LineEntered += OnLineEntered;
        }
    }

    private void OnNodeStart(string nodeName)
    {
        CurrentNodeName = nodeName ?? string.Empty;
        Trace("NodeStart", $"node={CurrentNodeName}");
    }

    private void OnLineEntered(LocalizedLine line)
    {
        if (line == null)
            return;

        _currentMeta = BuildMeta(line);

        CurrentLineId = _currentMeta.lineId;
        CurrentCharacterKey = _currentMeta.charName;

        Trace("LineEntered", $"meta={FormatMeta(_currentMeta)}, char={_currentMeta.charName}, text='{Preview(_currentMeta.rawText)}'");

        LineEntered?.Invoke(_currentMeta);
    }

    public override void OnPrepareForLine(MarkupParseResult line, TMP_Text text)
    {
        LinePrepared?.Invoke(_currentMeta);
    }

    public override void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text)
    {
        LineDisplayBegin?.Invoke(_currentMeta);
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
        LineFinishDisplaying?.Invoke(_currentMeta);
    }

    public override void OnLineWillDismiss()
    {
        LineWillDismiss?.Invoke(_currentMeta);
    }

    private YarnLineMeta BuildMeta(LocalizedLine line)
    {
        string nodeName = CurrentNodeName ?? string.Empty;
        string lineId = line.TextID ?? string.Empty;
        string charName = line.CharacterName ?? string.Empty;
        string rawText = line.TextWithoutCharacterName.Text ?? string.Empty;

        return new YarnLineMeta(nodeName, lineId, charName, rawText);
    }

    private void OnDestroy()
    {
        if (_dialogueRunner != null)
            _dialogueRunner.onNodeStart?.RemoveListener(OnNodeStart);

        if (_customLinePresenter != null)
            _customLinePresenter.LineEntered -= OnLineEntered;
    }

    private void Trace(string evt, string note = null)
    {
        if (_trace == null)
            return;

        _trace.Trace(nameof(YarnLineLifecycleBridge), evt, string.Empty, note);
    }

    private static string FormatMeta(YarnLineMeta meta)
    {
        return $"{meta.nodeName}/{meta.lineId}";
    }

    private static string Preview(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        const int max = 40;
        return text.Length <= max
            ? text
            : text.Substring(0, max) + "...";
    }
}