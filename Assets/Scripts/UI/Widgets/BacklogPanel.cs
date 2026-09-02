using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIRefValidation;

public sealed class BacklogPanel : UIPanel<BacklogPanel.Refs>
{
    public event Action OnCloseRequested;

    // 항목을 눌렀다 — 그 라인으로 되돌아가 달라는 요청. 되돌아갈 수 있는 항목에서만 온다.
    public event Action<DialogueLogEntry> OnJumpRequested;

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
    private RectTransform _content;

    #endregion

    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private BacklogEntryView entryPrefab;

    private readonly List<BacklogEntryView> _spawned = new();

    private bool _valid;

    protected override void OnInitialize()
    {
        _bgImage       = View.Image(Refs.BacklogBG_Image);
        _headerTitle   = View.Text(Refs.HeaderTitle_Text);
        _content       = View.Rect(Refs.Content);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif

        _headerTitle.text = "Backlog";
    }

    #region Present

    // canJump: 항목으로 되돌아갈 수 있는가 — 현재 장면 안이고 지금 라인이 아닌 것. 판단은 바깥의 것.
    public void Present(IReadOnlyList<DialogueLogEntry> entries, Func<DialogueLogEntry, bool> canJump)
    {
        if (!_valid)
            return;

        ClearEntries();

        int count = entries.Count;
        if (count <= 0)
            return;

        for (int i = count - 1; i >= 0; i--)
            Spawn(entries[i], canJump != null && canJump(entries[i]));

        ScrollToTop();
    }

    private void Spawn(in DialogueLogEntry e, bool jumpable)
    {
        var view = Instantiate(entryPrefab, _content);

        // 프리팹이 비활성이면 Awake 아직 안 돔. 사용 전 초기화.
        view.EnsureInitialized();

        view.gameObject.SetActive(true);
        view.Present(e, jumpable);
        view.Clicked += HandleEntryClicked;

        _spawned.Add(view);
    }


    private void ClearEntries()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            var v = _spawned[i];
            if (v == null) continue;
            v.Clicked -= HandleEntryClicked;
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

    private void HandleEntryClicked(DialogueLogEntry entry)
    {
        OnJumpRequested?.Invoke(entry);
    }

    #endregion

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _bgImage, Refs.BacklogBG_Image);
        AppendMissing(ref missing, _headerTitle, Refs.HeaderTitle_Text);

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
}
