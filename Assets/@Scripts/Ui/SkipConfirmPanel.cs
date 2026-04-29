using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UIRefValidation;

public sealed class SkipConfirmPanel : UIPanel<SkipConfirmPanel.Refs>, IManagedUI
{
    public event Action OnConfirmed;
    public event Action OnCancelled;

    #region Refs
    public enum Refs
    {
        // Root / BG
        SkipConfirmBG_Root,
        SkipConfirmBG_Image,

        // Header
        Title_Root,
        Title_Text,

        // Body
        SummaryScroll_Root,
        SummaryText_Text,

        // Buttons
        ConfirmButton_Button,
        ConfirmButtonLabel_Text,
        CancelButton_Button,
        CancelButtonLabel_Text,
    }

    private RectTransform _bgRoot;
    private Image _bgImage;

    private TMP_Text _titleText;
    private TMP_Text _summaryText;

    private Button _confirmButton;
    private TMP_Text _confirmLabel;

    private Button _cancelButton;
    private TMP_Text _cancelLabel;

    private bool _valid;
    #endregion

    protected override void Initialize()
    {
        _bgRoot        = View.Rect(Refs.SkipConfirmBG_Root);
        _bgImage       = View.Image(Refs.SkipConfirmBG_Image);
        _titleText     = View.Text(Refs.Title_Text);
        _summaryText   = View.Text(Refs.SummaryText_Text);
        _confirmButton = View.Button(Refs.ConfirmButton_Button);
        _confirmLabel  = View.Text(Refs.ConfirmButtonLabel_Text);
        _cancelButton  = View.Button(Refs.CancelButton_Button);
        _cancelLabel   = View.Text(Refs.CancelButtonLabel_Text);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();
        if (!_valid) return;
#else
        _valid = true;
#endif

        BindEvent(_confirmButton, HandleConfirm);
        BindEvent(_cancelButton, HandleCancel);
    }

    #region Present
    public void Present(string title, string body, string confirmLabel = null, string cancelLabel = null)
    {
        if (!_valid)
            return;

        _titleText.text = title;
        _summaryText.text = body;

        _confirmLabel.text = confirmLabel;
        _cancelLabel.text = cancelLabel;
    }
    #endregion

    #region Event Handlers
    private void HandleConfirm(PointerEventData eventData) => OnConfirmed?.Invoke();
    private void HandleCancel(PointerEventData eventData) => OnCancelled?.Invoke();
    #endregion
    
    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(ref missing, _bgRoot,        Refs.SkipConfirmBG_Root);
        AppendMissing(ref missing, _bgImage,       Refs.SkipConfirmBG_Image);
        AppendMissing(ref missing, _titleText,     Refs.Title_Text);
        AppendMissing(ref missing, _summaryText,   Refs.SummaryText_Text);
        AppendMissing(ref missing, _confirmButton, Refs.ConfirmButton_Button);
        AppendMissing(ref missing, _confirmLabel,  Refs.ConfirmButtonLabel_Text);
        AppendMissing(ref missing, _cancelButton,  Refs.CancelButton_Button);
        AppendMissing(ref missing, _cancelLabel,   Refs.CancelButtonLabel_Text);

        if (missing.Length > 0)
        {
            Debug.LogWarning($"[SkipConfirmPanel] Missing refs:\n{missing}", this);
            return false;
        }

        return true;
    }
}