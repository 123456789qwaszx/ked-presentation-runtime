using System;
using TMPro;
using UnityEngine;
using static UIRefValidation;

[Serializable]
public struct DialogueLogEntry
{
    public string lineId;
    public int lineSerial;
    public string nodeName;
    public string rawText;
    public double timestamp;

    // 표시된 최종 텍스트(마크업 제거/치환 결과)를 별도 저장하고 싶으면 필드 추가
    // public string renderedText;
}

public sealed class BacklogEntryView : UIBase<BacklogEntryView.Refs>
{
    #region Refs
    
    public enum Refs
    {
        BacklogEntry_Root,

        Speaker_Text,
        Body_Text,
    }

    private TMP_Text _speakerText;
    private TMP_Text _bodyText;
    
    #endregion

    public DialogueLogEntry Entry { get; private set; }

    private bool _valid;

    protected override void OnInitialize()
    {
        _speakerText = View.Text(Refs.Speaker_Text);
        _bodyText    = View.Text(Refs.Body_Text);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif
    }

    #region Present
    
    public void Present(in DialogueLogEntry entry)
    {
        if (!_valid) return;

        Entry = entry;

        string raw = entry.rawText ?? "";
        SplitSpeakerBody(raw, out string speaker, out string body);

        _speakerText.text = speaker;
        _bodyText.text = body;
    }

    #endregion

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _speakerText, Refs.Speaker_Text);
        AppendMissing(ref missing, _bodyText, Refs.Body_Text);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[BacklogEntryView] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }

    private static void SplitSpeakerBody(string raw, out string speaker, out string body)
    {
        speaker = "";
        body = "";

        if (string.IsNullOrWhiteSpace(raw))
            return;

        int idx = raw.IndexOf(':');

        if (idx > 0 && idx <= 24)
        {
            speaker = raw.Substring(0, idx).Trim();
            body = (idx + 1 < raw.Length) ? raw.Substring(idx + 1).Trim() : "";
            return;
        }

        body = raw.Trim();
    }
}
