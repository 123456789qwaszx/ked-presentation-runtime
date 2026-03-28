using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

public sealed class BacklogPanel : UIPanel<BacklogPanel.Refs>, IManagedUI
{
    public event Action OnCloseRequested;

    #region Refs
    
    public enum Refs
    {
        BacklogBG_Root,
        BacklogBG_Image,

        Header_Root,
        HeaderTitle_Text,

        CloseButton_BWidget,

        ScrollView_Root,
        ScrollRect,
        Viewport,
        Content,

        EntryPrefab,
    }

    private Image _bgImage;
    private TMP_Text _headerTitle;
    private ButtonWidget _close;
    private RectTransform _content;
    
    #endregion

    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private BacklogEntryView entryPrefab;

    private readonly List<BacklogEntryView> _spawned = new();
    
    private bool _valid;
    
    protected override void Initialize()
    {
        _bgImage       = View.Image(Refs.BacklogBG_Image);
        _headerTitle   = View.Text(Refs.HeaderTitle_Text);
        _close         = View.Widget<ButtonWidget>(Refs.CloseButton_BWidget);
        _content       = View.Rect(Refs.Content);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif

        _headerTitle.text = "Backlog";

        _close.OnClicked += HandleCloseClicked;
    }
    
    #region Present
    
    public void Present(BacklogRecorder backlog)
    {
        if (!_valid)
            return;

        ClearEntries();

        IReadOnlyList<DialogueLogEntry> entries = backlog.Entries;
        
        int count = entries.Count;
        if (count <= 0)
            return;
        
        for (int i = count - 1; i >= 0; i--)
            Spawn(entries[i]);
        
        ScrollToTop();
    }

    private void Spawn(in DialogueLogEntry e)
    {
        var view = UnityEngine.Object.Instantiate(entryPrefab, _content);
        view.gameObject.SetActive(true);
        view.Present(e);


        _spawned.Add(view);
    }


    private void ClearEntries()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            var v = _spawned[i];
            if (v == null) continue;
            UnityEngine.Object.Destroy(v.gameObject);
        }
        _spawned.Clear();
    }

    private void ScrollToTop()
    {
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
    }
    
    #endregion
    
    #region event Handlers
    
    private void HandleCloseClicked()
    {
        OnCloseRequested?.Invoke();
    }
    
    #endregion
    
    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _bgImage, Refs.BacklogBG_Image);
        AppendMissing(ref missing, _headerTitle, Refs.HeaderTitle_Text);
        AppendMissing(ref missing, _close, Refs.CloseButton_BWidget);

        AppendMissing(ref missing, scrollRect, Refs.ScrollRect);
        AppendMissing(ref missing, _content, Refs.Content);

        AppendMissing(ref missing, entryPrefab, Refs.EntryPrefab);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[BacklogPanel] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_close != null)
            _close.OnClicked -= HandleCloseClicked;
    }
}