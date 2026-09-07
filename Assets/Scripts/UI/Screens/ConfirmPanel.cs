using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UIRefValidation;

// 호출 측이 문구와 Confirm 동작을 정하는 재사용 확인 패널.
public sealed class ConfirmPanel : UIPanel<ConfirmPanel.Refs>
{
    public event Action ConfirmClicked;
    public event Action CloseClicked;

    #region Refs

    public enum Refs
    {
        ConfirmBG_Root,
        ConfirmBG_Image,

        Title_Root,
        Title_Text,

        SummaryScroll_Root,
        SummaryText_Text,

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

    #endregion

    private bool _valid;

    protected override void OnInitialize()
    {
        CacheRefs();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _valid = ValidateRefs();

        if (!_valid)
            return;
#else
        _valid = true;
#endif

        BindHandlers();
    }

    private void CacheRefs()
    {
        _bgRoot  = View.Rect(Refs.ConfirmBG_Root);
        _bgImage = View.Image(Refs.ConfirmBG_Image);

        _titleText   = View.Text(Refs.Title_Text);
        _summaryText = View.Text(Refs.SummaryText_Text);

        _confirmButton = View.Button(Refs.ConfirmButton_Button);
        _confirmLabel  = View.Text(Refs.ConfirmButtonLabel_Text);

        _cancelButton = View.Button(Refs.CancelButton_Button);
        _cancelLabel  = View.Text(Refs.CancelButtonLabel_Text);
    }

    private void BindHandlers()
    {
        BindEvent(
            _confirmButton,
            PressConfirmButton);

        BindEvent(
            _cancelButton,
            PressCancelButton);
    }

    #region Present

    public void Present(
        string title,
        string body,
        string confirmLabel,
        string cancelLabel)
    {
        if (!_valid)
            return;

        _titleText.text = title;
        _summaryText.text = body;

        _confirmLabel.text = confirmLabel;
        _cancelLabel.text = cancelLabel;
    }

    #endregion

    #region Handlers

    private void PressConfirmButton(PointerEventData _)
    {
        ConfirmClicked?.Invoke();
    }

    private void PressCancelButton(PointerEventData _)
    {
        CloseClicked?.Invoke();
    }

    #endregion

    private bool ValidateRefs()
    {
        string missing = "";

        AppendMissing(
            ref missing,
            _bgRoot,
            Refs.ConfirmBG_Root);

        AppendMissing(
            ref missing,
            _bgImage,
            Refs.ConfirmBG_Image);

        AppendMissing(
            ref missing,
            _titleText,
            Refs.Title_Text);

        AppendMissing(
            ref missing,
            _summaryText,
            Refs.SummaryText_Text);

        AppendMissing(
            ref missing,
            _confirmButton,
            Refs.ConfirmButton_Button);

        AppendMissing(
            ref missing,
            _confirmLabel,
            Refs.ConfirmButtonLabel_Text);

        AppendMissing(
            ref missing,
            _cancelButton,
            Refs.CancelButton_Button);

        AppendMissing(
            ref missing,
            _cancelLabel,
            Refs.CancelButtonLabel_Text);

        if (missing.Length > 0)
        {
            Debug.LogWarning(
                $"[ConfirmPanel] Missing refs:\n{missing}",
                this);

            return false;
        }

        return true;
    }
}