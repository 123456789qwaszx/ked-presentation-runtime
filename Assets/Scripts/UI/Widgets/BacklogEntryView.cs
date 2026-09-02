using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using static UIRefValidation;

// 백로그 항목 하나. 누르면 그 라인으로 백점프를 요청한다 — 되돌아갈 수 있는 항목만.
// 클릭은 루트의 IPointerClickHandler로 받는다 — 자식 텍스트(raycastTarget)에서 올라온다.
public sealed class BacklogEntryView : UIBase<BacklogEntryView.Refs>, IPointerClickHandler
{
    private const float DimmedAlpha = 0.45f;

    public event Action<DialogueLogEntry> Clicked;

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
    private DialogueLogEntry _entry;
    private bool _jumpable;

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

    public void Present(in DialogueLogEntry entry, bool jumpable)
    {
        if (!_valid)
            return;

        _entry = entry;
        _jumpable = jumpable;

        _speakerText.text = "";
        _bodyText.text = entry.rawText;

        // 되돌아갈 수 없는 항목(이전 장면·지금 라인)은 흐리게 — 읽기는 전부, 개입은 이번 장면만.
        float alpha = jumpable ? 1f : DimmedAlpha;
        _bodyText.alpha = alpha;
        _speakerText.alpha = alpha;
    }

    #endregion

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!_valid || !_jumpable)
            return;

        Clicked?.Invoke(_entry);
    }

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
