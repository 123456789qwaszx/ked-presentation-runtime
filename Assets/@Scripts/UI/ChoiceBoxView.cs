using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChoiceBoxView : UIBase<ChoiceBoxView.Refs>
{
    public event Action<int> OnClicked;

    public enum Refs
    {
        ChoiceBox_Root,
        ChoiceBox_Anchor,
        ChoiceBox_Pad,
        ChoiceBox_Image,

        ChoiceBoxTextArea00_Root,
        ChoiceBoxTextArea00_Anchor,
        ChoiceBoxTextArea00_Text,

        ChoiceBoxTextArea01_Root,
        ChoiceBoxTextArea01_Anchor,
        ChoiceBoxTextArea01_Text,

        ChoiceBoxHit_Button,
    }

    private TMP_Text _labelText;
    private Button _button;
    private CanvasGroup _rootCg;

    private int _index;

    protected override void OnInitialize()
    {
        _rootCg   = View.CanvasGroup(Refs.ChoiceBox_Root);
        _labelText = View.Text(Refs.ChoiceBoxTextArea00_Text);
        _button    = View.Button(Refs.ChoiceBoxHit_Button);
    }

    public void Present(int index, string label)
    {
        _index = index;

        if (_rootCg != null)
        {
            _rootCg.alpha = 1f;
            _rootCg.interactable = true;
            _rootCg.blocksRaycasts = true;
        }

        if (_labelText != null)
            _labelText.text = label ?? "";

        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleClick);
            _button.onClick.AddListener(HandleClick);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_button != null)
            _button.onClick.RemoveListener(HandleClick);
    }

    private void HandleClick()
    {
        OnClicked?.Invoke(_index);
    }
}