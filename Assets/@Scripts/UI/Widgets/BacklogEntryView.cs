using TMPro;
using UnityEngine;
using static UIRefValidation;

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
        if (!_valid) 
            return;
        
        _speakerText.text = "";
        _bodyText.text = entry.rawText;
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
}
